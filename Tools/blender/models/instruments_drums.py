"""Drum Valley instruments (Art Bible §4): Drum Kit, Snare, Bass Drum, Cymbals, Bongos, Congas, Tambourine,
Maracas, Xylophone — one shared 2048² atlas (T_DrumInstruments).

Every playable part has its own pivot node named "Pad_<note>" (drums/cymbals: the GM/instrument note it plays,
xylophone bars: their MIDI note) so Unity can squash/wobble exactly the part that sounds. Fronts face −Z.
"""
import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L  # noqa: E402

PREVIEW = "--preview" in sys.argv
OUT = L.ART_DIR + "/Instruments"
L.reset()
M = L.Mats()
chrome = M.get("chrome", "#DDE3EE", rough=0.18)
dark = M.get("darkmetal", "#3D405B", rough=0.4)
skin = M.get("skin", "#FFF6E6", rough=0.35)
shellR = M.get("shell_red", "#E2553F", rough=0.25)
shellO = M.get("shell_orange", "#F4A259", rough=0.3)
brass = M.get("brass", "#F2C14E", rough=0.25)
wood = M.get("wood", "#C68B59", rough=0.55, kind="wood", a="#D9A06B", b="#A8683E", scale=7)
woodD = M.get("wood_dark", "#8B5A3C", rough=0.6, kind="wood", a="#9C6B48", b="#6E4630", scale=7)
hide = M.get("hide", "#F2E2C4", rough=0.6, kind="plain")
ink = M.get("ink", "#2E2A4A", rough=0.4)
seat = M.get("seat", "#2E2A4A", rough=0.7)
PITCH = ["#FF5A5F", "#FFA62B", "#FFE156", "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0", "#FF5A5F"]
models = []


def model(name):
    m = L.Model(name)
    models.append(m)
    return m


def drum(m, node, r, depth, centre, axis, shell, hoop=chrome, lugs=6):
    """Shell + two heads + hoops + lugs. axis 'y' (snare/tom, head up) or 'z' (bass drum facing the player)."""
    cx, cy, cz = centre
    m.add(L.cyl(f"{node}Shell", r, depth, centre, axis=axis, segs=32, bevel=0.015), node, "P", shell)
    for s in (-1, 1):
        off = (0, s * depth / 2, 0) if axis == "y" else (0, 0, s * depth / 2)
        p = (cx + off[0], cy + off[1], cz + off[2])
        m.add(L.cyl(f"{node}Head{s}", r * 0.97, 0.012, p, axis=axis, segs=32, bevel=0.004), node, "P", skin)
        m.add(L.tube_arc(f"{node}Hoop{s}", r, 0.016, p, 0, 360, segs=32, ring=8,
                         euler=(90, 0, 0) if axis == "y" else (0, 0, 0)), node, "P", hoop)
    for i in range(lugs):
        a = i / lugs * math.tau
        if axis == "y":
            p = (cx + math.cos(a) * (r + 0.012), cy, cz + math.sin(a) * (r + 0.012))
        else:
            p = (cx + math.cos(a) * (r + 0.012), cy + math.sin(a) * (r + 0.012), cz)
        m.add(L.rbox(f"{node}Lug{i}", (0.03, 0.05 if axis == "y" else 0.03, 0.03 if axis == "y" else 0.05), p, 0.01, segs=1), node, "P", chrome)


def stand(m, node, x, z, height, legs=3, top=None):
    m.add(L.cyl(f"{node}Pole", 0.014, height, (x, height / 2, z), axis="y", segs=10, bevel=0.004), "Body", "P", chrome)
    for i in range(legs):
        a = i / legs * math.tau + 0.3
        ex, ez = x + math.cos(a) * 0.22, z + math.sin(a) * 0.22
        m.add(L.sweep(f"{node}Leg{i}", [(x, 0.28, z), (ex, 0.01, ez)], L.circle(0.01, 6)), "Body", "P", chrome)
        m.add(L.sphere(f"{node}Foot{i}", 0.018, (ex, 0.012, ez), segs=8, rings=5), "Body", "P", ink)


def cymbal(m, node, r, centre, tilt=10):
    m.add(L.lathe(f"{node}Disc", [(0.0, 0.03), (0.06 * r / 0.2, 0.028), (0.08 * r / 0.2, 0.012), (r, 0.0), (r, -0.004),
                                  (0.08 * r / 0.2, 0.006), (0.0, 0.012)], centre, axis="y", segs=36, euler=(tilt, 0, 0)), node, "P", brass)


