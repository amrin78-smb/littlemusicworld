"""Little Music World instrument manifest — the single source of truth for every playable instrument.

Each entry says where the recordings come from and how the instrument behaves. build_instruments.py turns this
into trimmed/looped WAV samples plus Assets/Audio/Instruments/<Id>/instrument.json, which the Unity generator turns
into InstrumentDefinition assets.

Sources (see Documentation/AUDIO_LICENSES.md):
  vsco  = VSCO 2 Community Edition (CC0)          sal = Salamander Grand Piano V3 (CC BY 3.0, Alexander Holm)
  sf2   = FluidR3_GM SoundFont (MIT, Frank Wen)   — rendered note by note with FluidSynth, no effects
"""

C_MAJOR = [0, 2, 4, 5, 7, 9, 11]


def scale(low, high, steps=None):
    """MIDI notes from low..high (inclusive) in the given scale (chromatic when steps is None)."""
    out = []
    for n in range(low, high + 1):
        if steps is None or (n % 12) in steps:
            out.append(n)
    return out


def every(low, high, step):
    return list(range(low, high + 1, step))


# GM percussion keys used by the drum pads
KICK, SNARE, CLOSED_HAT, OPEN_HAT, PEDAL_HAT = 36, 38, 42, 46, 44
TOM_LOW, TOM_MID, TOM_HIGH, CRASH, RIDE = 45, 48, 50, 49, 51

