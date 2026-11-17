"""Wind Village instruments: Flute, Trumpet, Saxophone, Clarinet, Trombone, Recorder — shared atlas T_WindInstruments.
Finger keys/holes/valves are nodes "Key_<i>" (and the trombone slide "Slide") so Unity can press the exact control
while the note sounds. Displayed on stands, mouthpieces up/left, fronts −Z.
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
silver = M.get("silver", "#E3E8F0", rough=0.15)
brass = M.get("brass", "#F2C14E", rough=0.18)
brassD = M.get("brassdark", "#C98F2A", rough=0.25)
black = M.get("clarinet", "#2E2A4A", rough=0.3)
cream = M.get("recorder", "#FFF1DC", rough=0.35)
pad = M.get("pad", "#FFF4E0", rough=0.6)
ink = M.get("ink", "#2E2A4A", rough=0.4)
wood = M.get("standwood", "#A8683E", rough=0.6)
PITCH = ["#FF5A5F", "#FFA62B", "#FFE156", "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0", "#FF5A5F"]
models = []


def model(name):
    m = L.Model(name)
    models.append(m)
    return m


def stand(m, x=0.0, z=0.0, h=0.4):
    m.add(L.cyl("StandBase", 0.16, 0.04, (x, 0.02, z), axis="y", segs=20, bevel=0.012), "Body", "P", wood)
    m.add(L.cyl("StandPost", 0.018, h, (x, h / 2 + 0.03, z), axis="y", segs=10, bevel=0.004), "Body", "P", ink)


# ------------------------------------------------------------------ Flute (horizontal on a stand)
f = model("Flute")
fy = 0.62
f.add(L.cyl("FluteBody", 0.022, 1.0, (0, fy, 0), axis="x", segs=16, bevel=0.004), "Body", "P", silver)
f.add(L.cyl("FluteHead", 0.024, 0.08, (-0.46, fy, 0), axis="x", segs=16, bevel=0.006), "Body", "P", silver)
f.add(L.rbox("LipPlate", (0.05, 0.008, 0.035), (-0.4, fy + 0.024, -0.005), 0.006), "Body", "P", silver)
for i in range(8):
    n = f.node(f"Key_{i}", "Body", (-0.15 + i * 0.07, fy + 0.025, 0))
    f.add(L.cyl(f"FKey{i}", 0.016, 0.01, (-0.15 + i * 0.07, fy + 0.025, 0), axis="y", segs=14, bevel=0.003), n, "P", silver)
    f.add(L.cyl(f"FDot{i}", 0.008, 0.012, (-0.15 + i * 0.07, fy + 0.029, 0), axis="y", segs=10, bevel=0.002), n, "P",
          M.get(f"kd{i}", PITCH[i], rough=0.3))
for x in (-0.25, 0.25):
    f.add(L.cyl(f"Cradle{x}", 0.012, fy - 0.03, (x, (fy - 0.03) / 2, 0), axis="y", segs=8, bevel=0.003), "Body", "P", ink)
f.add(L.rbox("FluteBase", (0.7, 0.04, 0.16), (0, 0.02, 0), 0.015), "Body", "P", wood)

# ------------------------------------------------------------------ Trumpet (bell toward the player)
t = model("Trumpet")
ty = 0.7
t.add(L.lathe("Bell", [(0.012, 0.0), (0.014, 0.12), (0.022, 0.24), (0.045, 0.31), (0.085, 0.345), (0.088, 0.35), (0.0, 0.35)],
              (0.0, ty, -0.12), axis="z", segs=30), "Body", "P", brass)
loop = [(0.0, ty, 0.06), (0.0, ty, 0.24), (0.0, ty - 0.07, 0.3), (0.0, ty - 0.12, 0.22), (0.0, ty - 0.12, 0.05),
        (0.0, ty - 0.07, -0.02)]
t.add(L.sweep("Tubing", loop, L.circle(0.013, 10), ref=(1, 0, 0)), "Body", "P", brass)
t.add(L.cyl("LeadPipe", 0.011, 0.36, (0.045, ty + 0.02, 0.12), axis="z", segs=10, bevel=0.003), "Body", "P", brass)
t.add(L.cyl("Mouthpiece", 0.016, 0.06, (0.045, ty + 0.02, 0.32), axis="z", segs=12, bevel=0.004, radius2=0.008), "Body", "P", silver)
for i in range(3):
    z = 0.06 + i * 0.045
    n = t.node(f"Key_{i}", "Body", (0.0, ty + 0.06, z))
    t.add(L.cyl(f"Valve{i}", 0.018, 0.12, (0.0, ty, z), axis="y", segs=14, bevel=0.004), "Body", "P", brassD)
    t.add(L.cyl(f"ValveCap{i}", 0.02, 0.025, (0.0, ty + 0.075, z), axis="y", segs=14, bevel=0.006), n, "P",
          M.get(f"vc{i}", PITCH[i * 2], rough=0.3))
stand(t, 0.0, 0.08, ty - 0.12)

# ------------------------------------------------------------------ Saxophone (upright)
s = model("Saxophone")
path = [(0.0, 1.05, 0.0), (0.0, 0.95, 0.02), (0.0, 0.7, 0.02), (0.0, 0.42, 0.02), (0.0, 0.26, 0.05), (0.0, 0.22, 0.13),
        (0.0, 0.3, 0.2), (0.0, 0.45, 0.22)]
s.add(L.sweep("SaxBody", path, L.circle(0.03, 14), ref=(1, 0, 0), taper=lambda t: (0.55 + 0.9 * t, 0.55 + 0.9 * t)), "Body", "P", brass)
s.add(L.lathe("SaxBell", [(0.045, 0.0), (0.06, 0.05), (0.095, 0.1), (0.1, 0.11), (0.0, 0.11)], (0.0, 0.43, 0.22), axis="y",
              segs=28), "Body", "P", brass)
s.add(L.sweep("SaxNeck", [(0.0, 1.05, 0.0), (0.0, 1.13, -0.04), (0.0, 1.15, -0.1)], L.circle(0.012, 10), ref=(1, 0, 0)), "Body", "P", brass)
s.add(L.cyl("SaxMouth", 0.014, 0.06, (0.0, 1.15, -0.14), axis="z", segs=10, bevel=0.004, radius2=0.008), "Body", "P", ink)
for i in range(8):
    y = 0.95 - i * 0.075
    n = s.node(f"Key_{i}", "Body", (0.0, y, -0.03))
    s.add(L.cyl(f"SKey{i}", 0.02, 0.008, (0.0, y, -0.03), axis="z", segs=14, bevel=0.003), n, "P", pad)
    s.add(L.cyl(f"SDot{i}", 0.009, 0.01, (0.0, y, -0.036), axis="z", segs=10, bevel=0.002), n, "P", M.get(f"sd{i}", PITCH[i], rough=0.3))
stand(s, 0.0, 0.08, 0.2)

# ------------------------------------------------------------------ Clarinet (upright, bell down)
c = model("Clarinet")
c.add(L.cyl("ClarBody", 0.019, 0.62, (0, 0.52, 0), axis="y", segs=16, bevel=0.004), "Body", "P", black)
c.add(L.lathe("ClarBell", [(0.019, 0.0), (0.024, -0.05), (0.045, -0.11), (0.05, -0.12), (0.0, -0.12)], (0, 0.21, 0), axis="y", segs=24), "Body", "P", black)
c.add(L.cyl("ClarBarrel", 0.022, 0.06, (0, 0.86, 0), axis="y", segs=16, bevel=0.006), "Body", "P", black)
c.add(L.cyl("ClarMouth", 0.017, 0.08, (0, 0.93, 0), axis="y", segs=14, bevel=0.004, radius2=0.008), "Body", "P", ink)
for ring_y in (0.25, 0.52, 0.83):
    c.add(L.tube_arc(f"Ring{ring_y}", 0.021, 0.004, (0, ring_y, 0), 0, 360, segs=20, ring=6, euler=(90, 0, 0)), "Body", "P", silver)
for i in range(8):
    y = 0.78 - i * 0.065
    n = c.node(f"Key_{i}", "Body", (0, y, -0.02))
    c.add(L.cyl(f"CKey{i}", 0.011, 0.008, (0, y, -0.02), axis="z", segs=12, bevel=0.002), n, "P", silver)
    c.add(L.cyl(f"CDot{i}", 0.006, 0.01, (0, y, -0.025), axis="z", segs=8, bevel=0.002), n, "P", M.get(f"cd{i}", PITCH[i], rough=0.3))
c.add(L.cyl("ClarStandBase", 0.13, 0.04, (0, 0.02, 0), axis="y", segs=20, bevel=0.012), "Body", "P", wood)
c.add(L.lathe("ClarPeg", [(0.0, 0.0), (0.03, 0.0), (0.012, 0.12), (0.0, 0.12)], (0, 0.04, 0), axis="y", segs=12), "Body", "P", ink)

# ------------------------------------------------------------------ Trombone (slide extends toward −Z)
tb = model("Trombone")
ty = 0.75
tb.add(L.lathe("TbBell", [(0.014, 0.0), (0.018, 0.2), (0.04, 0.33), (0.1, 0.38), (0.105, 0.39), (0.0, 0.39)], (0.07, ty, 0.06),
               axis="z", segs=30, euler=(180, 0, 0)), "Body", "P", brass)
tb.add(L.sweep("TbCrook", [(0.07, ty, 0.06), (0.07, ty, 0.25), (0.035, ty, 0.3), (0.0, ty, 0.25), (0.0, ty, 0.05)],
               L.circle(0.012, 10), ref=(0, 1, 0)), "Body", "P", brass)
sl = tb.node("Slide", "Body", (0.0, ty, 0.0))
for x in (0.0, 0.07):
    tb.add(L.cyl(f"SlideTube{x}", 0.011, 0.55, (x - 0.035 + 0.035, ty, -0.27), axis="z", segs=10, bevel=0.003), sl, "P", brass)
tb.add(L.sweep("SlideU", [(0.0, ty, -0.54), (0.0, ty, -0.58), (0.035, ty, -0.6), (0.07, ty, -0.58), (0.07, ty, -0.54)],
               L.circle(0.013, 10), ref=(0, 1, 0)), sl, "P", brass)
tb.add(L.cyl("TbMouth", 0.016, 0.06, (0.0, ty, 0.08), axis="z", segs=12, bevel=0.004, radius2=0.008), "Body", "P", silver)
for i in range(7):
    tb.node(f"Key_{i}", "Slide", (0, 0, 0))  # slide positions share the slide node
stand(tb, 0.035, 0.0, ty - 0.06)

# ------------------------------------------------------------------ Recorder (upright)
r = model("Recorder")
r.add(L.lathe("RecBody", [(0.0, 0.0), (0.028, 0.0), (0.03, 0.02), (0.02, 0.1), (0.017, 0.35), (0.021, 0.42), (0.024, 0.48),
                          (0.022, 0.52), (0.012, 0.56), (0.0, 0.56)], (0, 0.18, 0), axis="y", segs=20), "Body", "P", cream)
r.add(L.rbox("RecWindow", (0.014, 0.022, 0.006), (0, 0.66, -0.022), 0.003), "Body", "P", ink)
for i in range(8):
    y = 0.6 - i * 0.045
    n = r.node(f"Key_{i}", "Body", (0, y, -0.019))
    r.add(L.cyl(f"Hole{i}", 0.008, 0.008, (0, y, -0.019), axis="z", segs=10, bevel=0.002), n, "P", ink)
    r.add(L.tube_arc(f"HoleRing{i}", 0.011, 0.003, (0, y, -0.02), 0, 360, segs=14, ring=5), n, "P", M.get(f"rh{i}", PITCH[i], rough=0.3))
r.add(L.cyl("RecStandBase", 0.12, 0.04, (0, 0.02, 0), axis="y", segs=20, bevel=0.012), "Body", "P", wood)
r.add(L.lathe("RecPeg", [(0.0, 0.0), (0.028, 0.0), (0.012, 0.16), (0.0, 0.16)], (0, 0.04, 0), axis="y", segs=12), "Body", "P", ink)

# ------------------------------------------------------------------ bake + export
group = L.Group("WindInstruments", models)
tex, em = L.bake(group, size=1024, ao_samples=32, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material("M_WindInstruments", tex)
for obj, node, slot in group.parts:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
for mdl in models:
    root, empties, meshes = L.build_hierarchy(mdl, mdl.name)
    L.export_fbx(root, f"{OUT}/{mdl.name}.fbx")
    print(f"[LMW] {mdl.name}: {sum(len(o.data.polygons) for o in meshes.values())} faces")
L.save_source("WindInstruments")
if PREVIEW:
    import bpy
    for i, mdl in enumerate(models):
        for o in bpy.data.objects:
            if o.name == mdl.name:
                o.location = L.U(i * 0.75 - 1.9, 0, 0)
    L.render_preview(L.RENDER_DIR + "/WindInstruments", [((0.0, 1.3, -3.4), (0.0, 0.6, 0.0))], size=(1400, 700))
