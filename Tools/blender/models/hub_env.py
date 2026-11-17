"""Central Musical Hub — environment props (Art Bible §5, area key: cream / grass / gold).

Every prop is an original procedural model. All hub props share ONE baked 2048² atlas (T_HubProps) so Unity can
batch them with a single material; the ground has its own atlas. Each prop is exported as its own FBX with its pivot
at the base centre so the Unity scene builder can place, rotate and reuse it.
Props: Bandstand (landmark), GatePiano, LampNote, TreeRound, TreeBlossom, Bush, FlowerBed, Bench, Plaza, PathSegment,
HubGround. Outputs: Assets/Art/Environments/Hub/*.fbx, Assets/Art/Textures/T_HubProps.png, T_HubGround.png.
"""
import math
import os
import random
import sys

import bpy

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L  # noqa: E402

PREVIEW = "--preview" in sys.argv
OUT = L.ART_DIR + "/Environments/Hub"
L.reset()
M = L.Mats()
stone = M.get("stone", "#FFF1DC", rough=0.8, kind="plain")
stone_dark = M.get("stone_dark", "#E8D2B0", rough=0.85)
white = M.get("white", "#FFFBF3", rough=0.45)
gold = M.get("gold", "#F2C14E", rough=0.3)
coral = M.get("coral", "#FF6F5B", rough=0.5)
roof = M.get("roof", "#FFFBF3", rough=0.5, kind="stripes", a="#FF6F5B", b="#FFF4E0", scale=5.0)
ink = M.get("ink", "#2E2A4A", rough=0.35)
ivory = M.get("ivory", "#FFFBF3", rough=0.35)
wood = M.get("wood", "#C68B59", rough=0.6, kind="wood", a="#D9A06B", b="#A8683E", scale=6.0)
bark = M.get("bark", "#8B5A3C", rough=0.8, kind="bark", a="#9C6B48", b="#6E4630", scale=8.0)
leaf = M.get("leaf", "#7CCB5A", rough=0.75, kind="foliage", a="#4FA04A", b="#A6E07E", scale=2.5)
blossom = M.get("blossom", "#FFB4C2", rough=0.7, kind="foliage", a="#F28AA0", b="#FFD6DE", scale=2.5)
soil = M.get("soil", "#8E6A4E", rough=0.95)
lamp = M.get("lamp", "#FFE9A8", rough=0.2, emit=1.5)
glass = M.get("glassy", "#FFF7D6", rough=0.1, emit=0.6)
FLOWERS = [M.get(f"flower{i}", c, rough=0.6) for i, c in enumerate(("#FF5A5F", "#FFE156", "#D46CE0", "#FFFFFF", "#FFA62B"))]

models = []


def model(name):
    m = L.Model(name)
    models.append(m)
    return m


# ------------------------------------------------------------------ Bandstand (landmark)
bs = model("Bandstand")
bs.add(L.cyl("Base", 2.55, 0.42, (0, 0.21, 0), axis="y", segs=48, bevel=0.06), "Body", "P", stone)
bs.add(L.cyl("BaseTrim", 2.6, 0.08, (0, 0.38, 0), axis="y", segs=48, bevel=0.03), "Body", "P", gold)
for i in range(3):  # front steps
    bs.add(L.rbox(f"Step{i}", (1.6, 0.14, 0.35), (0, 0.07 + i * 0.14 - 0.0, -2.75 + i * 0.22), 0.05), "Body", "P", stone_dark)
cols = 8
for i in range(cols):
    a = i / cols * math.tau + math.pi / cols
    x, z = math.sin(a) * 2.15, math.cos(a) * 2.15
    if z < -1.8 and abs(x) < 0.9:
        continue  # leave the front open
    bs.add(L.lathe(f"Col{i}", [(0.0, 0.0), (0.16, 0.0), (0.16, 0.1), (0.1, 0.18), (0.085, 1.4), (0.1, 2.3), (0.15, 2.4),
                               (0.15, 2.5), (0.0, 2.5)], (x, 0.42, z), axis="y", segs=16), "Body", "P", white)
    bs.add(L.cyl(f"Cap{i}", 0.17, 0.08, (x, 2.95, z), axis="y", segs=16, bevel=0.02), "Body", "P", gold)
