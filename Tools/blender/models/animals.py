"""Animal musicians (Art Bible §3) — six original characters from one parametric generator.

Shared proportions (head ≈ 40 % of height, short body, stubby limbs, mitten hands) so one rig layout and one set of
clip generators drive them all; each animal differs in silhouette features, colours, accessory and motion style.
Rig (15 bones): root, body, head, ear_L/R, arm_L/R, hand_L/R, leg_L/R, tail, eye_L/R, jaw.
Clips: Idle, Walk, Dance, Celebrate, Rest, React (shared) + per-instrument "Play<Style>" one-shots that the
Orchestra re-triggers on every note so movement lands on the sound, and "Hold<Style>" loops while performing.
Outputs: Assets/Art/Characters/<Animal>.fbx, T_<Animal>.png, Blender/SourceModels, Blender/Renders.
Usage: blender -b --factory-startup --python animals.py -- [Bear Rabbit …] [--preview]
"""
import math
import os
import sys

import bpy
from mathutils import Quaternion

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW = "--preview" in ARGS
FPS = 30

ANIMALS = {
    "Bear":     dict(fur="#A8683E", belly="#E4B57E", accent="#E63946", ears="round", snout="muzzle", tail="stub",
                     accessory="bandana", style="Drums", nose="#3B2A24", height=1.0, girth=1.15),
    "Rabbit":   dict(fur="#F5F0EA", belly="#FFFFFF", accent="#5B7CFA", ears="long", snout="small", tail="puff",
                     accessory="bowtie", style="Keys", nose="#FF8FA3", height=0.95, girth=0.9, inner="#FFB4C2"),
    "Fox":      dict(fur="#F28C38", belly="#FFF4E0", accent="#2A9D8F", ears="pointy", snout="long", tail="bushy",
                     accessory="scarf", style="Bow", nose="#2E2A4A", height=0.98, girth=0.92, inner="#FFF4E0"),
    "Elephant": dict(fur="#9BA8C9", belly="#B8C2DD", accent="#C0392B", ears="flap", snout="trunk", tail="thin",
                     accessory="hat", style="Blow", nose="#9BA8C9", height=1.1, girth=1.2, inner="#C8A6D8"),
    "Cat":      dict(fur="#4A4E69", belly="#F2E9E4", accent="#F4C430", ears="pointy", snout="small", tail="long",
                     accessory="star", style="Strum", nose="#FF8FA3", height=0.92, girth=0.9, inner="#FF8FA3"),
    "Penguin":  dict(fur="#2E2A4A", belly="#FFFBF3", accent="#FFC93C", ears="none", snout="beak", tail="fan",
                     accessory="beanie", style="Shake", nose="#FFA62B", height=0.9, girth=1.05),
}
STYLES = ["Drums", "Keys", "Bow", "Strum", "Blow", "Shake"]


