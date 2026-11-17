#!/usr/bin/env python3
"""Builds Little Music World's instrument samples from the cached licensed sources.

For every instrument in instruments.py:
  1. gather source recordings (VSCO / Salamander files, or FluidSynth renders of FluidR3_GM),
  2. mix to mono, resample, trim leading silence (keeping the attack), cap the length and fade the tail,
  3. measure the real pitch of each sample and store fine tuning in cents,
  4. for bowed/blown instruments, find a seamless sustain loop (whole periods + crossfade),
  5. normalise the instrument as a whole (keeps velocity layers' relative loudness),
  6. write 16-bit WAVs + instrument.json into Assets/Audio/Instruments/<Id>/.

Usage: ~/.cache/lmw-audio/venv/bin/python -I Tools/audio/build_instruments.py [Id ...]
"""
import json, math, os, re, sys
from math import gcd

import numpy as np
import soundfile as sf
from scipy.signal import resample_poly

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from instruments import INSTRUMENTS  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Audio", "Instruments")
CACHE = os.path.expanduser(os.environ.get("LMW_SAMPLE_CACHE", "~/.cache/lmw-samples"))
NOTE_NAMES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]


def log(*a):
    print("[LMW audio]", *a, flush=True)


def note_number(name):
    m = re.match(r"([A-G])(#|b)?(-?\d)$", name)
    pc = NOTE_NAMES.index(m.group(1)) + (1 if m.group(2) == "#" else -1 if m.group(2) == "b" else 0)
    return (int(m.group(3)) + 1) * 12 + pc