bs.add(L.cyl("Ring", 2.45, 0.22, (0, 3.02, 0), axis="y", segs=48, bevel=0.05), "Body", "P", white)
dome = [(2.75, 0.0), (2.6, 0.18), (2.1, 0.55), (1.4, 0.95), (0.7, 1.3), (0.25, 1.55), (0.0, 1.62)]
bs.add(L.lathe("Roof", [(0.0, 0.0)] + dome, (0, 3.1, 0), axis="y", segs=48), "Body", "P", roof)
for i in range(16):  # scalloped trim
    a = i / 16 * math.tau
    bs.add(L.sphere(f"Scallop{i}", 0.24, (math.sin(a) * 2.68, 3.1, math.cos(a) * 2.68), scale=(1.0, 0.55, 0.5), segs=12,
                    rings=8, euler=(0, math.degrees(a), 0)), "Body", "P", coral)
bs.add(L.sphere("Knob", 0.22, (0, 4.75, 0), segs=16, rings=10), "Body", "P", gold)
spiral = []
for i in range(40):
    t = i / 39
    a = t * math.pi * 2.6
    r = 0.5 * (1 - 0.7 * t)
    spiral.append((math.cos(a) * r * 0.15, 5.4 + math.sin(a) * r + 0.6 * (1 - t) - 0.3, math.cos(a) * r))
bs.add(L.sweep("Clef", spiral, L.circle(0.07, 10), ref=(1, 0, 0), taper=lambda t: (1 - 0.6 * t, 1 - 0.6 * t)), "Body", "P", gold)
bs.add(L.cyl("ClefStem", 0.07, 1.4, (0.0, 5.2, 0.0), axis="y", segs=10, bevel=0.02), "Body", "P", gold)
bs.add(L.sphere("ClefDot", 0.12, (0.0, 4.55, 0.32), segs=12, rings=8), "Body", "P", gold)

# ------------------------------------------------------------------ Piano Paradise gate (giant keys)
gp = model("GatePiano")
for side in (-1, 1):
    x0 = side * 1.9
    for k in range(5):  # pillar of stacked keys, alternating white/black
        black = k % 2 == 1
        gp.add(L.rbox(f"Pillar{side}_{k}", (0.75 if not black else 0.6, 0.55, 0.75 if not black else 0.6),
                      (x0, 0.3 + k * 0.58, 0), 0.08), "Body", "P", ink if black else ivory)
    gp.add(L.rbox(f"PillarCap{side}", (0.9, 0.18, 0.9), (x0, 3.2, 0), 0.06), "Body", "P", gold)
arch = []
for i in range(13):
    a = math.pi * i / 12
    arch.append((math.cos(a) * 1.9, 3.3 + math.sin(a) * 1.25, 0.0))
for i in range(12):
    (x0, y0, _), (x1, y1, _) = arch[i], arch[i + 1]
    ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
    black = i % 2 == 1
    gp.add(L.rbox(f"ArchKey{i}", (math.hypot(x1 - x0, y1 - y0) + 0.05, 0.42, 0.62 if not black else 0.5),
                  ((x0 + x1) / 2, (y0 + y1) / 2, 0), 0.06, euler=(0, 0, ang)), "Body", "P", ink if black else ivory)
gp.add(L.rbox("Sign", (1.6, 0.8, 0.14), (0, 3.55, -0.28), 0.12), "Body", "P", M.get("lilac", "#C9B8FF", rough=0.5))
gp.add(L.rbox("SignKeysW", (1.2, 0.3, 0.06), (0, 3.45, -0.37), 0.04), "Body", "P", ivory)
for i in range(4):
    gp.add(L.rbox(f"SignKeyB{i}", (0.1, 0.2, 0.05), (-0.39 + i * 0.26, 3.52, -0.41), 0.02), "Body", "P", ink)
gp.add(L.sphere("SignNote", 0.12, (0, 3.83, -0.37), scale=(1.3, 1, 0.5), segs=12, rings=8), "Body", "P", gold)

