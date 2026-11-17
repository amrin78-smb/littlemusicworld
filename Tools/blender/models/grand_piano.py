"""Grand Piano — hero instrument of Piano Paradise (original model, procedurally built).

Visual target (Art Bible §4): a chunky, toy-like concert grand in deep indigo lacquer with gold trim; lid propped
open toward the audience; 49 real-pattern keys (C2–C6) as separate pivoted parts so Unity can press each key in
time with the sound; turned legs with brass casters; pedal lyre with three brass pedals; music stand.
Scale: Unity metres, keyboard front edge at z = -0.36, case spans x ±0.84, tail at z ≈ 2.05. Budget ≈ 18k tris.
Outputs: Assets/Art/Instruments/GrandPiano.fbx, Assets/Art/Textures/T_GrandPiano.png, Blender/SourceModels,
Blender/Renders/GrandPiano_*.png.
"""
import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L  # noqa: E402

PREVIEW = "--preview" in sys.argv
NAME = "GrandPiano"
LOW, HIGH = 36, 84  # C2..C6 (49 keys)

L.reset()
mats = L.Mats()
lac = mats.get("lacquer", "#3B3F6B", rough=0.22)
lac_dark = mats.get("lacquer_dark", "#2C2F55", rough=0.3)
gold = mats.get("gold", "#F2C14E", rough=0.32)
brass = mats.get("brass", "#E9B949", rough=0.28)
ivory = mats.get("ivory", "#FFFBF3", rough=0.32)
ebony = mats.get("ebony", "#24213A", rough=0.3)
felt = mats.get("felt", "#C0392B", rough=0.95)
board = mats.get("soundboard", "#E4B57E", rough=0.55, kind="wood", a="#E9C08E", b="#C8925A", scale=18.0)
string = mats.get("string", "#E6E9F0", rough=0.25)

m = L.Model(NAME)

# ------------------------------------------------------------------ case outline (top view, Unity x/z)
raw = [(-0.84, 0.0), (0.84, 0.0), (0.84, 0.42), (0.66, 0.78), (0.34, 1.05), (0.14, 1.38),
       (0.02, 1.72), (-0.16, 1.98), (-0.46, 2.06), (-0.72, 1.98), (-0.84, 1.78)]
outline = L.catmull(raw, steps=5)
CASE_Y0, CASE_Y1 = 0.60, 1.00

rim = L.outline_extrude("Rim", outline, CASE_Y0, CASE_Y1 - CASE_Y0, bevel=0.025, segs=3, inset=0.07)
m.add(rim, "Body", "P", lac)
band = L.outline_extrude("RimBand", L.offset_outline(outline, 0.008), 0.925, 0.03, bevel=0.006, segs=1, inset=0.012)
m.add(band, "Body", "P", gold)
bottom = L.outline_extrude("Bottom", L.offset_outline(outline, -0.03), CASE_Y0, 0.05, bevel=0.01, segs=1)
m.add(bottom, "Body", "P", lac_dark)
sb = L.outline_extrude("Soundboard", L.offset_outline(outline, -0.075), CASE_Y0 + 0.06, 0.02, bevel=0.004, segs=1)
m.add(sb, "Body", "P", board)
plate = L.outline_extrude("Plate", L.offset_outline(L.catmull([(x * 0.86, 0.14 + z * 0.86) for x, z in raw], 4), -0.04),
                          0.70, 0.05, bevel=0.02, segs=2)
m.add(plate, "Body", "P", gold)
# Plate holes (rosettes): darker discs give the cast-iron plate its classic look.
for i, (x, z) in enumerate([(-0.45, 0.62), (-0.1, 0.62), (-0.48, 1.25), (-0.2, 1.5)]):
    m.add(L.cyl(f"Rosette{i}", 0.09, 0.012, (x, 0.756, z), axis="y", segs=24, bevel=0.004), "Body", "P", lac_dark)
# Strings: fanned bundle from the keyboard end to the tail.
for i in range(26):
    t = i / 25
    x0 = -0.7 + 1.38 * t
    z0 = 0.22
    x1 = -0.74 + 0.7 * t
    z1 = 1.9 - 0.95 * t ** 1.3
    length = math.hypot(x1 - x0, z1 - z0)
    ang = math.degrees(math.atan2(x1 - x0, z1 - z0))
    m.add(L.rbox(f"String{i}", (0.008, 0.008, length), ((x0 + x1) / 2, 0.762, (z0 + z1) / 2), 0.003, segs=1,
                 euler=(0, ang, 0)), "Body", "P", string)

