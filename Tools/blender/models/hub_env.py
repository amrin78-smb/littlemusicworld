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
roof = M.get("roof", "#FF7A65", rough=0.45, kind="plain")
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
bs.add(L.tube_arc("BaseTrim", 2.57, 0.05, (0, 0.38, 0), 0, 360, segs=64, ring=8, euler=(90, 0, 0)), "Body", "P", gold)
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

# ------------------------------------------------------------------ themed gates for the other six areas
def arch_path(r=1.9, base=3.3, h=1.25, n=13):
    return [(math.cos(math.pi * i / (n - 1)) * r, base + math.sin(math.pi * i / (n - 1)) * h, 0.0) for i in range(n)]


def sign(m, colour, emblem):
    m.add(L.rbox("Sign", (1.6, 0.8, 0.14), (0, 3.55, -0.28), 0.12), "Body", "P", M.get("sign" + colour, colour, rough=0.5))
    emblem(m)


red = M.get("drumred", "#E2553F", rough=0.3)
apricot = M.get("apricot", "#F4A259", rough=0.35)
honey = M.get("honey", "#C68B59", rough=0.45, kind="wood", a="#D9A06B", b="#A8683E", scale=5.0)
mint = M.get("mint", "#5CC8B5", rough=0.4)
sky = M.get("skyblue", "#8ECAE6", rough=0.4)
stage_red = M.get("stagered", "#C0392B", rough=0.6)
violet = M.get("violet", "#7B5CD6", rough=0.35)
pink = M.get("pink", "#FF7BAC", rough=0.35)
candy = M.get("candy", "#FF5A8A", rough=0.4, kind="stripes", a="#FF5A8A", b="#FFF4E0", scale=6.0)
lemon = M.get("lemon", "#FFD23F", rough=0.4)

g = model("GateDrums")
for sx in (-1, 1):
    for k in range(3):
        g.add(L.cyl(f"Drum{sx}{k}", 0.48 - k * 0.04, 0.9, (sx * 1.9, 0.45 + k * 0.95, 0), axis="y", segs=28, bevel=0.05), "Body", "P", red if k % 2 == 0 else apricot)
        g.add(L.cyl(f"Head{sx}{k}", 0.46 - k * 0.04, 0.04, (sx * 1.9, 0.92 + k * 0.95, 0), axis="y", segs=28, bevel=0.015), "Body", "P", white)
g.add(L.sweep("Arch", arch_path(), L.circle(0.32, 16), ref=(0, 0, 1)), "Body", "P", red)
for i, (x, y, z) in enumerate(arch_path(n=9)[1:-1]):
    g.add(L.tube_arc(f"Hoop{i}", 0.34, 0.035, (x, y, z), 0, 360, segs=20, ring=6, euler=(0, 90, math.degrees(math.atan2(y - 3.3, x)) + 90)), "Body", "P", gold)
sign(g, "#F4A259", lambda m: [m.add(L.cyl(f"Stick{i}", 0.04, 0.7, (-0.15 + 0.3 * i, 3.6, -0.38), axis="y", segs=8, bevel=0.01,
                                            euler=(0, 0, 35 - 70 * i)), "Body", "P", wood) for i in range(2)])

g = model("GateStrings")
for sx in (-1, 1):
    g.add(L.rbox(f"Neck{sx}", (0.55, 3.4, 0.4), (sx * 1.9, 1.7, 0), 0.12), "Body", "P", honey)
    for f in range(6):
        g.add(L.rbox(f"Fret{sx}{f}", (0.58, 0.05, 0.08), (sx * 1.9, 0.5 + f * 0.5, -0.2), 0.02), "Body", "P", gold)
g.add(L.sweep("Arch", arch_path(), L.circle(0.3, 16), ref=(0, 0, 1)), "Body", "P", honey)
for i in range(6):
    g.add(L.cyl(f"Str{i}", 0.018, 3.6, (-0.4 + i * 0.16, 3.0, -0.12), axis="x", segs=6, bevel=0.0, euler=(0, 0, 90)), "Body", "P", white)
sign(g, "#F2C14E", lambda m: m.add(L.cyl("Hole", 0.24, 0.05, (0, 3.55, -0.36), axis="z", segs=24, bevel=0.01), "Body", "P", ink))

g = model("GateWind")
for sx in (-1, 1):
    for k in range(3):
        g.add(L.cyl(f"Pipe{sx}{k}", 0.16, 2.8 + k * 0.35, (sx * (1.6 + k * 0.32), (2.8 + k * 0.35) / 2, 0), axis="y", segs=16, bevel=0.03),
              "Body", "P", sky if k % 2 else mint)
        g.add(L.lathe(f"Bell{sx}{k}", [(0.16, 0.0), (0.2, 0.15), (0.34, 0.3), (0.0, 0.3)], (sx * (1.6 + k * 0.32), 2.8 + k * 0.35, 0),
                      axis="y", segs=20), "Body", "P", gold)
g.add(L.sweep("Arch", arch_path(r=1.75, base=3.5), L.circle(0.22, 16), ref=(0, 0, 1)), "Body", "P", gold)
sign(g, "#5CC8B5", lambda m: m.add(L.lathe("Horn", [(0.04, 0.0), (0.05, 0.3), (0.2, 0.45), (0.0, 0.45)], (-0.25, 3.55, -0.38), axis="x", segs=20),
                                   "Body", "P", gold))