# ------------------------------------------------------------------ Drum kit (GM: kick 36 snare 38 hats 42/46 toms 45/48/50 crash 49 ride 51)
k = model("DrumKit")
n = k.node("Pad_36", "Body", (0, 0.32, 0.25))
drum(k, n, 0.3, 0.38, (0, 0.32, 0.25), "z", shellR, lugs=8)
k.add(L.cyl("KickLogo", 0.12, 0.004, (0, 0.32, 0.055), axis="z", segs=24, bevel=0.0), n, "P", brass)
for sx in (-1, 1):
    k.add(L.sweep(f"KickSpur{sx}", [(sx * 0.24, 0.12, 0.2), (sx * 0.36, 0.0, 0.12)], L.circle(0.012, 6)), "Body", "P", chrome)
n = k.node("Pad_38", "Body", (-0.45, 0.6, -0.05))
drum(k, n, 0.17, 0.14, (-0.45, 0.6, -0.05), "y", shellR)
stand(k, "SnareStand", -0.45, -0.05, 0.53)
for i, (note, x, y, z, r) in enumerate(((50, -0.17, 0.78, 0.22, 0.13), (48, 0.17, 0.78, 0.22, 0.14), (45, 0.56, 0.5, 0.0, 0.19))):
    n = k.node(f"Pad_{note}", "Body", (x, y, z))
    drum(k, n, r, 0.16 if note != 45 else 0.32, (x, y, z), "y", shellR)
for i in range(3):
    a = i / 3 * math.tau
    k.add(L.cyl(f"FloorLeg{i}", 0.012, 0.36, (0.56 + math.cos(a) * 0.2, 0.18, math.sin(a) * 0.2), axis="y", segs=8, bevel=0.003), "Body", "P", chrome)
n = k.node("Pad_42", "Body", (-0.78, 0.82, 0.0))
stand(k, "HatStand", -0.78, 0.0, 0.8)
cymbal(k, n, 0.17, (-0.78, 0.84, 0.0), 0)
cymbal(k, n, 0.17, (-0.78, 0.81, 0.0), 0)
k.node("Pad_46", "Pad_42", (0, 0, 0))
n = k.node("Pad_49", "Body", (-0.55, 1.12, 0.35))
stand(k, "CrashStand", -0.55, 0.35, 1.1)
cymbal(k, n, 0.22, (-0.55, 1.12, 0.35), 14)
n = k.node("Pad_51", "Body", (0.78, 1.0, 0.35))
stand(k, "RideStand", 0.78, 0.35, 0.98)
cymbal(k, n, 0.25, (0.78, 1.0, 0.35), -12)
k.add(L.cyl("Stool", 0.17, 0.08, (0, 0.46, -0.55), axis="y", segs=24, bevel=0.03), "Body", "P", seat)
stand(k, "StoolStand", 0.0, -0.55, 0.43)

# ------------------------------------------------------------------ single drums
s = model("Snare")
n = s.node("Pad_38", "Body", (0, 0.72, 0))
drum(s, n, 0.22, 0.16, (0, 0.72, 0), "y", shellO, lugs=8)
s.node("Pad_40", "Pad_38", (0, 0, 0))
stand(s, "Stand", 0, 0, 0.64)
for i, x in enumerate((-0.1, 0.1)):  # sticks resting on top
    s.add(L.cyl(f"Stick{i}", 0.012, 0.42, (x, 0.82, 0.02), axis="z", segs=8, bevel=0.004, radius2=0.008, euler=(0, 12 - 24 * i, 0)), "Body", "P", wood)

b = model("BassDrum")
n = b.node("Pad_36", "Body", (0, 0.55, 0))
drum(b, n, 0.5, 0.42, (0, 0.55, 0), "z", shellR, hoop=brass, lugs=10)
b.add(L.cyl("Star", 0.2, 0.004, (0, 0.55, -0.212), axis="z", segs=5, bevel=0.0), n, "P", brass)
for sx in (-1, 1):
    b.add(L.rbox(f"Cradle{sx}", (0.08, 0.12, 0.5), (sx * 0.32, 0.06, 0), 0.03), "Body", "P", woodD)