# ------------------------------------------------------------------ keyboard section
KEY_W, KEY_GAP, KEY_L, KEY_H = 0.0435, 0.0025, 0.30, 0.036
whites = [n for n in range(LOW, HIGH + 1) if (n % 12) in (0, 2, 4, 5, 7, 9, 11)]
kb_width = len(whites) * KEY_W
x_start = -kb_width / 2
KEY_TOP = 0.735
KEY_BACK_Z = -0.02  # hinge line (keys pivot here)
keybed = L.rbox("KeyBed", (kb_width + 0.32, 0.08, 0.44), (0, 0.655, -0.17), 0.02)
m.add(keybed, "Body", "P", lac)
for side in (-1, 1):
    cheek = L.rbox(f"Cheek{side}", (0.13, 0.14, 0.44), (side * (kb_width / 2 + 0.085), 0.72, -0.17), 0.035,
                   top=(1.0, 0.85), top_shift=0.03)
    m.add(cheek, "Body", "P", lac)
felt_strip = L.rbox("KeyFelt", (kb_width + 0.02, 0.012, 0.03), (0, KEY_TOP - 0.02, KEY_BACK_Z + 0.01), 0.004, segs=1)
m.add(felt_strip, "Body", "P", felt)
slip = L.rbox("KeySlip", (kb_width + 0.04, 0.05, 0.035), (0, 0.68, -0.38), 0.012)
m.add(slip, "Body", "P", lac)
fallboard = L.rbox("Fallboard", (kb_width + 0.04, 0.11, 0.035), (0, 0.80, 0.0), 0.015, euler=(-12, 0, 0))
m.add(fallboard, "Body", "P", lac)
name_badge = L.rbox("Badge", (0.16, 0.03, 0.006), (0, 0.815, -0.022), 0.006, segs=1, euler=(-12, 0, 0))
m.add(name_badge, "Body", "P", gold)

key_nodes = []
for i, n in enumerate(whites):
    x = x_start + KEY_W * (i + 0.5)
    node = m.node(f"Key_{n}", "Body", (x, KEY_TOP, KEY_BACK_Z))
    k = L.rbox(f"White{n}", (KEY_W - KEY_GAP, KEY_H, KEY_L), (x, KEY_TOP - KEY_H / 2, KEY_BACK_Z - KEY_L / 2), 0.008, segs=2)
    m.add(k, node, "P", ivory)
    key_nodes.append(node)
for n in range(LOW, HIGH + 1):
    if (n % 12) not in (1, 3, 6, 8, 10):
        continue
    left = whites.index(n - 1)
    x = x_start + KEY_W * (left + 1)
    node = m.node(f"Key_{n}", "Body", (x, KEY_TOP + 0.022, KEY_BACK_Z))
    k = L.rbox(f"Black{n}", (KEY_W * 0.58, 0.034, KEY_L * 0.6), (x, KEY_TOP + 0.005, KEY_BACK_Z - KEY_L * 0.3), 0.006, segs=2,
               top=(0.82, 0.92))
    m.add(k, node, "P", ebony)

# ------------------------------------------------------------------ music stand
desk = L.rbox("MusicDesk", (0.78, 0.36, 0.03), (0, 1.18, 0.20), 0.02, euler=(-16, 0, 0))
m.add(desk, "Body", "P", lac)
ledge = L.rbox("DeskLedge", (0.8, 0.035, 0.07), (0, 1.01, 0.16), 0.012)
m.add(ledge, "Body", "P", lac)
for i, x in enumerate((-0.24, 0.0, 0.24)):  # fretwork accents
    m.add(L.cyl(f"DeskDot{i}", 0.035, 0.012, (x, 1.22, 0.21), axis="z", segs=18, bevel=0.004, euler=(-16, 0, 0)), "Body", "P", gold)

# ------------------------------------------------------------------ legs, casters
leg_profile = [(0.0, 0.0), (0.075, 0.0), (0.085, 0.03), (0.07, 0.06), (0.05, 0.16), (0.045, 0.3), (0.06, 0.4),
               (0.075, 0.46), (0.09, 0.5), (0.1, 0.54), (0.0, 0.54)]