# ------------------------------------------------------------------ Lamp post with a note-shaped head
lp = model("LampNote")
lp.add(L.cyl("LampBase", 0.24, 0.2, (0, 0.1, 0), axis="y", segs=18, bevel=0.05), "Body", "P", ink)
lp.add(L.cyl("LampPole", 0.07, 2.6, (0, 1.4, 0), axis="y", segs=12, bevel=0.02), "Body", "P", ink)
lp.add(L.sphere("LampBulb", 0.3, (-0.18, 2.8, 0), scale=(1.25, 0.95, 0.95), segs=20, rings=12, euler=(0, 0, 20)), "Body", "P", lamp)
lp.add(L.sweep("LampFlag", [(0.06, 2.7, 0), (0.12, 3.05, 0), (0.42, 3.15, 0), (0.48, 2.85, 0)], L.circle(0.06, 10),
               ref=(0, 0, 1), taper=lambda t: (1.0, 1.0 - 0.5 * t)), "Body", "P", ink)
lp.add(L.cyl("LampRing", 0.12, 0.08, (0, 2.68, 0), axis="y", segs=14, bevel=0.02), "Body", "P", gold)

# ------------------------------------------------------------------ Trees, bush, flowers, bench
def tree(name, canopy_mat, seed, tall=1.0):
    t = model(name)
    t.add(L.lathe("Trunk", [(0.0, 0.0), (0.28, 0.0), (0.2, 0.25), (0.16, 1.2 * tall), (0.12, 1.9 * tall), (0.0, 1.95 * tall)],
                  (0, 0, 0), axis="y", segs=12), "Body", "P", bark)
    rnd = random.Random(seed)
    balls = [((0, 2.3 * tall, 0), 1.0)]
    for i in range(6):
        a = i / 6 * math.tau + rnd.uniform(-0.3, 0.3)
        balls.append(((math.cos(a) * 0.75, 2.2 * tall + rnd.uniform(-0.2, 0.45), math.sin(a) * 0.75), rnd.uniform(0.6, 0.8)))
    balls.append(((0, 2.95 * tall, 0), 0.7))
    t.add(L.blob("Canopy", balls, (0, 0, 0), resolution=0.2, target=700, noise=0.08), "Body", "P", canopy_mat)
    return t


tree("TreeRound", leaf, 4)
tree("TreeBlossom", blossom, 9, tall=0.85)
bu = model("Bush")
bu.add(L.blob("BushBlob", [((0, 0.35, 0), 0.55), ((0.45, 0.3, 0.1), 0.42), ((-0.42, 0.28, -0.05), 0.4), ((0.05, 0.3, 0.4), 0.38)],
              (0, 0, 0), resolution=0.14, target=420, noise=0.05), "Body", "P", leaf)
fb = model("FlowerBed")
fb.add(L.cyl("Kerb", 1.1, 0.22, (0, 0.11, 0), axis="y", segs=32, bevel=0.06), "Body", "P", stone_dark)
fb.add(L.cyl("Soil", 0.98, 0.06, (0, 0.21, 0), axis="y", segs=32, bevel=0.02), "Body", "P", soil)
rnd = random.Random(5)
for i in range(22):
    a, r = rnd.uniform(0, math.tau), math.sqrt(rnd.uniform(0.02, 0.8)) * 0.9
    x, z = math.cos(a) * r, math.sin(a) * r
    h = rnd.uniform(0.25, 0.45)
    fb.add(L.cyl(f"Stem{i}", 0.02, h, (x, 0.22 + h / 2, z), axis="y", segs=5, bevel=0.0), "Body", "P", leaf)
    fb.add(L.sphere(f"Bloom{i}", rnd.uniform(0.08, 0.12), (x, 0.24 + h, z), scale=(1, 0.7, 1), segs=8, rings=5), "Body", "P",
           FLOWERS[i % len(FLOWERS)])
bn = model("Bench")
for i in range(3):
    bn.add(L.rbox(f"Slat{i}", (1.8, 0.07, 0.16), (0, 0.48, -0.17 + i * 0.18), 0.03), "Body", "P", wood)
for i in range(2):
    bn.add(L.rbox(f"Back{i}", (1.8, 0.14, 0.06), (0, 0.72 + i * 0.2, 0.3), 0.03, euler=(-12, 0, 0)), "Body", "P", wood)
for sx in (-1, 1):
    bn.add(L.rbox(f"Leg{sx}", (0.1, 0.48, 0.5), (sx * 0.75, 0.24, 0.02), 0.04), "Body", "P", ink)
    bn.add(L.rbox(f"Arm{sx}", (0.1, 0.06, 0.55), (sx * 0.82, 0.68, 0.02), 0.03), "Body", "P", ink)
    bn.add(L.cyl(f"Back{sx}", 0.04, 0.6, (sx * 0.75, 0.8, 0.3), axis="y", segs=8, bevel=0.01, euler=(-12, 0, 0)), "Body", "P", ink)

