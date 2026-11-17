"""Piano Paradise companions: Upright Piano (warm walnut, candle sconces) and Digital Keyboard (teal, on an X-stand).
Keys are pivoted nodes "Key_<midi>" (like the Grand Piano) so PianoModel3D animates them. Shared atlas
T_KeyInstruments. Keyboards face −Z.
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
walnut = M.get("walnut", "#9C5B3A", rough=0.35, kind="wood", a="#A8683E", b="#7C4429", scale=6)
walnutD = M.get("walnutD", "#6E3E25", rough=0.4)
ivory = M.get("ivory", "#FFFBF3", rough=0.32)
ebony = M.get("ebony", "#24213A", rough=0.3)
brass = M.get("brass", "#F2C14E", rough=0.25)
felt = M.get("felt", "#C0392B", rough=0.95)
teal = M.get("teal", "#2EC4B6", rough=0.3)
tealD = M.get("tealD", "#1F8F86", rough=0.35)
grey = M.get("grey", "#3D405B", rough=0.4)
screen = M.get("screen", "#9BE8FF", rough=0.1, emit=1.2)
grille = M.get("grille", "#2E2A4A", rough=0.7, kind="rings", a="#2E2A4A", b="#45405F", scale=40)
PITCH = ["#FF5A5F", "#FFA62B", "#FFE156", "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0"]
models = []


def keys(m, low, high, centre_x, top_y, back_z, key_w, key_l, key_h, white_mat=ivory, black_mat=ebony):
    whites = [n for n in range(low, high + 1) if n % 12 in (0, 2, 4, 5, 7, 9, 11)]
    x0 = centre_x - len(whites) * key_w / 2
    for i, n in enumerate(whites):
        x = x0 + key_w * (i + 0.5)
        node = m.node(f"Key_{n}", "Body", (x, top_y, back_z))
        m.add(L.rbox(f"W{n}", (key_w - 0.003, key_h, key_l), (x, top_y - key_h / 2, back_z - key_l / 2), 0.007, segs=2), node, "P", white_mat)
    for n in range(low, high + 1):
        if n % 12 not in (1, 3, 6, 8, 10):
            continue
        x = x0 + key_w * (whites.index(n - 1) + 1)
        node = m.node(f"Key_{n}", "Body", (x, top_y + 0.02, back_z))
        m.add(L.rbox(f"B{n}", (key_w * 0.58, 0.03, key_l * 0.6), (x, top_y + 0.004, back_z - key_l * 0.3), 0.005, segs=2,
                     top=(0.85, 0.92)), node, "P", black_mat)
    return len(whites) * key_w


# ------------------------------------------------------------------ Upright piano (C3–C6, 22 white keys)
u = L.Model("UprightPiano")
models.append(u)
kw = keys(u, 48, 84, 0.0, 0.74, -0.02, 0.05, 0.24, 0.032)
W = kw + 0.26
u.add(L.rbox("Case", (W, 1.18, 0.5), (0, 0.69, 0.22), 0.04), "Body", "P", walnut)
u.add(L.rbox("Top", (W + 0.06, 0.05, 0.56), (0, 1.3, 0.22), 0.02), "Body", "P", walnutD)
u.add(L.rbox("KeyShelf", (W, 0.07, 0.34), (0, 0.67, -0.12), 0.02), "Body", "P", walnut)
for sx in (-1, 1):
    u.add(L.rbox(f"Cheek{sx}", (0.1, 0.14, 0.36), (sx * (kw / 2 + 0.06), 0.74, -0.12), 0.03), "Body", "P", walnut)
    u.add(L.rbox(f"Toe{sx}", (0.08, 0.1, 0.42), (sx * (W / 2 - 0.08), 0.05, -0.08), 0.03), "Body", "P", walnutD)
    u.add(L.lathe(f"Leg{sx}", [(0.0, 0.0), (0.04, 0.0), (0.05, 0.1), (0.035, 0.3), (0.05, 0.5), (0.0, 0.52)], (sx * (W / 2 - 0.08), 0.1, -0.22),
                  axis="y", segs=14), "Body", "P", walnut)
    u.add(L.cyl(f"SconceArm{sx}", 0.012, 0.18, (sx * (W / 2 - 0.15), 1.0, -0.1), axis="z", segs=8, bevel=0.003), "Body", "P", brass)
    u.add(L.cyl(f"Candle{sx}", 0.02, 0.12, (sx * (W / 2 - 0.15), 1.07, -0.18), axis="y", segs=10, bevel=0.005), "Body", "P", ivory)
    u.add(L.sphere(f"Flame{sx}", 0.018, (sx * (W / 2 - 0.15), 1.15, -0.18), scale=(0.8, 1.4, 0.8), segs=10, rings=6), "Body", "P",
          M.get("flame", "#FFD27A", rough=0.2, emit=3.0))
u.add(L.rbox("Fallboard", (kw + 0.02, 0.12, 0.03), (0, 0.81, 0.0), 0.012, euler=(-10, 0, 0)), "Body", "P", walnutD)
u.add(L.rbox("Desk", (0.7, 0.26, 0.03), (0, 1.0, -0.04), 0.012, euler=(-12, 0, 0)), "Body", "P", walnutD)
u.add(L.rbox("Felt", (kw, 0.01, 0.02), (0, 0.718, -0.005), 0.003, segs=1), "Body", "P", felt)
for i in range(3):  # carved panel ovals
    u.add(L.cyl(f"Panel{i}", 0.12, 0.012, (-0.42 + i * 0.42, 0.33, -0.035), axis="z", segs=24, bevel=0.004), "Body", "P", walnutD)
for i, x in enumerate((-0.07, 0.07)):
    u.add(L.rbox(f"Pedal{i}", (0.045, 0.02, 0.12), (x, 0.05, -0.08), 0.008), "Body", "P", brass)

# ------------------------------------------------------------------ Digital keyboard (C3–C6) on an X-stand
d = L.Model("DigitalKeyboard")
models.append(d)
kw = keys(d, 48, 84, 0.0, 0.86, 0.0, 0.042, 0.17, 0.026)
Wd = kw + 0.18
d.add(L.rbox("KbBody", (Wd, 0.09, 0.36), (0, 0.82, 0.04), 0.035), "Body", "P", teal)
d.add(L.rbox("KbPanel", (Wd - 0.04, 0.02, 0.15), (0, 0.875, 0.15), 0.02), "Body", "P", tealD)
d.add(L.rbox("Screen", (0.22, 0.012, 0.08), (0, 0.888, 0.15), 0.01), "Body", "P", screen)
for sx in (-1, 1):
    d.add(L.cyl(f"Speaker{sx}", 0.06, 0.012, (sx * (Wd / 2 - 0.16), 0.888, 0.15), axis="y", segs=24, bevel=0.004), "Body", "P", grille)
for i in range(8):
    x = -0.38 + i * 0.075 + (0.3 if i >= 4 else 0)
    d.add(L.cyl(f"Btn{i}", 0.016, 0.014, (x, 0.888, 0.12 if i % 2 else 0.18), axis="y", segs=12, bevel=0.004), "Body", "P",
          M.get(f"btn{i}", PITCH[i % 7], rough=0.3))
for sx in (-1, 1):
    for a in (-1, 1):
        d.add(L.rbox(f"XLeg{sx}{a}", (0.04, 0.95, 0.04), (sx * 0.32, 0.4, 0.04), 0.015, euler=(a * 32, 0, 0)), "Body", "P", grey)
    d.add(L.cyl(f"XBolt{sx}", 0.025, 0.06, (sx * 0.32, 0.4, 0.04), axis="x", segs=12, bevel=0.006), "Body", "P", tealD)
d.add(L.rbox("XBar", (0.7, 0.04, 0.04), (0, 0.77, 0.04), 0.015), "Body", "P", grey)

group = L.Group("KeyInstruments", models)
tex, em = L.bake(group, size=1024, ao_samples=32, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material("M_KeyInstruments", tex, em, 1.0)
for obj, node, slot in group.parts:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
for mdl in models:
    root, empties, meshes = L.build_hierarchy(mdl, mdl.name)
    L.export_fbx(root, f"{OUT}/{mdl.name}.fbx")
    print(f"[LMW] {mdl.name}: {sum(len(o.data.polygons) for o in meshes.values())} faces, "
          f"{len([q for q in mdl.nodes if q.startswith('Key_')])} keys")
L.save_source("KeyInstruments")
if PREVIEW:
    import bpy
    for i, mdl in enumerate(models):
        for o in bpy.data.objects:
            if o.name == mdl.name:
                o.location = L.U(i * 2.0 - 1.0, 0, 0)
    L.render_preview(L.RENDER_DIR + "/KeyInstruments", [((0.6, 1.8, -3.6), (0.0, 0.7, 0.0))], size=(1280, 720))