for i, (x, z) in enumerate([(-0.70, 0.12), (0.70, 0.12), (-0.42, 1.80)]):
    m.add(L.lathe(f"Leg{i}", leg_profile, (x, 0.07, z), axis="y", segs=20), "Body", "P", lac)
    m.add(L.rbox(f"LegCap{i}", (0.24, 0.05, 0.24), (x, CASE_Y0 - 0.005, z), 0.02), "Body", "P", lac)
    m.add(L.cyl(f"Caster{i}", 0.05, 0.05, (x, 0.04, z), axis="x", segs=18, bevel=0.012), "Body", "P", brass)
    m.add(L.rbox(f"CasterCup{i}", (0.1, 0.035, 0.1), (x, 0.075, z), 0.015), "Body", "P", brass)

# ------------------------------------------------------------------ pedal lyre
for side in (-1, 1):
    post = L.sweep(f"LyrePost{side}", [(side * 0.11, 0.12, 0.42), (side * 0.15, 0.3, 0.42), (side * 0.09, 0.48, 0.42),
                                       (side * 0.07, 0.6, 0.42)], L.circle(0.022, 10))
    m.add(post, "Body", "P", lac)
m.add(L.rbox("LyreTop", (0.3, 0.04, 0.08), (0, 0.6, 0.42), 0.015), "Body", "P", lac)
m.add(L.rbox("PedalBox", (0.4, 0.08, 0.16), (0, 0.1, 0.42), 0.03), "Body", "P", lac)
for i, x in enumerate((-0.1, 0.0, 0.1)):
    pedal = L.rbox(f"Pedal{i}", (0.05, 0.022, 0.16), (x, 0.085, 0.29), 0.01, top=(0.9, 1.0))
    m.add(pedal, "Body", "P", brass)

# ------------------------------------------------------------------ lid (hinged on the bass side, open 34°)
HINGE_X, HINGE_Y = -0.84, CASE_Y1 + 0.005
lid_node = m.node("Lid", "Body", (HINGE_X, HINGE_Y, 0.0))
lid_outline = L.catmull([(x, z) for x, z in raw], steps=5)
lid = L.outline_extrude("LidPanel", lid_outline, HINGE_Y, 0.032, bevel=0.012, segs=2)
L.set_origin(lid, (HINGE_X, HINGE_Y, 0.0))
lid.rotation_euler = (0.0, math.radians(-34), 0.0)  # Blender Y = Unity Z: lifts the treble (+x) side
m.add(lid, lid_node, "P", lac)
lid_front = L.rbox("LidFrontFlap", (1.66, 0.026, 0.38), (0.0, HINGE_Y + 0.013, 0.17), 0.01)
L.set_origin(lid_front, (HINGE_X, HINGE_Y, 0.0))
lid_front.rotation_euler = (0.0, math.radians(-34), 0.0)
m.add(lid_front, lid_node, "P", lac)
# Prop stick: from the treble rim up to the lid's underside (lid point 1.44 m from the hinge at 34°).
_r = 1.44
_top = (HINGE_X + _r * math.cos(math.radians(34)), HINGE_Y + _r * math.sin(math.radians(34)) - 0.01, 0.9)
_base = (0.62, CASE_Y1, 0.9)
_len = math.hypot(_top[0] - _base[0], _top[1] - _base[1])
_tilt = math.degrees(math.atan2(_base[0] - _top[0], _top[1] - _base[1]))
prop = L.cyl("LidProp", 0.018, _len, ((_top[0] + _base[0]) / 2, (_top[1] + _base[1]) / 2, 0.9), axis="y", segs=10,
             bevel=0.005, euler=(0, 0, -_tilt))
m.add(prop, "Body", "P", lac_dark)

# ------------------------------------------------------------------ bake, assemble, export
tex, em = L.bake(m, size=1024, ao_samples=48, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material(f"M_{NAME}", tex)
for obj, node, slot in m.parts:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
root, empties, meshes = L.build_hierarchy(m, NAME)
# LOD for the case body only (keys are tiny and disappear with the parent's LOD culling)
L.export_fbx(root, L.ART_DIR + f"/Instruments/{NAME}.fbx")
tris = sum(len(o.data.polygons) for o in meshes.values())
print(f"[LMW] {NAME}: {len(meshes)} mesh parts, ~{tris} faces, {len([n for n in m.nodes if n.startswith('Key_')])} keys")
L.save_source(NAME)
if PREVIEW:
    L.render_preview(L.RENDER_DIR + f"/{NAME}", [((-2.4, 2.2, -3.0), (0.0, 0.85, 0.6)), ((2.2, 1.5, -2.1), (0, 0.8, 0.3)),
                                                  ((2.6, 2.4, 3.4), (0.0, 0.9, 0.9))])
