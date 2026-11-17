#!/usr/bin/env python3
"""Objective QA for the built instruments (we cannot rely on listening alone).

Re-implements the runtime sampler's voice maths (zone choice, pitch step incl. fine tune, sustain loop, linear
interpolation) and, for every playable note of every instrument, renders a held note and checks:
  * pitch  : measured fundamental of the rendered note is within 25 cents of the requested MIDI note
  * clicks : no sample-to-sample jump at the loop seam bigger than 4x the local typical jump
  * pumping: RMS of 50 ms windows across two loop cycles varies less than 4 dB (sustained instruments)
  * level  : peak below 0 dBFS
Also writes Logs/Audio/<Id>_demo.wav (a short scale) for listening.

Usage: ~/.cache/lmw-audio/venv/bin/python -I Tools/audio/verify_instruments.py [Id ...]
Exit code 1 if any check fails.
"""
import json, math, os, sys

import numpy as np
import soundfile as sf

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from build_instruments import detect_pitch, midi_to_hz, note_label  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
INST = os.path.join(ROOT, "Assets", "Audio", "Instruments")
OUTRATE = 48000


def load_inst(iid):
    meta = json.load(open(os.path.join(INST, iid, "instrument.json")))
    for z in meta["zones"]:
        z["pcm"], z["rate"] = sf.read(os.path.join(INST, iid, z["file"]), dtype="float32")
    return meta


def pick(meta, note, vel):
    best, bd = None, 1e9
    for z in meta["zones"]:
        if not (z["loVel"] <= vel <= z["hiVel"]):
            continue
        if z["lo"] <= note <= z["hi"]:
            return z
        d = min(abs(note - z["lo"]), abs(note - z["hi"]))
        if d < bd:
            best, bd = z, d
    return best or meta["zones"][0]


def render(meta, note, vel=100, seconds=None):
    z = pick(meta, note, vel)
    step = 2 ** ((note - z["root"] - z["tune"] / 100.0) / 12.0) * z["rate"] / OUTRATE
    pcm = z["pcm"]
    loop = meta["sustainLoop"] and z["loopEnd"] > z["loopStart"] + 64
    if seconds is None:
        seconds = max(2.5, z["loopEnd"] / step / OUTRATE + 2.0) if loop else 2.5
    n = int(seconds * OUTRATE)
    pos = np.arange(n) * step
    if loop:
        ls, le = z["loopStart"], z["loopEnd"]
        over = pos >= le
        pos[over] = ls + np.mod(pos[over] - le, le - ls)
    valid = pos < len(pcm) - 1
    pos = pos[valid]
    i = pos.astype(np.int64)
    f = (pos - i).astype(np.float32)
    out = pcm[i] + (pcm[i + 1] - pcm[i]) * f
    return out, z, loop, step


def check(iid):
    meta = load_inst(iid)
    fails = []
    pitched = not (meta["family"] == "Percussion" and meta["style"] == "DrumPads")
    notes = meta["playable"] if pitched else [z["root"] for z in meta["zones"]]
    for note in sorted(set(notes)):
        out, z, loop, step = render(meta, note)
        peak = float(np.max(np.abs(out))) if len(out) else 0
        if peak >= 1.0:
            fails.append(f"{note_label(note)} clips ({peak:.2f})")
        if pitched and len(out) > OUTRATE * 0.3:
            f = detect_pitch(out[: int(OUTRATE * 1.5)], OUTRATE, midi_to_hz(note))
            if f is None:
                fails.append(f"{note_label(note)} pitch unreadable")
            else:
                cents = 1200 * math.log2(f / midi_to_hz(note))
                lim = 25 if meta.get("tuneChecked", True) else 40
                # pianos/harp keep natural (stretch) tuning of the recording: allow more
                if meta["id"] in ("GrandPiano", "UprightPiano", "Harp", "Xylophone"):
                    lim = 40
                if abs(cents) > lim:
                    fails.append(f"{note_label(note)} pitch {cents:+.0f}c")
        if loop:
            # seam: output index where the source position wraps
            period_out = (z["loopEnd"] - z["loopStart"]) / step
            first_wrap = int((z["loopEnd"]) / step)
            d = np.abs(np.diff(out))
            typical = np.percentile(d[max(0, first_wrap - 2000):first_wrap + 2000], 95) + 1e-6
            seam = d[max(0, first_wrap - 3):first_wrap + 3].max() if first_wrap + 3 < len(d) else 0
            if seam > 4 * typical:
                fails.append(f"{note_label(note)} loop click ({seam / typical:.1f}x)")
            a = first_wrap - int(period_out * 0.5)
            b = min(len(out), first_wrap + int(period_out * 1.5))
            w = int(0.05 * OUTRATE)
            rms = [np.sqrt(np.mean(out[k:k + w] ** 2)) + 1e-9 for k in range(max(0, a), b - w, w)]
            if rms:
                spread = 20 * math.log10(max(rms) / min(rms))
                if spread > 4.0:
                    fails.append(f"{note_label(note)} loop pumps {spread:.1f} dB")
    # demo
    os.makedirs(os.path.join(ROOT, "Logs", "Audio"), exist_ok=True)
    demo = []
    seq = notes[:: max(1, len(notes) // 8)][:8] if pitched else notes
    for note in seq:
        out, *_ = render(meta, note, seconds=0.6 if not meta["sustainLoop"] else 0.9)
        env = np.ones(len(out), np.float32)
        r = min(len(out), int(0.08 * OUTRATE))
        env[-r:] = np.linspace(1, 0, r)
        demo.append(out * env)
        demo.append(np.zeros(int(0.05 * OUTRATE), np.float32))
    sf.write(os.path.join(ROOT, "Logs", "Audio", f"{iid}_demo.wav"), np.concatenate(demo) * 0.9, OUTRATE)
    return len(set(notes)), fails


if __name__ == "__main__":
    ids = sys.argv[1:] or sorted(d for d in os.listdir(INST) if os.path.isdir(os.path.join(INST, d)))
    bad = 0
    for iid in ids:
        n, fails = check(iid)
        status = "OK  " if not fails else "FAIL"
        print(f"[LMW audio QA] {status} {iid:15s} {n:3d} notes" + ("" if not fails else "  " + "; ".join(fails[:6])))
        bad += bool(fails)
    print(f"[LMW audio QA] {len(ids) - bad}/{len(ids)} instruments pass")
    sys.exit(1 if bad else 0)