# ------------------------------------------------------------------ Plaza disc with cobble rings + paths
pz = model("Plaza")
pz.add(L.cyl("PlazaDisc", 7.0, 0.12, (0, 0.06, 0), axis="y", segs=64, bevel=0.04), "Body", "P",
       M.get("cobble", "#FFF1DC", rough=0.85, kind="rings", a="#FFF4E0", b="#EBD6B5", scale=2.6))
pz.add(L.cyl("PlazaKerb", 7.12, 0.16, (0, 0.05, 0), axis="y", segs=64, bevel=0.05), "Body", "P", stone_dark)
for i in range(12):  # inlaid coloured note dots around the ring (pitch colours)
    a = i / 12 * math.tau
    col = ["#FF5A5F", "#FFA62B", "#FFE156", "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0"][i % 7]
    pz.add(L.cyl(f"Inlay{i}", 0.32, 0.03, (math.sin(a) * 5.6, 0.125, math.cos(a) * 5.6), axis="y", segs=20, bevel=0.01),
           "Body", "P", M.get(f"inlay{col}", col, rough=0.4))
ps = model("PathSegment")  # 2.4 m wide, 4 m long straight path tile with kerbs; pivot at its start
ps.add(L.rbox("PathTile", (2.4, 0.1, 4.0), (0, 0.05, 2.0), 0.03), "Body", "P",
       M.get("pathstone", "#FFF1DC", rough=0.85, kind="bricks", a="#FFF4E0", b="#F0DDBF", scale=1.4))
for sx in (-1, 1):
    ps.add(L.rbox(f"Kerb{sx}", (0.16, 0.16, 4.0), (sx * 1.26, 0.07, 2.0), 0.05), "Body", "P", stone_dark)

# ------------------------------------------------------------------ bake shared atlas, export each prop
group = L.Group("HubProps", [m for m in models])
tex, em = L.bake(group, size=2048, ao_samples=40, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material("M_HubProps", tex, em, 1.0)
for obj, node, slot in group.parts:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
for m in models:
    root, empties, meshes = L.build_hierarchy(m, m.name)
    if m.name.startswith("Tree") or m.name == "Bandstand":
        for o in list(meshes.values()):
            L.add_lod(o, 0.35)
    L.export_fbx(root, f"{OUT}/{m.name}.fbx")
    faces = sum(len(o.data.polygons) for o in root.children_recursive if o.type == "MESH" and "_LOD1" not in o.name)
    print(f"[LMW] {m.name}: ~{faces} faces")
L.save_source("HubProps")

# ------------------------------------------------------------------ ground (own atlas)
L.reset()
M = L.Mats()
gm = L.Model("HubGround")
grass = M.get("grass", "#7CCB5A", rough=0.9, kind="foliage", a="#5FB54E", b="#8FD86A", scale=0.08)
bm_obj = L.cyl("GroundDisc", 38.0, 0.2, (0, -0.1, 0), axis="y", segs=96, bevel=0.0)
sub = bm_obj.modifiers.new("Sub", "SUBSURF")
sub.levels = 2
sub.subdivision_type = "SIMPLE"
L._bake_modifiers(bm_obj, None, weighted=False)
for v in bm_obj.data.vertices:  # rolling hills that rise past the playable radius (hide the horizon)
    x, z = v.co.x, v.co.y
    r = math.hypot(x, z)
    if v.co.z > -0.05:
        lift = max(0.0, r - 17.0) ** 1.6 * 0.12 + 0.6 * math.sin(x * 0.35) * math.cos(z * 0.3) * max(0.0, (r - 15) / 20)
        v.co.z += lift
gm.add(bm_obj, "Body", "P", grass)
tex2, em2 = L.bake(gm, size=1024, ao_samples=16, tex_dir=L.ART_DIR + "/Textures")
mat2 = L.final_material("M_HubGround", tex2)
bm_obj.data.materials.clear()
bm_obj.data.materials.append(mat2)
root, _, _ = L.build_hierarchy(gm, "HubGround")
L.export_fbx(root, f"{OUT}/HubGround.fbx")
L.save_source("HubGround")
print("[LMW] hub environment done")
