"""String Town instruments: Acoustic Guitar, Electric Guitar, Ukulele, Violin (+bow), Cello (+bow), Harp, Banjo.
Shared 2048² atlas T_StringInstruments. Strings are separate nodes "String_<i>" (0 = lowest) so Unity can vibrate
the exact string that sounds; bows are nodes "Bow". Instruments stand upright on display stands, fronts −Z.
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
spruce = M.get("spruce", "#E9C08E", rough=0.45, kind="wood", a="#F0CD9B", b="#D9A86B", scale=14)
maple = M.get("maple", "#B5651D", rough=0.35, kind="wood", a="#C67A32", b="#8E4A17", scale=10)
rosewood = M.get("rosewood", "#5A3825", rough=0.5)
ebony = M.get("ebony", "#2E2A4A", rough=0.35)
chrome = M.get("chrome", "#DDE3EE", rough=0.2)
gold = M.get("gold", "#F2C14E", rough=0.28)
string = M.get("string", "#F3F1EA", rough=0.25)
redlac = M.get("redlac", "#E63946", rough=0.22)
white = M.get("pickguard", "#FFFBF3", rough=0.35)
yellow = M.get("uke", "#FFB703", rough=0.35, kind="wood", a="#FFC233", b="#E89B00", scale=10)
skin = M.get("banjohead", "#FFF6E6", rough=0.4)
teal = M.get("teal", "#2A9D8F", rough=0.4)
hair = M.get("bowhair", "#FFF4E0", rough=0.6)
PITCH = ["#FF5A5F", "#FFA62B", "#FFE156", "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0"]
models = []


def model(name):
    m = L.Model(name)
    models.append(m)
    return m


def figure8(lower, upper, waist, length, waist_pos=0.55, upper_w=None, steps=8):
    """Guitar/violin body outline in the XY plane (x across, y along the neck), y from 0 (bottom) to length."""
    pts = []
    half = []
    prof = [(0.0, 0.0), (lower * 0.7, 0.02 * length), (lower, 0.28 * length), (waist, waist_pos * length),
            (upper_w or upper, 0.8 * length), (upper * 0.6, 0.97 * length), (0.0, length)]
    for x, y in prof:
        half.append((x, y))
    right = L.catmull(half, steps=steps, closed=False)
    left = [(-x, y) for x, y in reversed(right[1:-1])]
    return right + left


def body_extrude(m, node, name, outline, depth, centre, mat, tilt=0.0, bevel=0.015):
    """Body standing upright: outline XY (y up), extruded along Z (depth), placed at centre."""
    cx, cy, cz = centre
    # outline_extrude works in Unity XZ; build flat then rotate it upright about X.
    o = L.outline_extrude(name, [(x, y) for x, y in outline], 0.0, depth, bevel=bevel, segs=2)
    L.set_origin(o, (0, 0, 0))
    import bpy
    from mathutils import Matrix
    o.matrix_world = L.unity_matrix((cx, cy, cz + depth / 2), (-90 + tilt, 0, 0)) @ Matrix.Identity(4)
    m.add(o, node, "P", mat)
    return o


def strings(m, xs, y0, y1, z, thick=0.006, base="Body"):
    for i, (xa, xb) in enumerate(xs):
        n = m.node(f"String_{i}", base, ((xa + xb) / 2, (y0 + y1) / 2, z))
        length = math.hypot(xb - xa, y1 - y0)
        ang = math.degrees(math.atan2(xb - xa, y1 - y0))
        m.add(L.cyl(f"Str{i}", thick, length, ((xa + xb) / 2, (y0 + y1) / 2, z), axis="y", segs=6, bevel=0.0, euler=(0, 0, -ang)), n, "P", string)


def display_stand(m, x, z, h, w=0.3):
    m.add(L.rbox("StandBase", (w, 0.04, 0.3), (x, 0.02, z + 0.05), 0.015), "Body", "P", ebony)
    m.add(L.cyl("StandPost", 0.015, h, (x, h / 2, z + 0.12), axis="y", segs=8, bevel=0.003, euler=(-8, 0, 0)), "Body", "P", ebony)


# ------------------------------------------------------------------ Acoustic guitar
def guitar(name, body_mat, top_mat, scale=1.0, n_str=6, electric=False, uke=False):
    g = model(name)
    L_ = 0.5 * scale
    outline = figure8(0.2 * scale, 0.15 * scale, 0.115 * scale, L_) if not electric else \
        [(x * 1.0, y) for x, y in figure8(0.19 * scale, 0.16 * scale, 0.1 * scale, 0.44 * scale, waist_pos=0.6)]
    y0 = 0.12
    body_extrude(g, "Body", "GtrBody", outline, 0.1 * scale if not electric else 0.045, (0, y0, 0), body_mat)
    body_extrude(g, "Body", "GtrTop", outline, 0.006, (0, y0, -0.006 - (0.05 * scale if not electric else 0.0225)), top_mat, bevel=0.003)
    bl = 0.44 * scale if electric else L_
    neck_len = 0.48 * scale
    g.add(L.rbox("Neck", (0.055 * scale, neck_len, 0.025), (0, y0 + bl + neck_len / 2 - 0.02, -0.03), 0.01), "Body", "P", rosewood)
    g.add(L.rbox("Head", (0.08 * scale, 0.16 * scale, 0.025), (0, y0 + bl + neck_len + 0.06 * scale, -0.02), 0.015,
                 euler=(8, 0, 0)), "Body", "P", body_mat)
    for i in range(n_str):
        side = -1 if i < (n_str + 1) // 2 else 1
        row = i if side < 0 else i - (n_str + 1) // 2
        g.add(L.cyl(f"Peg{i}", 0.012, 0.05, (side * 0.06 * scale, y0 + bl + neck_len + (0.02 + row * 0.045) * scale, -0.02),
                    axis="x", segs=8, bevel=0.003), "Body", "P", chrome)
    for f in range(6):  # frets
        g.add(L.rbox(f"Fret{f}", (0.056 * scale, 0.004, 0.006), (0, y0 + bl + 0.06 + f * 0.07 * scale, -0.044), 0.002, segs=1), "Body", "P", chrome)
    zf = -0.05 * scale - 0.012 if not electric else -0.03
    if not electric:
        g.add(L.cyl("Soundhole", 0.05 * scale, 0.004, (0, y0 + 0.62 * L_, zf + 0.004), axis="z", segs=28, bevel=0.0), "Body", "P", ebony)
        g.add(L.tube_arc("Rosette", 0.058 * scale, 0.006, (0, y0 + 0.62 * L_, zf + 0.002), 0, 360, segs=28, ring=6), "Body", "P",
              teal if uke else gold)
        g.add(L.rbox("Bridge", (0.11 * scale, 0.025, 0.012), (0, y0 + 0.2 * L_, zf), 0.006), "Body", "P", rosewood)
    else:
        g.add(L.rbox("Guard", (0.18, 0.22, 0.004), (0.03, y0 + 0.16, zf - 0.004), 0.03), "Body", "P", white)
        for i, yy in enumerate((0.13, 0.25)):
            g.add(L.rbox(f"Pickup{i}", (0.08, 0.025, 0.012), (0, y0 + yy, zf - 0.01), 0.006), "Body", "P", ebony)
        for i in range(3):
            g.add(L.cyl(f"Knob{i}", 0.014, 0.014, (0.12, y0 + 0.06 + i * 0.045, zf - 0.008), axis="z", segs=12, bevel=0.003), "Body", "P", chrome)
        g.add(L.rbox("Bridge", (0.09, 0.02, 0.012), (0, y0 + 0.07, zf - 0.006), 0.005), "Body", "P", chrome)
    top_y = y0 + bl + neck_len
    bot_y = y0 + (0.2 * L_ if not electric else 0.07)
    span = (0.045 if uke else 0.042) * scale
    xs = [(-span + 2 * span * i / (n_str - 1) * 1.15, -span * 0.8 + 1.6 * span * 0.8 * i / (n_str - 1)) for i in range(n_str)]
    strings(g, [(a, b) for a, b in xs], bot_y, top_y, zf - 0.015, 0.0045 if not electric else 0.004)
    display_stand(g, 0, 0.12, 0.3)
    return g


guitar("AcousticGuitar", maple, spruce)
guitar("ElectricGuitar", redlac, redlac, electric=True)
guitar("Ukulele", yellow, yellow, scale=0.62, n_str=4, uke=True)
guitar("Banjo", maple, skin, scale=0.85, n_str=5)  # body replaced below

# Banjo: round drum body instead of figure-8
bj = models[-1]
import bpy  # noqa: E402
for o, nd, sl in list(bj.parts):
    if o.name.startswith(("GtrBody", "GtrTop", "Soundhole", "Rosette")):
        bj.parts.remove((o, nd, sl))
        bpy.data.objects.remove(o)
bj.add(L.cyl("BanjoPot", 0.19, 0.08, (0, 0.33, 0.0), axis="z", segs=36, bevel=0.015), "Body", "P", maple)
bj.add(L.cyl("BanjoHead", 0.18, 0.006, (0, 0.33, -0.045), axis="z", segs=36, bevel=0.002), "Body", "P", skin)
bj.add(L.tube_arc("BanjoRim", 0.19, 0.012, (0, 0.33, -0.045), 0, 360, segs=36, ring=8), "Body", "P", chrome)
for i in range(16):
    a = i / 16 * math.tau
    bj.add(L.rbox(f"BanjoHook{i}", (0.012, 0.03, 0.06), (math.cos(a) * 0.195, 0.33 + math.sin(a) * 0.195, 0.0), 0.004, segs=1,
                  euler=(0, 0, -math.degrees(a))), "Body", "P", chrome)


# ------------------------------------------------------------------ Violin and cello (+ bows)
def bowed(name, scale, endpin):
    v = model(name)
    L_ = 0.36 * scale
    y0 = endpin
    outline = figure8(0.105 * scale, 0.085 * scale, 0.06 * scale, L_, waist_pos=0.5)
    body_extrude(v, "Body", "VBody", outline, 0.06 * scale, (0, y0, 0), maple)
    neck = 0.24 * scale
    v.add(L.rbox("VNeck", (0.03 * scale, neck, 0.025 * scale), (0, y0 + L_ + neck / 2, -0.02 * scale), 0.008), "Body", "P", maple)
    v.add(L.rbox("Fingerboard", (0.034 * scale, neck * 1.15, 0.01 * scale), (0, y0 + L_ + neck * 0.4, -0.04 * scale), 0.005), "Body", "P", ebony)
    scroll = []
    for i in range(20):
        a = i / 19 * math.pi * 2.2
        r = 0.035 * scale * (1 - 0.6 * i / 19)
        scroll.append((0.0, y0 + L_ + neck + 0.03 * scale + r * math.sin(a), -0.02 * scale - r * math.cos(a)))
    v.add(L.sweep("Scroll", scroll, L.circle(0.016 * scale, 8), ref=(1, 0, 0)), "Body", "P", maple)
    for sx in (-1, 1):
        fh = [(sx * 0.045 * scale, y0 + L_ * (0.3 + 0.25 * t), -0.032 * scale - 0.001) for t in (0, 0.5, 1)]
        v.add(L.sweep(f"FHole{sx}", [(p[0] + sx * 0.008 * math.sin(i * 2), p[1], p[2]) for i, p in enumerate(fh)],
                      L.circle(0.005 * scale, 6)), "Body", "P", ebony)
    v.add(L.rbox("VBridge", (0.06 * scale, 0.02 * scale, 0.025 * scale), (0, y0 + L_ * 0.42, -0.045 * scale), 0.004), "Body", "P", spruce)
    v.add(L.rbox("Tailpiece", (0.035 * scale, 0.1 * scale, 0.01 * scale), (0, y0 + L_ * 0.18, -0.04 * scale), 0.005), "Body", "P", ebony)
    span = 0.011 * scale
    strings(v, [(-1.5 * span + i * span, -1.2 * span + i * 0.8 * span) for i in range(4)], y0 + L_ * 0.25, y0 + L_ + neck,
            -0.052 * scale, 0.0025 * scale)
    if scale > 2.0:  # cello stands on its endpin
        v.add(L.cyl("Endpin", 0.01, endpin, (0, endpin / 2, 0), axis="y", segs=8, bevel=0.003, radius2=0.004), "Body", "P", chrome)
    else:            # violin rests on a display stand
        display_stand(v, 0, 0.06, endpin)
    b = v.node("Bow", "Body", (0.22 * scale, y0 + L_ * 0.5, -0.1 * scale))
    bl = 0.75 * scale
    v.add(L.cyl("BowStick", 0.006 * scale, bl, (0.22 * scale, y0 + L_ * 0.5, -0.1 * scale), axis="y", segs=8, bevel=0.002,
                euler=(0, 0, 25)), b, "P", rosewood)
    v.add(L.rbox("BowHair", (0.004 * scale, bl * 0.92, 0.012 * scale), (0.2 * scale, y0 + L_ * 0.5, -0.11 * scale), 0.002, segs=1,
                 euler=(0, 0, 25)), b, "P", hair)
    v.add(L.rbox("BowFrog", (0.02 * scale, 0.05 * scale, 0.03 * scale), (0.22 * scale + 0.16 * scale, y0 + L_ * 0.5 - 0.34 * scale, -0.1 * scale),
                 0.006, euler=(0, 0, 25)), b, "P", ebony)
    return v


bowed("Violin", 1.0, 0.35)
bowed("Cello", 2.6, 0.18)

# ------------------------------------------------------------------ Harp (coloured strings, C major from C3)
h = model("Harp")
h.add(L.sweep("HarpNeck", [(-0.32, 1.45, 0), (-0.1, 1.55, 0), (0.12, 1.42, 0), (0.3, 1.5, 0), (0.42, 1.62, 0)],
              L.circle(0.05, 12), ref=(0, 0, 1), taper=lambda t: (1.0, 1.0 + 0.2 * math.sin(t * math.pi))), "Body", "P", gold)
h.add(L.cyl("Pillar", 0.05, 1.5, (-0.34, 0.78, 0), axis="y", segs=16, bevel=0.012), "Body", "P", gold)
h.add(L.sphere("PillarTop", 0.08, (-0.34, 1.52, 0), segs=14, rings=8), "Body", "P", gold)
h.add(L.cyl("Soundbox", 0.11, 1.55, (0.15, 0.82, 0.02), axis="y", segs=20, bevel=0.02, radius2=0.05, euler=(0, 0, -20)), "Body", "P", maple)
h.add(L.rbox("HarpBase", (0.75, 0.08, 0.32), (-0.05, 0.04, 0.02), 0.03), "Body", "P", maple)
harp_notes = 15
for i in range(harp_notes):
    t = i / (harp_notes - 1)
    x = -0.26 + 0.6 * t
    top = 1.47 + 0.08 * math.sin(t * math.pi * 1.2)
    bottom = 0.12 + 1.35 * t * 0.62 + 0.04
    n = h.node(f"String_{i}", "Body", (x, (top + bottom) / 2, 0))
    h.add(L.cyl(f"HStr{i}", 0.007, top - bottom, (x, (top + bottom) / 2, 0), axis="y", segs=6, bevel=0.0), n, "P",
          M.get(f"hs{i % 7}", PITCH[i % 7], rough=0.3))

# ------------------------------------------------------------------ bake + export
group = L.Group("StringInstruments", models)
tex, em = L.bake(group, size=2048, ao_samples=32, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material("M_StringInstruments", tex)
for obj, node, slot in group.parts:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
for mdl in models:
    root, empties, meshes = L.build_hierarchy(mdl, mdl.name)
    L.export_fbx(root, f"{OUT}/{mdl.name}.fbx")
    print(f"[LMW] {mdl.name}: {sum(len(o.data.polygons) for o in meshes.values())} faces, "
          f"{len([q for q in mdl.nodes if q.startswith('String_')])} strings")
L.save_source("StringInstruments")
if PREVIEW:
    for i, mdl in enumerate(models):
        for o in bpy.data.objects:
            if o.name == mdl.name:
                o.location = L.U(i * 0.9 - 3.0, 0, 0)
    L.render_preview(L.RENDER_DIR + "/StringInstruments", [((0.0, 1.6, -4.6), (0.0, 0.8, 0.0))], size=(1400, 700))