INSTRUMENTS = [
    # ------------------------------------------------------------------ Piano Paradise
    dict(id="GrandPiano", tune_correct=False, name="Grand Piano", family="Keys", style="Keyboard", color="#3B3F6B",
         source="sal", rate=32000, notes=None,  # all Salamander minor-third roots A0..C8 within range
         sample_range=(33, 96), velocity_layers=[("v6", 0, 84), ("v12", 85, 127)],
         max_seconds=(5.5, 1.6), attack=0.002, release=0.45, playable=scale(48, 84), volume=1.0, voices=24),
    dict(id="UprightPiano", tune_correct=False, name="Upright Piano", family="Keys", style="Keyboard", color="#9C5B3A",
         source="vsco_upright", rate=32000, sample_range=(36, 96), velocity_layers=[("dyn2", 0, 84), ("dyn3", 85, 127)],
         max_seconds=(4.5, 1.5), attack=0.002, release=0.35, playable=scale(48, 84), volume=1.0, voices=20),
    dict(id="DigitalKeyboard", name="Digital Keyboard", family="Keys", style="Keyboard", color="#2EC4B6",
         source="sf2", program=4, rate=32000, notes=every(36, 96, 4), hold=2.6, sustain_loop=False,
         max_seconds=(3.0, 1.6), attack=0.003, release=0.30, playable=scale(48, 84), volume=0.9, voices=16),

    # ------------------------------------------------------------------ Drum Valley
    dict(id="DrumKit", name="Drum Kit", family="Percussion", style="DrumPads", color="#E63946",
         source="sf2_drums", rate=44100, notes=[KICK, SNARE, CLOSED_HAT, OPEN_HAT, TOM_LOW, TOM_MID, TOM_HIGH, CRASH, RIDE],
         labels={KICK: "Kick", SNARE: "Snare", CLOSED_HAT: "HiHat", OPEN_HAT: "OpenHat", TOM_LOW: "LowTom",
                 TOM_MID: "MidTom", TOM_HIGH: "HighTom", CRASH: "Crash", RIDE: "Ride"},
         choke={CLOSED_HAT: 1, OPEN_HAT: 1}, max_seconds=(3.0, 3.0), one_shot=True, volume=1.0, voices=16),
    dict(id="Snare", name="Snare Drum", family="Percussion", style="DrumPads", color="#F1FAEE",
         source="vsco_files", rate=44100, files=[
             ("Percussion/Snare2-HitSN_v3_rr1_Sum.wav", 38, 0, 70, "Tap"),
             ("Percussion/Snare2-HitSN_v5_rr1_Sum.wav", 38, 71, 105, "Hit"),
             ("Percussion/Snare2-HitSN_v7_rr1_Sum.wav", 38, 106, 127, "Hit"),
             ("Percussion/Snare2-rollSN_v3_rr1_Sum.wav", 40, 0, 127, "Roll")],
         max_seconds=(2.5, 2.5), one_shot=True, volume=1.0, voices=8),
    dict(id="BassDrum", name="Bass Drum", family="Percussion", style="DrumPads", color="#1D3557",
         source="vsco_files", rate=32000, files=[
             ("Percussion/BDrumNewhit_v3_rr1_Sum.wav", 36, 0, 90, "Boom"),
             ("Percussion/BDrumNewhit_v6_rr1_Sum.wav", 36, 91, 127, "Boom")],
         max_seconds=(3.0, 3.0), one_shot=True, volume=1.1, voices=6),
    dict(id="Cymbals", name="Cymbals", family="Percussion", style="DrumPads", color="#F4C430",
         source="vsco_files", rate=44100, files=[
             ("Percussion/cymbal-crash1_mf_rr1.wav", 49, 0, 127, "Crash"),
             ("Percussion/susCymb1-hitstick_f_rr1.wav", 51, 0, 127, "Ride"),
             ("Percussion/susCymb1-hit-bell_mf.wav", 53, 0, 127, "Bell"),
             ("Percussion/susCymb1-hit_f_rr1.wav", 57, 0, 127, "Swell")],
         max_seconds=(4.0, 4.0), one_shot=True, volume=0.8, voices=8),
    dict(id="Bongos", name="Bongos", family="Percussion", style="DrumPads", color="#D98E04",
         source="sf2_drums", rate=44100, notes=[60, 61], labels={60: "High", 61: "Low"},
         max_seconds=(1.5, 1.5), one_shot=True, volume=1.0, voices=8),
    dict(id="Congas", name="Congas", family="Percussion", style="DrumPads", color="#B5651D",
         source="vsco_files", rate=44100, files=[
             ("Percussion/Quinto-HitN_v2_rr1_Sum.wav", 63, 0, 127, "High"),
             ("Percussion/Conga-HitN_v2_rr1_Sum.wav", 64, 0, 127, "Middle"),
             ("Percussion/Tumba-HitN_v2_rr1_Sum.wav", 65, 0, 127, "Low"),
             ("Percussion/Conga-Tap1_v1_rr1_Sum.wav", 62, 0, 127, "Tap")],
         max_seconds=(1.8, 1.8), one_shot=True, volume=1.0, voices=8),
    dict(id="Tambourine", name="Tambourine", family="Percussion", style="DrumPads", color="#E9C46A",
         source="vsco_files", rate=44100, files=[
             ("Percussion/Tamb1-Hit_v2_rr1_Sum.wav", 54, 0, 127, "Hit"),
             ("Percussion/Tamb1-Shake_v1_rr1_Sum.wav", 55, 0, 127, "Shake"),
             ("Percussion/Tamb1-Roll_v2_rr1_Sum.wav", 56, 0, 127, "Roll")],
         max_seconds=(2.0, 2.0), one_shot=True, volume=0.85, voices=6),
    dict(id="Maracas", name="Maracas", family="Percussion", style="DrumPads", color="#F77F00",
         source="sf2_drums", rate=44100, notes=[70, 82], labels={70: "Shake", 82: "Shaker"},
         max_seconds=(1.0, 1.0), one_shot=True, volume=1.0, voices=6),
    dict(id="Xylophone", tune_correct=False, name="Xylophone", family="Percussion", style="Mallets", color="#FF6F91",
         source="vsco_dir", rate=44100, dir="Percussion/Xylo", name_pattern=r"Xylo_Medium_([A-G]#?\d)_",
         octave_shift=12,  # xylophone sounds an octave above written pitch; VSCO names are written pitch
         max_seconds=(2.0, 1.0), one_shot=True, playable=scale(72, 84, C_MAJOR), volume=0.9, voices=12),

    # ------------------------------------------------------------------ String Town
    dict(id="AcousticGuitar", name="Acoustic Guitar", family="Strings", style="Strum", color="#C68B59",
         source="sf2", program=25, rate=32000, notes=every(40, 84, 4), hold=2.6,
         max_seconds=(3.0, 1.8), attack=0.002, release=0.25, one_shot=False, open_strings=[40, 45, 50, 55, 59, 64],
         playable=scale(40, 76), volume=1.0, voices=18),
    dict(id="ElectricGuitar", name="Electric Guitar", family="Strings", style="Strum", color="#E63946",
         source="sf2", program=27, rate=32000, notes=every(40, 84, 4), hold=2.8,
         max_seconds=(3.0, 2.0), attack=0.002, release=0.25, open_strings=[40, 45, 50, 55, 59, 64],
         playable=scale(40, 76), volume=0.85, voices=18),
    dict(id="Ukulele", name="Ukulele", family="Strings", style="Strum", color="#FFB703",
         # GM has no ukulele: the nylon-string guitar recorded high and short is the closest honest timbre.
         source="sf2", program=24, rate=32000, notes=every(60, 84, 3), hold=1.4,
         max_seconds=(1.8, 1.2), attack=0.002, release=0.18, open_strings=[67, 60, 64, 69],
         playable=scale(60, 81), volume=1.0, voices=12),
    dict(id="Violin", name="Violin", family="Strings", style="Bow", color="#A23B1C",
         source="vsco_dir", rate=32000, dir="Strings/Solo Violin/Arco Vib", name_pattern=r"ArcoVib_([A-G]#?\d)_f",
         sustain_loop=True, max_seconds=(3.2, 3.2), attack=0.06, release=0.22,
         playable=scale(67, 84, C_MAJOR), volume=0.75, voices=6),
    dict(id="Cello", name="Cello", family="Strings", style="Bow", color="#6A2E1F",
         source="vsco_dir", rate=32000, dir="Strings/Cello Section/susvib", name_pattern=r"susvib_([A-G]#?\d)_v1",
         sustain_loop=True, max_seconds=(3.2, 3.2), attack=0.08, release=0.3,
         playable=scale(48, 64, C_MAJOR), volume=0.8, voices=6),
    dict(id="Harp", tune_correct=False, name="Harp", family="Strings", style="Pluck", color="#D4A373",
         source="vsco_dir", rate=32000, dir="Strings/Harp", name_pattern=r"KSHarp_([A-G]#?\d)_",
         max_seconds=(3.5, 1.6), one_shot=True, playable=scale(48, 84, C_MAJOR), volume=0.95, voices=24),
    dict(id="Banjo", name="Banjo", family="Strings", style="Strum", color="#E9D8A6",
         source="sf2", program=105, rate=32000, notes=every(48, 84, 4), hold=1.6,
         max_seconds=(2.0, 1.2), attack=0.002, release=0.2, open_strings=[67, 50, 55, 59, 62],
         playable=scale(50, 79), volume=0.9, voices=15),

    # ------------------------------------------------------------------ Wind Village
    dict(id="Flute", name="Flute", family="Wind", style="Blow", color="#8ECAE6",
         source="vsco_dir", rate=32000, dir="Woodwinds/Flute/susvib", name_pattern=r"susvib_([A-G]#?\d)_v1_1",
         sustain_loop=True, max_seconds=(3.0, 3.0), attack=0.05, release=0.18,
         playable=scale(72, 84, C_MAJOR), volume=0.8, voices=4),
    dict(id="Trumpet", name="Trumpet", family="Wind", style="Blow", color="#FFB000",
         source="vsco_dir", rate=32000, dir="Brass/Trumpet/sus", name_pattern=r"sus_([A-G]#?\d)_v3",
         sustain_loop=True, max_seconds=(3.0, 3.0), attack=0.03, release=0.15,
         playable=scale(60, 72, C_MAJOR), volume=0.7, voices=4),
    dict(id="Saxophone", name="Saxophone", family="Wind", style="Blow", color="#C9A227",
         source="sf2", program=65, rate=32000, notes=every(49, 81, 4), hold=2.8, sustain_loop=True,
         max_seconds=(3.0, 3.0), attack=0.03, release=0.15, playable=scale(60, 72, C_MAJOR), volume=0.8, voices=4),
    dict(id="Clarinet", name="Clarinet", family="Wind", style="Blow", color="#2B2D42",
         source="vsco_dir", rate=32000, dir="Woodwinds/Clarinet/susLong", name_pattern=r"susLong_([A-G]#?\d)_v2",
         sustain_loop=True, max_seconds=(3.0, 3.0), attack=0.03, release=0.15,
         playable=scale(60, 72, C_MAJOR), volume=0.85, voices=4),
    dict(id="Trombone", name="Trombone", family="Wind", style="Blow", color="#E0A458",
         source="vsco_dir", rate=32000, dir="Brass/Tenor Trombone/sus", name_pattern=r"sus_([A-G]#?\d)_v2",
         sustain_loop=True, max_seconds=(3.0, 3.0), attack=0.04, release=0.18,
         playable=scale(48, 60, C_MAJOR), volume=0.75, voices=4),
    dict(id="Recorder", name="Recorder", family="Wind", style="Blow", color="#90BE6D",
         source="sf2", program=74, rate=32000, notes=every(60, 96, 4), hold=2.6, sustain_loop=True,
         max_seconds=(3.0, 3.0), attack=0.03, release=0.12, playable=scale(72, 84, C_MAJOR), volume=0.75, voices=4),
]