g = model("GateOrchestra")
for sx in (-1, 1):
    g.add(L.lathe(f"Col{sx}", [(0.0, 0.0), (0.32, 0.0), (0.24, 0.2), (0.2, 3.0), (0.32, 3.2), (0.0, 3.2)], (sx * 1.9, 0, 0), axis="y", segs=20),
          "Body", "P", white)
    for k in range(5):
        g.add(L.sphere(f"Curtain{sx}{k}", 0.3, (sx * (1.45 - k * 0.05), 2.7 - k * 0.12, -0.15), scale=(0.5, 2.2, 0.4), segs=12, rings=8),
              "Body", "P", stage_red)
shell = [(math.cos(math.pi * i / 12) * 2.3, 3.2 + math.sin(math.pi * i / 12) * 1.5, 0.0) for i in range(13)]
g.add(L.sweep("Shell", shell, L.circle(0.26, 14), ref=(0, 0, 1)), "Body", "P", gold)
for i in range(1, 12, 2):
    x, y, z = shell[i]
    g.add(L.sphere(f"Lantern{i}", 0.16, (x, y - 0.35, -0.1), segs=12, rings=8), "Body", "P", lamp)
sign(g, "#C0392B", lambda m: m.add(L.sphere("Star", 0.18, (0, 3.6, -0.38), scale=(1, 1, 0.4), segs=5, rings=4), "Body", "P", gold))

g = model("GateStudio")
for sx in (-1, 1):
    g.add(L.cyl(f"Cup{sx}", 0.7, 0.5, (sx * 1.9, 0.95, 0), axis="x", segs=28, bevel=0.15), "Body", "P", violet)
    g.add(L.cyl(f"Pad{sx}", 0.52, 0.2, (sx * 1.6, 0.95, 0), axis="x", segs=24, bevel=0.08), "Body", "P", pink)
    g.add(L.rbox(f"Post{sx}", (0.3, 1.9, 0.3), (sx * 1.9, 2.4, 0), 0.1), "Body", "P", violet)
g.add(L.sweep("Band", arch_path(r=1.9, base=3.3, h=1.4), L.circle(0.24, 14), ref=(0, 0, 1)), "Body", "P", violet)
sign(g, "#FF7BAC", lambda m: m.add(L.sphere("Rec", 0.16, (0, 3.55, -0.38), scale=(1, 1, 0.4), segs=16, rings=8), "Body", "P",
                                   M.get("recred", "#FF3B3B", rough=0.3, emit=0.6)))

g = model("GateCarnival")
for sx in (-1, 1):
    g.add(L.cyl(f"Pole{sx}", 0.22, 3.4, (sx * 1.9, 1.7, 0), axis="y", segs=16, bevel=0.04), "Body", "P", candy)
    g.add(L.sphere(f"Ball{sx}", 0.3, (sx * 1.9, 3.55, 0), segs=16, rings=10), "Body", "P", lemon)
g.add(L.sweep("Arch", arch_path(h=1.5), L.circle(0.28, 16), ref=(0, 0, 1)), "Body", "P", candy)
for i, (x, y, z) in enumerate(arch_path(n=11)[1:-1]):
    g.add(L.cyl(f"Flag{i}", 0.2, 0.02, (x, y - 0.38, -0.05), axis="z", segs=3, bevel=0.0), "Body", "P",
          [lemon, pink, sky, mint][i % 4])
sign(g, "#FFD23F", lambda m: [m.add(L.sphere(f"Balloon{i}", 0.13, (-0.2 + 0.2 * i, 3.6 + 0.05 * (i % 2), -0.38), scale=(1, 1.2, 1), segs=12,
                                             rings=8), "Body", "P", [pink, sky, mint][i]) for i in range(3)])

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
group.ao_distance = 0.8
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
import bmesh
from mathutils import Vector
bm = bmesh.new()
bmesh.ops.create_grid(bm, x_segments=72, y_segments=72, size=40.0)   # 80 m square grid, Blender XY = Unity XZ
for v in bm.verts:
    x, z = v.co.x, v.co.y
    r = math.hypot(x, z)
    # flat playable area, then soft rolling hills that rise past ~17 m and hide the horizon
    lift = max(0.0, r - 17.0) ** 1.6 * 0.12 + 0.6 * math.sin(x * 0.35) * math.cos(z * 0.3) * max(0.0, (r - 15) / 20)
    v.co.z = lift
bm_obj = L._link("GroundGrid", bm, recalc=False)  # open plane: keep create_grid's upward normals
L._bake_modifiers(bm_obj, None, weighted=False)
gm.add(bm_obj, "Body", "P", grass)
tex2, em2 = L.bake(gm, size=1024, ao_samples=16, tex_dir=L.ART_DIR + "/Textures")
mat2 = L.final_material("M_HubGround", tex2)
bm_obj.data.materials.clear()
bm_obj.data.materials.append(mat2)
root, _, _ = L.build_hierarchy(gm, "HubGround")
L.export_fbx(root, f"{OUT}/HubGround.fbx")
L.save_source("HubGround")
print("[LMW] hub environment done")