b.add(L.cyl("Mallet", 0.012, 0.5, (0.42, 0.35, -0.35), axis="y", segs=8, bevel=0.004, euler=(0, 0, 20)), "Body", "P", wood)
b.add(L.sphere("MalletHead", 0.06, (0.33, 0.6, -0.35), segs=12, rings=8), "Body", "P", hide)

c = model("Cymbals")
for note, x, r, tilt, h in ((49, -0.32, 0.24, 14, 1.15), (51, 0.32, 0.27, -12, 1.0), (53, 0.32, 0.27, -12, 1.0), (57, -0.32, 0.24, 14, 1.15)):
    if note in (53, 57):
        c.node(f"Pad_{note}", f"Pad_{51 if note == 53 else 49}", (0, 0, 0))
        continue
    n = c.node(f"Pad_{note}", "Body", (x, h, 0))
    stand(c, f"Stand{note}", x, 0, h - 0.02)
    cymbal(c, n, r, (x, h, 0), tilt)

bo = model("Bongos")
for note, x, r in ((60, -0.12, 0.11), (61, 0.15, 0.14)):
    n = bo.node(f"Pad_{note}", "Body", (x, 0.62, 0))
    bo.add(L.lathe(f"Bongo{note}", [(0.0, 0.0), (r * 0.75, 0.0), (r * 0.82, 0.04), (r, 0.18), (r * 1.02, 0.2), (0.0, 0.2)], (x, 0.42, 0),
                   axis="y", segs=28), n, "P", wood)
    bo.add(L.cyl(f"BongoHead{note}", r * 1.0, 0.012, (x, 0.622, 0), axis="y", segs=28, bevel=0.004), n, "P", hide)
    bo.add(L.tube_arc(f"BongoRim{note}", r * 1.02, 0.014, (x, 0.618, 0), 0, 360, segs=28, ring=8, euler=(90, 0, 0)), n, "P", brass)
bo.add(L.rbox("BongoBridge", (0.12, 0.06, 0.06), (0.015, 0.52, 0), 0.02), "Body", "P", woodD)
stand(bo, "Stand", 0.015, 0, 0.42)

co = model("Congas")
for i, (note, x, r, h) in enumerate(((63, -0.38, 0.13, 0.82), (64, 0.0, 0.15, 0.9), (65, 0.4, 0.17, 0.86))):
    n = co.node(f"Pad_{note}", "Body", (x, h, 0))
    prof = [(0.0, 0.0), (r * 0.8, 0.0), (r * 0.95, h * 0.2), (r * 1.25, h * 0.55), (r * 1.05, h * 0.92), (r, h), (0.0, h)]
    co.add(L.lathe(f"Conga{note}", prof, (x, 0, 0), axis="y", segs=30), n, "P", [shellO, shellR, woodD][i])
    co.add(L.cyl(f"CongaHead{note}", r * 1.02, 0.014, (x, h + 0.005, 0), axis="y", segs=30, bevel=0.004), n, "P", hide)
    co.add(L.tube_arc(f"CongaRim{note}", r * 1.03, 0.017, (x, h, 0), 0, 360, segs=30, ring=8, euler=(90, 0, 0)), n, "P", chrome)
co.node("Pad_62", "Pad_63", (0, 0, 0))

t = model("Tambourine")
n = t.node("Pad_54", "Body", (0, 0.9, 0))
t.node("Pad_55", "Pad_54", (0, 0, 0))
t.node("Pad_56", "Pad_54", (0, 0, 0))
t.add(L.tube_arc("TambRing", 0.2, 0.035, (0, 0.9, 0), 0, 360, segs=36, ring=10, euler=(0, 0, 0)), n, "P", wood)
t.add(L.cyl("TambHead", 0.19, 0.01, (0, 0.9, 0.0), axis="z", segs=32, bevel=0.003), n, "P", skin)
for i in range(6):
    a = i / 6 * math.tau + 0.26
    for dz in (-0.012, 0.012):
        t.add(L.cyl(f"Zil{i}{dz}", 0.036, 0.006, (math.cos(a) * 0.2, 0.9 + math.sin(a) * 0.2, dz), axis="z", segs=16, bevel=0.002), n, "P", brass)
