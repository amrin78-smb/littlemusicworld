#!/usr/bin/env python3
"""Downloads the licensed source recordings used to build Little Music World's instruments.

Raw sources are cached outside the repository (default ~/.cache/lmw-samples) and never committed; the processed,
trimmed instrument samples produced by build_instruments.py are what ships. Every source is recorded in
Documentation/AUDIO_LICENSES.md.

  VSCO 2 Community Edition  (Versilian Studios, CC0 1.0)           github.com/sgossner/VSCO-2-CE
  Salamander Grand Piano V3 (Alexander Holm, CC BY 3.0)             github.com/sfzinstruments/SalamanderGrandPiano
  FluidR3_GM SoundFont      (Frank Wen, MIT licence, Debian pkg)    fluid-soundfont-gm 3.1

Usage: python3 -I Tools/audio/fetch_samples.py [--cache DIR]
"""
import io, json, os, re, sys, tarfile, urllib.parse, urllib.request

CACHE = os.path.expanduser(os.environ.get("LMW_SAMPLE_CACHE", "~/.cache/lmw-samples"))
VSCO_RAW = "https://raw.githubusercontent.com/sgossner/VSCO-2-CE/master/"
VSCO_TREE = "https://api.github.com/repos/sgossner/VSCO-2-CE/git/trees/master?recursive=1"
SAL_RAW = "https://raw.githubusercontent.com/sfzinstruments/SalamanderGrandPiano/master/Samples/"
FLUID_DEB = "http://archive.ubuntu.com/ubuntu/pool/universe/f/fluid-soundfont/fluid-soundfont-gm_3.1-5.3_all.deb"

# VSCO 2 CE files, selected by regular expression on the repository path.
VSCO_SELECT = [
    r"^Keys/Upright Piano/Player_dyn[23]_rr1_\d+\.wav$",
    r"^Strings/Solo Violin/Arco Vib/LLVln_ArcoVib_[A-G]#?\d_f\.wav$",
    r"^Strings/Solo Violin/Pizz/LLVln_Pizz_[A-G]#?\d_f_RR1\.wav$",
    r"^Strings/Cello Section/susvib/susvib_[A-G]#?\d_v1_1\.wav$",
    r"^Strings/Harp/KSHarp_.*\.wav$",
    r"^Woodwinds/Flute/susvib/LDFlute_susvib_[A-G]#?\d_v1_1\.wav$",
    r"^Woodwinds/Clarinet/susLong/DCClar_susLong_[A-G]#?\d_v2_rr1_sum\.wav$",
    r"^Brass/Trumpet/sus/Sum_SHTrumpet_sus_[A-G]#?\d_v3_rr1\.wav$",
    r"^Brass/Tenor Trombone/sus/tenortbn_sus_[A-G]#?\d_v2_1\.wav$",
    r"^Percussion/Xylo/.*\.wav$",
    r"^Percussion/Glock/.*\.wav$",
    r"^Percussion/Snare2-HitSN_v[357]_rr1_Sum\.wav$",
    r"^Percussion/Snare2-rollSN_v3_rr1_Sum\.wav$",
    r"^Percussion/BDrumNewhit_v[36]_rr1_Sum\.wav$",
    r"^Percussion/cymbal-crash1_(mf|ff)_rr1\.wav$",
    r"^Percussion/susCymb1-(hitstick_f_rr1|hit-bell_mf|hit_f_rr1)\.wav$",
    r"^Percussion/(Conga|Tumba|Quinto)-HitN_v2_rr1_Sum\.wav$",
    r"^Percussion/Conga-Tap1_v1_rr1_Sum\.wav$",
    r"^Percussion/Tamb1-(Hit_v2_rr1|Shake_v1_rr1|Roll_v2_rr1)_Sum\.wav$",
    r"^Percussion/Triangle3-Hit_v2_rr1_Sum\.wav$",
    r"^Percussion/Claves1_Hit_v2_rr1_Sum\.wav$",
    r"^Percussion/Cowbell1_Hit_v2_rr1_Sum\.wav$",
    r"^Percussion/Timpani/.*\.wav$",
]