def build(name, a):
    L.reset()
    scene = bpy.context.scene
    scene.render.fps = FPS
    M = L.Mats()
    fur = M.get("fur", a["fur"], rough=0.75)
    belly = M.get("belly", a["belly"], rough=0.8)
    accent = M.get("accent", a["accent"], rough=0.55)
    nose = M.get("nose", a["nose"], rough=0.3)
    inner = M.get("inner", a.get("inner", a["belly"]), rough=0.8)
    ink = M.get("ink", "#2E2A4A", rough=0.12)
    white = M.get("white", "#FFFFFF", rough=0.1, emit=0.4)
    blush = M.get("blush", "#FF8FA3", rough=0.8)
    H = a["height"]
    G = a["girth"]
    m = L.Model(name)
    bone_of = {}

    def part(obj, bone, mat):
        m.add(obj, "Body", "P", mat)
        bone_of[obj] = bone
        return obj

    # body: rounded pear, belly patch as a front ellipsoid
    by = 0.42 * H
    part(L.lathe("Torso", [(0.0, 0.0), (0.22 * G, 0.02), (0.3 * G, 0.12), (0.31 * G, 0.24), (0.27 * G, 0.36), (0.2 * G, 0.44),
                           (0.0, 0.46)], (0, 0.14 * H, 0), axis="y", segs=28), "body", fur)
    part(L.sphere("Belly", 0.24 * G, (0, 0.33 * H, 0.1 * G), scale=(0.95, 1.05, 0.6), segs=20, rings=12), "body", belly)
    # head
    hy = 0.82 * H
    hr = 0.3 * H
    part(L.sphere("Head", hr, (0, hy, 0.02), scale=(1.08, 0.95, 1.0), segs=28, rings=18), "head", fur)
    part(L.sphere("Face", hr * 0.62, (0, hy - 0.05 * H, hr * 0.55), scale=(1.1, 0.85, 0.6), segs=20, rings=12), "head",
         belly if a["snout"] != "beak" else belly)
    for side, sx in (("L", -1), ("R", 1)):
        x = sx * hr * 0.38
        part(L.sphere(f"Eye{side}", hr * 0.17, (x, hy + 0.04 * H, hr * 0.84), scale=(0.9, 1.1, 0.5), segs=16, rings=10), f"eye_{side}", ink)
        part(L.sphere(f"Hi{side}", hr * 0.055, (x + hr * 0.05, hy + 0.07 * H, hr * 0.93), segs=8, rings=5), f"eye_{side}", white)
        part(L.sphere(f"Cheek{side}", hr * 0.12, (sx * hr * 0.62, hy - 0.06 * H, hr * 0.72), scale=(1, 0.65, 0.4), segs=10, rings=6), "head", blush)

    # snout / nose / beak / trunk
    sn = a["snout"]
    if sn == "muzzle":
        part(L.sphere("Muzzle", hr * 0.36, (0, hy - 0.08 * H, hr * 0.85), scale=(1.15, 0.85, 0.75), segs=18, rings=10), "head", belly)
        part(L.sphere("Nose", hr * 0.13, (0, hy - 0.03 * H, hr * 1.1), scale=(1.3, 0.8, 0.8), segs=12, rings=8), "head", nose)
    elif sn == "long":
        part(L.cyl("Snout", hr * 0.24, hr * 0.55, (0, hy - 0.07 * H, hr * 1.0), axis="z", segs=16, bevel=0.03, radius2=hr * 0.12), "head", belly)
        part(L.sphere("Nose", hr * 0.09, (0, hy - 0.06 * H, hr * 1.28), segs=10, rings=6), "head", nose)
    elif sn == "small":
        part(L.sphere("Nose", hr * 0.08, (0, hy - 0.03 * H, hr * 0.98), scale=(1.3, 0.9, 0.8), segs=10, rings=6), "head", nose)
        part(L.sphere("Mouth", hr * 0.14, (0, hy - 0.1 * H, hr * 0.9), scale=(1.4, 0.6, 0.5), segs=12, rings=6), "jaw", belly)
    elif sn == "beak":
        part(L.cyl("Beak", hr * 0.2, hr * 0.32, (0, hy - 0.05 * H, hr * 1.0), axis="z", segs=14, bevel=0.02, radius2=hr * 0.04), "head", nose)
        part(L.cyl("BeakLow", hr * 0.14, hr * 0.22, (0, hy - 0.1 * H, hr * 0.95), axis="z", segs=12, bevel=0.015, radius2=hr * 0.03), "jaw", nose)
    elif sn == "trunk":
        pts = []
        for i in range(14):
            t = i / 13
            pts.append((0.0, hy - 0.05 * H - 0.32 * H * t + 0.1 * H * t ** 3, hr * 0.85 + 0.18 * H * math.sin(t * 2.2)))
        part(L.sweep("Trunk", pts, L.circle(hr * 0.17, 12), ref=(1, 0, 0), taper=lambda t: (1 - 0.45 * t, 1 - 0.45 * t)), "jaw", fur)
        part(L.cyl("TrunkTip", hr * 0.1, 0.03, pts[-1], axis="y", segs=12, bevel=0.01), "jaw", nose)

    # ears
    er = a["ears"]
    for side, sx in (("L", -1), ("R", 1)):
        eb = f"ear_{side}"
        if er == "round":
            part(L.sphere(f"Ear{side}", hr * 0.3, (sx * hr * 0.72, hy + hr * 0.72, 0), scale=(1, 1, 0.5), segs=14, rings=8), eb, fur)
            part(L.sphere(f"EarIn{side}", hr * 0.17, (sx * hr * 0.72, hy + hr * 0.72, hr * 0.13), scale=(1, 1, 0.4), segs=10, rings=6), eb, belly)
        elif er == "long":
            part(L.sphere(f"Ear{side}", hr * 0.24, (sx * hr * 0.32, hy + hr * 1.35, 0), scale=(0.75, 2.5, 0.42), segs=14, rings=10,
                          euler=(0, 0, -sx * 10)), eb, fur)
            part(L.sphere(f"EarIn{side}", hr * 0.13, (sx * hr * 0.32, hy + hr * 1.35, hr * 0.08), scale=(0.7, 2.6, 0.3), segs=10, rings=8,
                          euler=(0, 0, -sx * 10)), eb, inner)
        elif er == "pointy":
            part(L.cyl(f"Ear{side}", hr * 0.3, hr * 0.55, (sx * hr * 0.55, hy + hr * 0.95, 0), axis="y", segs=4, bevel=0.03,
                       radius2=0.01, euler=(0, 45, -sx * 18)), eb, fur)
            part(L.cyl(f"EarIn{side}", hr * 0.18, hr * 0.38, (sx * hr * 0.55, hy + hr * 0.92, hr * 0.1), axis="y", segs=4, bevel=0.02,
                       radius2=0.01, euler=(0, 45, -sx * 18)), eb, inner)
        elif er == "flap":
            part(L.sphere(f"Ear{side}", hr * 0.62, (sx * hr * 1.05, hy + 0.02 * H, -0.02), scale=(0.35, 1.0, 0.9), segs=18, rings=10,
                          euler=(0, sx * 18, 0)), eb, fur)
            part(L.sphere(f"EarIn{side}", hr * 0.48, (sx * hr * 1.0, hy + 0.02 * H, 0.04), scale=(0.25, 0.95, 0.85), segs=14, rings=8,
                          euler=(0, sx * 18, 0)), eb, inner)

    # arms (mitten hands) and legs
    for side, sx in (("L", -1), ("R", 1)):
        part(L.sphere(f"Arm{side}", 0.08 * G, (sx * 0.33 * G, 0.42 * H, 0.03), scale=(0.9, 1.7, 0.9), segs=14, rings=10,
                      euler=(0, 0, sx * 20)), f"arm_{side}", fur)
        part(L.sphere(f"Hand{side}", 0.075 * G, (sx * 0.38 * G, 0.28 * H, 0.06), segs=14, rings=10), f"hand_{side}",
             belly if name != "Penguin" else fur)
        part(L.sphere(f"Leg{side}", 0.1 * G, (sx * 0.15 * G, 0.1 * H, 0.03), scale=(1.0, 1.1, 1.0), segs=14, rings=10), f"leg_{side}", fur)
        part(L.sphere(f"Foot{side}", 0.09 * G, (sx * 0.16 * G, 0.04, 0.1), scale=(1.0, 0.55, 1.4), segs=14, rings=8), f"leg_{side}",
             nose if name == "Penguin" else belly)

    # tails
    tl = a["tail"]
    if tl == "stub":
        part(L.sphere("Tail", 0.07, (0, 0.24 * H, -0.3 * G), segs=10, rings=6), "tail", fur)
    elif tl == "puff":
        part(L.sphere("Tail", 0.1, (0, 0.22 * H, -0.3 * G), segs=12, rings=8), "tail", belly)
    elif tl in ("bushy", "long", "thin"):
        pts = [(0, 0.2 * H, -0.27 * G)]
        for i in range(1, 12):
            t = i / 11
            pts.append((0.08 * math.sin(t * 3), 0.2 * H + 0.35 * H * t ** 1.4, -0.27 * G - 0.25 * t))
        rad = {"bushy": 0.11, "long": 0.045, "thin": 0.025}[tl]
        part(L.sweep("Tail", pts, L.circle(rad, 10), ref=(1, 0, 0),
                     taper=(lambda t: (0.6 + 0.6 * math.sin(t * math.pi), 0.6 + 0.6 * math.sin(t * math.pi))) if tl == "bushy" else
                     (lambda t: (1 - 0.5 * t, 1 - 0.5 * t))), "tail", fur)
        if tl == "bushy":
            part(L.sphere("TailTip", 0.09, pts[-1], segs=12, rings=8), "tail", belly)
    elif tl == "fan":
        part(L.sphere("Tail", 0.09, (0, 0.12 * H, -0.28 * G), scale=(1.4, 0.4, 1.0), segs=12, rings=8, euler=(30, 0, 0)), "tail", fur)

    # accessory
    ac = a["accessory"]
    if ac == "bandana":
        part(L.tube_arc("Bandana", hr * 0.78, 0.04, (0, hy + hr * 0.42, 0.0), 0, 360, segs=26, ring=8, euler=(90, 0, 0)), "head", accent)
        part(L.sphere("Knot", 0.06, (hr * 0.5, hy + hr * 0.45, -hr * 0.6), segs=10, rings=6), "head", accent)
    elif ac == "bowtie":
        for sx in (-1, 1):
            part(L.cyl(f"Bow{sx}", 0.07, 0.1, (sx * 0.06, 0.6 * H, 0.22 * G), axis="x", segs=3, bevel=0.015, radius2=0.015), "body", accent)
        part(L.sphere("BowKnot", 0.03, (0, 0.6 * H, 0.24 * G), segs=10, rings=6), "body", accent)
    elif ac == "scarf":
        part(L.tube_arc("Scarf", 0.2 * G, 0.055, (0, 0.6 * H, 0.0), 0, 360, segs=26, ring=8, euler=(90, 0, 0)), "body", accent)
        part(L.rbox("ScarfEnd", (0.09, 0.22, 0.04), (0.1, 0.48 * H, 0.2 * G), 0.03, euler=(0, 0, 12)), "body", accent)
    elif ac == "hat":
        part(L.cyl("HatBrim", hr * 0.6, 0.03, (0, hy + hr * 0.93, 0), axis="y", segs=24, bevel=0.012), "head", accent)
        part(L.cyl("HatTop", hr * 0.38, hr * 0.42, (0, hy + hr * 1.13, 0), axis="y", segs=24, bevel=0.02), "head", accent)
    elif ac == "star":
        star = [(math.cos(math.pi / 2 + i * math.pi / 5) * (0.06 if i % 2 == 0 else 0.026),
                 math.sin(math.pi / 2 + i * math.pi / 5) * (0.06 if i % 2 == 0 else 0.026)) for i in range(10)]
        o = L.outline_extrude("Star", [(x, z) for x, z in star], 0.0, 0.02, bevel=0.006, segs=1)
        o.rotation_euler = (math.radians(90), 0, 0)
        o.location = L.U(0.12 * G, 0.45 * H, 0.26 * G)
        part(o, "body", accent)
    elif ac == "beanie":
        part(L.sphere("Beanie", hr * 1.02, (0, hy + hr * 0.25, 0), scale=(1.0, 0.75, 1.0), segs=24, rings=14), "head", accent)
        part(L.sphere("Pompom", hr * 0.2, (0, hy + hr * 1.0, 0), segs=12, rings=8), "head", M.get("pom", "#FFFFFF", 0.9))

    # hide the top half of the head inside the beanie/hat looks fine; bake
    tex, em = L.bake(m, size=1024, ao_samples=32, tex_dir=L.ART_DIR + "/Textures")
    final = L.final_material(f"M_{name}", tex, em, 0.5)

    BONES = {
        "root": ((0, 0, 0), (0, 0.12, 0), None),
        "body": ((0, 0.14 * H, 0), (0, 0.55 * H, 0), "root"),
        "head": ((0, 0.58 * H, 0), (0, hy + hr, 0), "body"),
        "jaw": ((0, hy - 0.06 * H, hr * 0.7), (0, hy - 0.1 * H, hr * 1.2), "head"),
        "eye_L": ((-hr * 0.38, hy, hr * 0.84), (-hr * 0.38, hy + 0.08 * H, hr * 0.84), "head"),
        "eye_R": ((hr * 0.38, hy, hr * 0.84), (hr * 0.38, hy + 0.08 * H, hr * 0.84), "head"),
        "ear_L": ((-hr * 0.5, hy + hr * 0.6, 0), (-hr * 0.6, hy + hr * 1.2, 0), "head"),
        "ear_R": ((hr * 0.5, hy + hr * 0.6, 0), (hr * 0.6, hy + hr * 1.2, 0), "head"),
        "arm_L": ((-0.27 * G, 0.52 * H, 0.02), (-0.36 * G, 0.33 * H, 0.05), "body"),
        "arm_R": ((0.27 * G, 0.52 * H, 0.02), (0.36 * G, 0.33 * H, 0.05), "body"),
        "hand_L": ((-0.36 * G, 0.33 * H, 0.05), (-0.39 * G, 0.22 * H, 0.07), "arm_L"),
        "hand_R": ((0.36 * G, 0.33 * H, 0.05), (0.39 * G, 0.22 * H, 0.07), "arm_R"),
        "leg_L": ((-0.15 * G, 0.18 * H, 0.02), (-0.16 * G, 0.02, 0.08), "root"),
        "leg_R": ((0.15 * G, 0.18 * H, 0.02), (0.16 * G, 0.02, 0.08), "root"),
        "tail": ((0, 0.2 * H, -0.25 * G), (0, 0.35 * H, -0.45 * G), "body"),
    }
    objs = []
    for obj, node, slot in m.parts:
        obj.data.materials.clear()
        obj.data.materials.append(final)
        for b in BONES:
            obj.vertex_groups.new(name=b)
        b = bone_of[obj]
        obj.vertex_groups[b].add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
        objs.append(obj)
    mesh = L.join(objs, f"{name}_Mesh")
    arm_data = bpy.data.armatures.new(f"{name}_Rig")
    arm = bpy.data.objects.new(name, arm_data)
    scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for b, (h, t, p) in BONES.items():
        eb = arm_data.edit_bones.new(b)
        eb.head, eb.tail, eb.roll = L.U(*h), L.U(*t), 0.0
    for b, (h, t, p) in BONES.items():
        if p:
            arm_data.edit_bones[b].parent = arm_data.edit_bones[p]
    bpy.ops.object.mode_set(mode="OBJECT")
    mesh.parent = arm
    mesh.modifiers.new("Armature", "ARMATURE").object = arm
    REST = {bn.name: bn.matrix_local.to_quaternion() for bn in arm_data.bones}

    def pose(b, rot=None, loc=(0, 0, 0), scale=(1, 1, 1)):
        pb = arm.pose.bones[b]
        pb.rotation_mode = "QUATERNION"
        q = Quaternion()
        for axis, deg in (rot or []):
            q = Quaternion(L.U(*axis).normalized(), math.radians(-deg)) @ q
        r = REST[b]
        pb.rotation_quaternion = r.inverted() @ q @ r
        pb.location = r.inverted() @ L.U(*loc)
        pb.scale = scale

    X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
    S, TAU = math.sin, 2 * math.pi
    pers = {"Bear": 0.7, "Rabbit": 1.3, "Fox": 1.0, "Elephant": 0.6, "Cat": 0.9, "Penguin": 1.4}[name]  # bounce energy

    def ears(t, amt=1.0):
        pose("ear_L", [(Z, -8 * amt * S(t * TAU))])
        pose("ear_R", [(Z, 8 * amt * S(t * TAU + 0.5))])

    def idle(t):
        br = S(t * TAU)
        pose("body", [(Z, 2 * br)], scale=(1 + 0.012 * br, 1 - 0.015 * br, 1))
        pose("head", [(Z, -3 * S(t * TAU + 0.7)), (X, 2 * S(t * TAU * 2))])
        ears(t, 0.6)
        pose("tail", [(Y, 12 * S(t * TAU))])
        for s, sg in (("L", -1), ("R", 1)):
            pose(f"arm_{s}", [(Z, sg * 4 * br)])
            pose(f"eye_{s}", scale=(1, 0.1 if abs(t - 0.8) < 0.03 else 1, 1))

    def walk(t):
        hop = abs(S(t * TAU))
        pose("root", loc=(0, 0.04 * hop * pers, 0))
        pose("body", [(Z, 6 * S(t * TAU)), (X, 4)])
        pose("leg_L", [(X, 30 * S(t * TAU))])
        pose("leg_R", [(X, -30 * S(t * TAU))])
        pose("arm_L", [(X, -25 * S(t * TAU))])
        pose("arm_R", [(X, 25 * S(t * TAU))])
        pose("head", [(Z, -4 * S(t * TAU))])
        ears(t * 2, 1.2)
        pose("tail", [(Y, 18 * S(t * TAU))])

    def dance(t):
        b = abs(S(t * TAU))
        pose("root", loc=(0, 0.07 * b * pers, 0))
        pose("body", [(Z, 12 * S(t * TAU))], scale=(1 + 0.04 * (1 - b), 1 - 0.05 * (1 - b), 1))
        pose("head", [(Z, -10 * S(t * TAU)), (X, 5 * S(t * TAU * 2))])
        pose("arm_L", [(Z, -(70 + 50 * max(0, S(t * TAU))))])
        pose("arm_R", [(Z, 70 + 50 * max(0, -S(t * TAU)))])
        pose("leg_L", [(X, 15 * max(0, S(t * TAU)))])
        pose("leg_R", [(X, 15 * max(0, -S(t * TAU)))])
        ears(t * 2, 1.5)
        pose("tail", [(Y, 25 * S(t * TAU))])

    def celebrate(t):
        k = S(min(1.0, t / 0.8) * math.pi)
        pose("root", [(Y, 360 * min(1.0, t / 0.8))], loc=(0, 0.35 * k * pers, 0))
        pose("arm_L", [(Z, -150 * k)])
        pose("arm_R", [(Z, 150 * k)])
        pose("body", scale=(1 - 0.05 * k, 1 + 0.08 * k, 1 - 0.05 * k))
        ears(t * 3, 2.0)

    def rest(t):
        br = S(t * TAU)
        pose("body", [(X, 12)], scale=(1 + 0.03 * br, 1 - 0.03 * br, 1))
        pose("head", [(X, 22), (Z, 10)])
        for s in ("L", "R"):
            pose(f"eye_{s}", scale=(1, 0.08, 1))
        pose("ear_L", [(Z, 20)])
        pose("ear_R", [(Z, -20)])
        pose("arm_L", [(Z, -6)])
        pose("arm_R", [(Z, 6)])

    def react(t):
        k = S(min(1.0, t / 0.5) * math.pi)
        pose("root", loc=(0, 0.14 * k * pers, 0))
        pose("arm_L", [(Z, -80 * k)])
        pose("arm_R", [(Z, 80 * k)])
        ears(t * 4, 2.0 * k)
        for s in ("L", "R"):
            pose(f"eye_{s}", scale=(1, 1 - 0.4 * k, 1))

    # ---- playing: Hold<Style> = loop while performing (instrument held), Play<Style> = one hit (0.3 s) per note
    def hold(style, t):
        br = S(t * TAU)
        pose("body", [(Z, 2 * br)])
        pose("head", [(Z, 3 * S(t * TAU))])
        if style == "Drums":
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -55), (Z, sg * 20)])
        elif style == "Keys":
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -70), (Z, sg * 12)])
        elif style == "Bow":   # violin under the chin (left hand up), bow arm across
            pose("arm_L", [(X, -80), (Z, -30)])
            pose("arm_R", [(X, -60), (Z, 50)])
            pose("head", [(Z, 14)])
        elif style == "Strum":
            pose("arm_L", [(X, -45), (Z, -60)])
            pose("arm_R", [(X, -35), (Z, 30)])
        elif style == "Blow":  # instrument to the mouth
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -95), (Z, sg * 10)])
            pose("head", [(X, -6)])
        elif style == "Shake":
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -40), (Z, sg * 45)])
        ears(t, 0.5)

    def play(style, t):
        hold(style, 0.0)
        k = S(min(1.0, t / 0.6) * math.pi)  # quick out-and-back
        pose("body", [(X, 4 * k)], scale=(1 + 0.03 * k, 1 - 0.04 * k, 1))
        pose("head", [(X, 8 * k)])
        if style == "Drums":
            sw = "L" if t < 0.5 else "R"
            pose(f"arm_{sw}", [(X, -55 - 35 * k), (Z, (-20 if sw == "L" else 20))])
        elif style == "Keys":
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -70 + 12 * k), (Z, sg * 12)])
                pose(f"hand_{s}", [(X, 25 * k)])
        elif style == "Bow":
            pose("arm_L", [(X, -80), (Z, -30)])
            pose("arm_R", [(X, -60), (Z, 50 - 45 * k)])
            pose("head", [(Z, 14 + 4 * k)])
        elif style == "Strum":
            pose("arm_L", [(X, -45), (Z, -60)])
            pose("arm_R", [(X, -35 + 30 * k), (Z, 30)])
        elif style == "Blow":
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -95 - 8 * k), (Z, sg * 10)])
            pose("body", scale=(1 + 0.06 * k, 1 + 0.03 * k, 1 + 0.06 * k))  # puff up
        elif style == "Shake":
            for s, sg in (("L", -1), ("R", 1)):
                pose(f"arm_{s}", [(X, -40 - 25 * k), (Z, sg * (45 + 15 * k))])
        pose("ear_L", [(Z, -15 * k)])
        pose("ear_R", [(Z, 15 * k)])

    def clip(cname, seconds, fn):
        act = bpy.data.actions.new(cname)
        act.use_fake_user = True
        arm.animation_data_create()
        arm.animation_data.action = act
        frames = max(2, int(round(seconds * FPS)))
        for f in range(frames + 1):
            for b in BONES:
                pose(b)
            fn(f / frames)
            for b in BONES:
                pb = arm.pose.bones[b]
                pb.keyframe_insert("rotation_quaternion", frame=f + 1)
                pb.keyframe_insert("location", frame=f + 1)
                pb.keyframe_insert("scale", frame=f + 1)
        arm.animation_data.action = None

    clip("Idle", 2.0 + 0.3 * (pers - 1), idle)
    clip("Walk", 0.7, walk)
    clip("Dance", 1.0, dance)
    clip("Celebrate", 1.2, celebrate)
    clip("Rest", 3.0, rest)
    clip("React", 0.5, react)
    for st in STYLES:
        clip(f"Hold{st}", 2.0, lambda t, st=st: hold(st, t))
        clip(f"Play{st}", 0.3, lambda t, st=st: play(st, t))
    arm.animation_data.action = bpy.data.actions["Idle"]
    scene.frame_set(1)
    print(f"[LMW] {name}: {len(mesh.data.polygons)} faces, {len(BONES)} bones, {6 + 2 * len(STYLES)} clips")
    L.export_fbx(arm, L.ART_DIR + f"/Characters/{name}.fbx")
    L.save_source(name)
    if PREVIEW:
        arm.animation_data.action = bpy.data.actions[f"Hold{a['style']}"]
        scene.frame_set(1)
        L.render_preview(L.RENDER_DIR + f"/{name}", [((0.4, 0.9, 2.6), (0, 0.55, 0))], size=(640, 640))


names = [n for n in ARGS if n in ANIMALS] or list(ANIMALS)
for n in names:
    build(n, ANIMALS[n])
print("[LMW] animals done:", " ".join(names))