t.add(L.cyl("TambHandle", 0.02, 0.6, (0, 0.33, 0.06), axis="y", segs=10, bevel=0.005), "Body", "P", wood)
t.add(L.cyl("TambBase", 0.15, 0.04, (0, 0.02, 0.06), axis="y", segs=20, bevel=0.012), "Body", "P", woodD)

ma = model("Maracas")
for i, (note, x, col) in enumerate(((70, -0.12, "#FF5A8A"), (82, 0.12, "#FFC93C"))):
    n = ma.node(f"Pad_{note}", "Body", (x, 0.55, 0))
    ma.add(L.sphere(f"MaracaHead{note}", 0.1, (x, 0.62, 0), scale=(1, 1.25, 1), segs=20, rings=12), n, "P", M.get(f"mar{i}", col, rough=0.3))
    ma.add(L.tube_arc(f"MaracaBand{note}", 0.1, 0.012, (x, 0.62, 0), 0, 360, segs=24, ring=6, euler=(90, 0, 0)), n, "P", M.get(f"band{i}", PITCH[4 + i], rough=0.3))
    ma.add(L.cyl(f"MaracaHandle{note}", 0.022, 0.25, (x, 0.44, 0), axis="y", segs=10, bevel=0.006, radius2=0.03), n, "P", wood)
ma.add(L.rbox("MaracaStand", (0.5, 0.08, 0.2), (0, 0.29, 0), 0.03), "Body", "P", woodD)

x = model("Xylophone")
notes = [72, 74, 76, 77, 79, 81, 83, 84]
for i, nt in enumerate(notes):
    length = 0.5 - i * 0.035
    px = -0.42 + i * 0.12
    n = x.node(f"Pad_{nt}", "Body", (px, 0.72, 0))
    x.add(L.rbox(f"Bar{nt}", (0.1, 0.035, length), (px, 0.72, 0), 0.015), n, "P", M.get(f"bar{i}", PITCH[i], rough=0.35))
    x.add(L.cyl(f"Nail{nt}a", 0.008, 0.01, (px, 0.74, length * 0.32), axis="y", segs=6, bevel=0.0), n, "P", chrome)
for sz in (-1, 1):
    x.add(L.rbox(f"Rail{sz}", (1.08, 0.04, 0.05), (0, 0.685, sz * 0.15), 0.015, euler=(0, sz * 4, 0)), "Body", "P", woodD)
for sx in (-1, 1):
    x.add(L.rbox(f"Frame{sx}", (0.05, 0.62, 0.36), (sx * 0.56, 0.36, 0), 0.02), "Body", "P", wood)
x.add(L.rbox("Shelf", (1.1, 0.04, 0.3), (0, 0.18, 0), 0.015), "Body", "P", wood)
for i, sx in enumerate((-0.2, 0.2)):
    x.add(L.cyl(f"MalletS{i}", 0.01, 0.36, (sx, 0.79, -0.32), axis="x", segs=8, bevel=0.003, euler=(0, 30 - 60 * i, 0)), "Body", "P", wood)
    x.add(L.sphere(f"MalletB{i}", 0.035, (sx + (0.15 if i == 0 else -0.15), 0.79, -0.24), segs=10, rings=6), "Body", "P", M.get("malletball", "#FF5A8A", rough=0.4))

# ------------------------------------------------------------------ bake + export
group = L.Group("DrumInstruments", models)
tex, em = L.bake(group, size=2048, ao_samples=32, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material("M_DrumInstruments", tex)
for obj, node, slot in group.parts:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
for mdl in models:
    root, empties, meshes = L.build_hierarchy(mdl, mdl.name)
    L.export_fbx(root, f"{OUT}/{mdl.name}.fbx")
    print(f"[LMW] {mdl.name}: {sum(len(o.data.polygons) for o in meshes.values())} faces, {len([q for q in mdl.nodes if q.startswith('Pad_')])} pads")
L.save_source("DrumInstruments")
if PREVIEW:
    import bpy
    for i, mdl in enumerate(models):  # spread out for one review render
        for o in bpy.data.objects:
            if o.name == mdl.name:
                o.location = L.U((i % 5) * 1.6 - 3.2, 0, (i // 5) * 1.8)
    L.render_preview(L.RENDER_DIR + "/DrumInstruments", [((0.0, 3.2, -6.5), (0.0, 0.5, 0.9))], size=(1280, 720))