# Salamander: every minor third (A, C, D#, F#) from A0 to C8, two velocity layers (soft v6, firm v12).
SAL_NOTES = [f"{n}{o}" for o in range(0, 9) for n in ("A", "C", "D#", "F#")
             if (o, n) >= (0, "A") and not (o == 0 and n != "A") and not (o == 8 and n != "C")]
SAL_VELS = ("v6", "v12")


def get(url, timeout=120):
    req = urllib.request.Request(url, headers={"User-Agent": "lmw-asset-fetch/1.0"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read()


def save(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return False
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = get(url)
    tmp = path + ".part"
    with open(tmp, "wb") as f:
        f.write(data)
    os.replace(tmp, path)
    return True


def fetch_vsco():
    tree_path = os.path.join(CACHE, "vsco", "tree.json")
    if not os.path.exists(tree_path):
        os.makedirs(os.path.dirname(tree_path), exist_ok=True)
        with open(tree_path, "wb") as f:
            f.write(get(VSCO_TREE))
    tree = json.load(open(tree_path))["tree"]
    pats = [re.compile(p) for p in VSCO_SELECT]
    paths = [t["path"] for t in tree if t["type"] == "blob" and any(p.match(t["path"]) for p in pats)]
    n = 0
    for p in paths:
        if save(VSCO_RAW + urllib.parse.quote(p), os.path.join(CACHE, "vsco", p)):
            n += 1
    for lic in ("LICENSE", "Readme.txt"):
        save(VSCO_RAW + lic, os.path.join(CACHE, "vsco", lic))
    print(f"[LMW audio] VSCO: {len(paths)} files selected, {n} downloaded")


def fetch_salamander():
    n = 0
    for note in SAL_NOTES:
        for v in SAL_VELS:
            name = f"{note}{v}.flac"
            if save(SAL_RAW + urllib.parse.quote(name), os.path.join(CACHE, "salamander", name)):
                n += 1
    save("https://raw.githubusercontent.com/sfzinstruments/SalamanderGrandPiano/master/LICENSE",
         os.path.join(CACHE, "salamander", "LICENSE"))
    print(f"[LMW audio] Salamander: {len(SAL_NOTES) * len(SAL_VELS)} files, {n} downloaded")


def fetch_fluid():
    sf2 = os.path.join(CACHE, "fluid", "FluidR3_GM.sf2")
    if os.path.exists(sf2):
        print("[LMW audio] FluidR3_GM present"); return
    deb = get(FLUID_DEB, timeout=600)
    # .deb is an ar archive: find data.tar.*
    assert deb[:8] == b"!<arch>\n"
    pos = 8
    while pos < len(deb):
        name = deb[pos:pos + 16].decode().strip().rstrip("/")
        size = int(deb[pos + 48:pos + 58].decode().strip())
        body = deb[pos + 60:pos + 60 + size]
        pos += 60 + size + (size & 1)
        if name.startswith("data.tar"):
            if name.endswith("zst"):  # Python 3.12 tarfile lacks zstd: use the system zstd
                import subprocess
                body = subprocess.run(["zstd", "-dc"], input=body, capture_output=True, check=True).stdout
            with tarfile.open(fileobj=io.BytesIO(body), mode="r:*") as tar:
                for m in tar.getmembers():
                    base = os.path.basename(m.name)
                    if base == "FluidR3_GM.sf2" or base == "copyright":
                        os.makedirs(os.path.dirname(sf2), exist_ok=True)
                        with open(os.path.join(os.path.dirname(sf2), base), "wb") as f:
                            f.write(tar.extractfile(m).read())
    print("[LMW audio] FluidR3_GM extracted:", os.path.exists(sf2))


if __name__ == "__main__":
    if "--cache" in sys.argv:
        CACHE = os.path.expanduser(sys.argv[sys.argv.index("--cache") + 1])
    fetch_fluid()
    fetch_vsco()
    fetch_salamander()