def note_label(n):
    return NOTE_NAMES[n % 12].replace("#", "s") + str(n // 12 - 1)


def midi_to_hz(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


# ----------------------------------------------------------------------------------------------- loading

def load(path, rate):
    x, sr = sf.read(path, dtype="float32", always_2d=True)
    x = x.mean(axis=1)
    if sr != rate:
        g = gcd(sr, rate)
        x = resample_poly(x, rate // g, sr // g).astype(np.float32)
    return x


_fs = None


def fluid():
    global _fs
    if _fs is None:
        import fluidsynth
        _fs = {}
    return _fs


def render_sf2(program, note, rate, hold, tail=1.2, drums=False, velocity=100):
    import fluidsynth
    key = (rate,)
    cache = fluid()
    if key not in cache:
        syn = fluidsynth.Synth(samplerate=float(rate), gain=0.6)
        syn.setting("synth.reverb.active", 0)
        syn.setting("synth.chorus.active", 0)
        sfid = syn.sfload(os.path.join(CACHE, "fluid", "FluidR3_GM.sf2"))
        cache[key] = (syn, sfid)
    syn, sfid = cache[key]
    ch = 9 if drums else 0
    if drums:
        syn.program_select(9, sfid, 128, 0)
    else:
        syn.program_select(0, sfid, 0, program)
    syn.get_samples(int(0.05 * rate))  # settle
    syn.noteon(ch, note, velocity)
    a = syn.get_samples(int(hold * rate))
    syn.noteoff(ch, note)
    b = syn.get_samples(int(tail * rate))
    syn.system_reset() if hasattr(syn, "system_reset") else None
    s = np.concatenate([a, b]).astype(np.float32).reshape(-1, 2).mean(axis=1) / 32768.0
    syn.get_samples(int(0.3 * rate))  # flush any remaining tail
    return s


# ----------------------------------------------------------------------------------------------- processing

def trim(x, rate, max_seconds):
    peak = float(np.max(np.abs(x))) or 1.0
    thr = peak * 10 ** (-42 / 20)
    idx = np.nonzero(np.abs(x) > thr)[0]
    if len(idx) == 0:
        return x[:1]
    start = max(0, idx[0] - int(0.0015 * rate))
    x = x[start:]
    # end: last point above -60 dB, capped to max_seconds
    thr_end = peak * 10 ** (-60 / 20)
    idx = np.nonzero(np.abs(x) > thr_end)[0]
    end = min(len(x), idx[-1] + int(0.02 * rate), int(max_seconds * rate))
    x = x[:end].copy()
    fade = min(len(x) // 4, int(0.08 * rate))
    if fade > 0:
        x[-fade:] *= np.linspace(1.0, 0.0, fade, dtype=np.float32) ** 2
    return x


def detect_pitch(x, rate, expected=None):
    """Autocorrelation pitch on a stable window; returns Hz or None. Constrained to ±1.5 semitones if expected."""
    n = len(x)
    if n < rate * 0.12:
        return None
    w = min(8192, n // 2)
    best = None
    vals = []
    for c in (0.25, 0.4, 0.55):
        s = int(n * c)
        seg = x[s:s + w]
        if len(seg) < w:
            continue
        seg = (seg - seg.mean()) * np.hanning(len(seg))
        spec = np.fft.rfft(seg, 2 * w)
        ac = np.fft.irfft(spec * np.conj(spec))[:w]
        if ac[0] <= 0:
            continue
        ac /= ac[0]
        if expected:
            lo = int(rate / (expected * 2 ** (1.5 / 12)))
            hi = int(rate / (expected * 2 ** (-1.5 / 12))) + 1
        else:
            lo, hi = int(rate / 2000), int(rate / 28)
        lo, hi = max(lo, 2), min(hi, w - 2)
        if hi <= lo:
            continue
        k = lo + int(np.argmax(ac[lo:hi]))
        if ac[k] < 0.3:
            continue
        # parabolic interpolation
        a, b, cc = ac[k - 1], ac[k], ac[k + 1]
        d = 0.5 * (a - cc) / (a - 2 * b + cc) if (a - 2 * b + cc) != 0 else 0.0
        if abs(d) < 1.0:
            vals.append(rate / (k + d))
    if not vals:
        return best
    return float(np.median(vals))


def make_loop(x, rate, f0):
    """Finds a sustain loop of whole pitch periods and crossfades it seamlessly. Returns (x, start, end, quality).

    Bowed/blown notes have vibrato (~5-6 Hz), so similarity is judged over ~70 ms windows (spanning part of a
    vibrato cycle) and candidate loop lengths from 0.3 to 1.0 s are searched; the best pair gets an equal-power
    crossfade of up to 0.25 s, which hides the remaining phase/amplitude difference.
    """
    n = len(x)
    period = rate / f0
    end = int(min(n * 0.88, n - 0.1 * rate))
    attack_end = int(0.3 * rate)
    win = int(0.035 * rate)
    ref = x[end - win:end + win]
    ref_n = np.linalg.norm(ref) + 1e-9
    best, best_s = -2.0, None
    k = 1
    while True:
        s = int(round(end - k * period))
        k += 1
        if end - s < 0.3 * rate:
            continue
        if end - s > 1.0 * rate or s - win < attack_end:
            break
        seg = x[s - win:s + win]
        c = float(np.dot(seg, ref) / (np.linalg.norm(seg) * ref_n))
        if c > best:
            best, best_s = c, s
    if best_s is None:
        best_s = max(attack_end + win, end - int(0.3 * rate))
    # sample-accurate refinement within one period
    s0 = best_s
    for s in range(int(s0 - period / 2), int(s0 + period / 2) + 1):
        seg = x[s - win:s + win]
        if len(seg) != len(ref):
            continue
        c = float(np.dot(seg, ref) / (np.linalg.norm(seg) * ref_n))
        if c > best:
            best, best_s = c, s
    start = best_s
    xf = int(min((end - start) * 0.5, 0.25 * rate, start - attack_end))
    xf_max = xf
    # Flatten the loudness of the looped body (players swell or fade while holding a note): divide by a smooth
    # RMS envelope from just before the crossfade region to the loop end, easing in so the attack is untouched.
    x = x.copy()
    a0 = start - xf
    w = int(0.06 * rate)
    sq = np.convolve(x.astype(np.float64) ** 2, np.ones(w) / w, mode="same")
    env = np.sqrt(np.maximum(sq, 1e-12))
    target = float(np.sqrt(np.mean(env[start:end] ** 2)))
    ramp = int(0.15 * rate)
    lo = max(0, a0 - ramp)
    g = np.ones(len(x))
    g[a0:end] = target / env[a0:end]
    g[lo:a0] = 1.0 + (target / env[a0] - 1.0) * np.linspace(0.0, 1.0, a0 - lo)
    g = np.clip(g, 0.25, 4.0)
    x[:end] = (x[:end] * g[:end]).astype(np.float32)
    # Try several crossfade lengths/curves; keep the one whose looped body has the steadiest loudness and the
    # smallest seam jump (long fades can cancel when vibrato drifts the two regions out of phase).
    period_i = max(1, int(period))
    best_y, best_score = None, 1e9
    for fade_s in (0.012, 0.03, 0.06, 0.12, 0.25):
        n_x = int(min(fade_s * rate, xf_max))
        if n_x < 8:
            continue
        for curve in ("linear", "power"):
            t = np.linspace(0.0, 1.0, n_x, dtype=np.float32)
            ga, gb = (1 - t, t) if curve == "linear" else (np.cos(t * np.pi / 2), np.sin(t * np.pi / 2))
            y = x.copy()
            y[end - n_x:end] = x[end - n_x:end] * ga + x[start - n_x:start] * gb
            body = np.concatenate([y[start:end], y[start:end]])
            w = int(0.02 * rate)
            rms = np.sqrt(np.convolve(body.astype(np.float64) ** 2, np.ones(w) / w, mode="valid")[::w // 2]) + 1e-9
            spread = 20 * np.log10(rms.max() / rms.min())
            seam = abs(float(y[end - 1]) - float(y[start])) / (np.percentile(np.abs(np.diff(body)), 95) + 1e-9)
            score = spread + 2.0 * max(0.0, seam - 1.0)
            if score < best_score:
                best_score, best_y = score, y
    y = best_y[:end + 16].copy()
    # Guard samples after the loop end mirror the loop start, so interpolation across the seam is continuous.
    y[end:end + 16] = y[start:start + 16]
    return y, int(start), int(end), best


# ----------------------------------------------------------------------------------------------- sources

def gather(inst):
    """Yields dicts {audio, root, layer, loVel, hiVel, label, expected}."""
    src = inst["source"]
    rate = inst["rate"]
    lo_r, hi_r = inst.get("sample_range", (0, 127))
    out = []
    if src == "sal":
        for layer, lv, hv in inst["velocity_layers"]:
            for f in sorted(os.listdir(os.path.join(CACHE, "salamander"))):
                m = re.match(r"([A-G]#?\d)" + layer + r"\.flac$", f)
                if not m:
                    continue
                n = note_number(m.group(1))
                if lo_r <= n <= hi_r:
                    out.append(dict(path=os.path.join(CACHE, "salamander", f), root=n, layer=layer, lv=lv, hv=hv))
    elif src == "vsco_upright":
        d = os.path.join(CACHE, "vsco", "Keys", "Upright Piano")
        for layer, lv, hv in inst["velocity_layers"]:
            for f in sorted(os.listdir(d)):
                m = re.match(r"Player_" + layer + r"_rr1_(\d+)\.wav$", f)
                if m:
                    out.append(dict(path=os.path.join(d, f), index=int(m.group(1)), layer=layer, lv=lv, hv=hv))
    elif src == "vsco_dir":
        d = os.path.join(CACHE, "vsco", inst["dir"])
        for f in sorted(os.listdir(d)):
            m = re.search(inst["name_pattern"], f)
            if m:
                n = note_number(m.group(1)) + inst.get("octave_shift", 0)
                out.append(dict(path=os.path.join(d, f), root=n, layer="v", lv=0, hv=127))
    elif src == "vsco_files":
        for path, note, lv, hv, label in inst["files"]:
            out.append(dict(path=os.path.join(CACHE, "vsco", path), root=note, layer=f"v{hv}", lv=lv, hv=hv,
                            label=label, perc=True))
    elif src == "sf2":
        for n in inst["notes"]:
            out.append(dict(render=("sf2", inst["program"], n, inst.get("hold", 2.0)), root=n, layer="v", lv=0, hv=127))
    elif src == "sf2_drums":
        for n in inst["notes"]:
            out.append(dict(render=("drum", 0, n, 0.6), root=n, layer="v", lv=0, hv=127,
                            label=inst.get("labels", {}).get(n), perc=True))
    for o in out:
        if "render" in o:
            kind, prog, n, hold = o["render"]
            o["audio"] = render_sf2(prog, n, rate, hold, drums=(kind == "drum"))
        else:
            o["audio"] = load(o["path"], rate)
    return out


def length_cap(inst, root):
    lo_s, hi_s = inst["max_seconds"]
    lo_r, hi_r = inst.get("sample_range", (36, 96))
    t = min(1.0, max(0.0, (root - lo_r) / max(1, hi_r - lo_r)))
    return lo_s + (hi_s - lo_s) * t


def assign_ranges(zones, percussion):
    if percussion:
        for z in zones:
            z["lo"] = z["hi"] = z["root"]
        return
    by_layer = {}
    for z in zones:
        by_layer.setdefault(z["layer"], []).append(z)
    for layer in by_layer.values():
        layer.sort(key=lambda z: z["root"])
        for i, z in enumerate(layer):
            z["lo"] = 0 if i == 0 else (layer[i - 1]["root"] + z["root"]) // 2 + 1
            z["hi"] = 127 if i == len(layer) - 1 else (z["root"] + layer[i + 1]["root"]) // 2


def build(inst):
    iid = inst["id"]
    rate = inst["rate"]
    log(f"{iid}: gathering")
    raw = gather(inst)
    if not raw:
        log(f"{iid}: NO SOURCES"); return None
    percussion = inst["family"] == "Percussion" and inst["style"] == "DrumPads"
    pitched = not percussion

    # Upright piano: Player_dynN_rr1_<i>.wav is sampled in major thirds from A0, i.e. MIDI note = 21 + 2*i
    # (verified by measuring every file's pitch; see Documentation/ASSET_PIPELINE.md).
    if inst["source"] == "vsco_upright":
        for o in raw:
            o["root"] = 21 + 2 * o["index"]
        lo_r, hi_r = inst.get("sample_range", (0, 127))
        raw = [o for o in raw if lo_r <= o["root"] <= hi_r]

    zones = []
    for o in raw:
        x = trim(o["audio"], rate, length_cap(inst, o["root"]))
        z = dict(root=o["root"], layer=o["layer"], loVel=o["lv"], hiVel=o["hv"], label=o.get("label") or "",
                 tune=0.0, loopStart=-1, loopEnd=-1, audio=x)
        if pitched:
            f = detect_pitch(x, rate, midi_to_hz(o["root"]))
            if f:
                cents = 1200 * math.log2(f / midi_to_hz(o["root"]))
                if not inst.get("tune_correct", True):
                    z["tune"] = 0.0
                elif abs(cents) < 60:
                    z["tune"] = round(cents, 1)
                else:  # mislabelled source: re-root to the measured semitone, keep the fine remainder
                    shift = int(round(cents / 100.0))
                    z["root"] = o["root"] + shift
                    z["tune"] = round(cents - 100 * shift, 1)
                    log(f"{iid}: {note_label(o['root'])} measures {cents:+.0f} cents -> re-rooted to {note_label(z['root'])}")
            if inst.get("sustain_loop"):
                f0 = f or midi_to_hz(o["root"])
                if len(x) > rate * 1.0:
                    y, s, e, q = make_loop(x, rate, f0)
                    z.update(audio=y, loopStart=s, loopEnd=e)
                    if q < 0.6:
                        log(f"{iid}: loop {note_label(o['root'])} correlation {q:.2f} (check by ear)")
                else:
                    log(f"{iid}: {note_label(o['root'])} too short for a loop ({len(x) / rate:.2f}s)")
        zones.append(z)

    peak = max(float(np.max(np.abs(z["audio"]))) for z in zones) or 1.0
    norm = 0.89 / peak
    assign_ranges(zones, percussion)

    d = os.path.join(OUT, iid)
    os.makedirs(d, exist_ok=True)
    for f in os.listdir(d):
        if f.endswith(".wav") or f.endswith(".wav.meta"):
            os.remove(os.path.join(d, f))
    zj = []
    total = 0
    for z in sorted(zones, key=lambda z: (z["layer"], z["root"])):
        name = f"{iid}_{note_label(z['root'])}_{z['layer']}.wav"
        a = np.clip(z["audio"] * norm, -1, 1)
        sf.write(os.path.join(d, name), a, rate, subtype="PCM_16")
        total += len(a)
        zj.append(dict(file=name, root=z["root"], lo=z["lo"], hi=z["hi"], loVel=z["loVel"], hiVel=z["hiVel"],
                       loopStart=z["loopStart"], loopEnd=z["loopEnd"], tune=z["tune"], gain=1.0, label=z["label"]))

    lows = [z["root"] for z in zones]
    choke = inst.get("choke", {})
    meta = dict(
        id=iid, name=inst["name"], family=inst["family"], style=inst["style"], color=inst["color"],
        attack=inst.get("attack", 0.002), release=inst.get("release", 0.3), oneShot=inst.get("one_shot", False),
        sustainLoop=inst.get("sustain_loop", False), volume=inst.get("volume", 1.0), voices=inst.get("voices", 12),
        lowest=min(lows), highest=max(lows), playable=inst.get("playable", sorted(set(lows))),
        openStrings=inst.get("open_strings", []),
        choke={str(k): v for k, v in choke.items()}, rate=rate, zones=zj)
    with open(os.path.join(d, "instrument.json"), "w") as f:
        json.dump(meta, f, indent=1)
    log(f"{iid}: {len(zj)} zones, {total / rate:.1f}s audio, {total * 2 / 1e6:.1f} MB PCM")
    return meta


if __name__ == "__main__":
    want = set(sys.argv[1:])
    done = []
    for inst in INSTRUMENTS:
        if want and inst["id"] not in want:
            continue
        m = build(inst)
        if m:
            done.append(m["id"])
    log(f"built {len(done)} instruments: {' '.join(done)}")
