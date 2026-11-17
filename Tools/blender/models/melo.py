"""Melo the songbird — Little Music World's original mascot (Art Bible §3).

Plump coral pear-shaped bird with cream belly/face, sunflower beak and feet, a treble-clef swirl crest, blush cheeks
and big glossy eyes. Faces Unity +Z. Height ≈ 1.1 m incl. crest. One skinned mesh (one draw call), 13 bones,
1024² baked atlas. Clips: Idle, Walk, Wave, Dance, Celebrate, Sing, Talk, Sleep, Point, React (30 fps).
Outputs: Assets/Art/Characters/Melo.fbx, Assets/Art/Textures/T_Melo.png, Blender/SourceModels/Melo.blend,
Blender/Renders/Melo_*.png
"""
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L  # noqa: E402

PREVIEW = "--preview" in sys.argv
NAME = "Melo"
FPS = 30

L.reset()
scene = bpy.context.scene
scene.render.fps = FPS
mats = L.Mats()
coral = mats.get("coral", "#FF7A59", rough=0.62)
cream = mats.get("cream", "#FFF4E0", rough=0.7)
sun = mats.get("sun", "#FFC93C", rough=0.45)
ink = mats.get("ink", "#2E2A4A", rough=0.12)
white = mats.get("white", "#FFFFFF", rough=0.1, emit=0.4)
blush = mats.get("blush", "#FF8FA3", rough=0.8)
crest_m = mats.get("crest", "#FF5A5F", rough=0.55)
wing_m = mats.get("wing", "#F2603F", rough=0.6)


def patch_material():
    """Coral body with soft-edged cream belly + face patches (object-space ellipses, anti-aliased when baked)."""
    mt = bpy.data.materials.new("MeloBody")
    nt = mt.node_tree
    n, ln = nt.nodes, nt.links
    bsdf = n["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.65
    tc = n.new("ShaderNodeTexCoord")
    sep = n.new("ShaderNodeSeparateXYZ")
    ln.new(tc.outputs["Object"], sep.inputs[0])

    def math(op, a, b=None, v=None):
        nd = n.new("ShaderNodeMath")
        nd.operation = op
        for i, src in enumerate((a, b)):
            if src is None:
                continue
            if isinstance(src, (int, float)):
                nd.inputs[i].default_value = src
            else:
                ln.new(src, nd.inputs[i])
        return nd.outputs[0]

    def ellipse(cx, cy, rx, ry, front0, front1):
        # x across, z up (Blender), y forward: inside → 1 with a soft 0.06 edge; only on the front.
        dx = math("DIVIDE", math("SUBTRACT", sep.outputs["X"], cx), rx)
        dy = math("DIVIDE", math("SUBTRACT", sep.outputs["Z"], cy), ry)
        d = math("ADD", math("MULTIPLY", dx, dx), math("MULTIPLY", dy, dy))
        inside = math("MINIMUM", math("MAXIMUM", math("MULTIPLY", math("SUBTRACT", 1.0, d), 9.0), 0.0), 1.0)
        fr = math("MINIMUM", math("MAXIMUM", math("DIVIDE", math("SUBTRACT", sep.outputs["Y"], front0), front1 - front0), 0.0), 1.0)
        return math("MULTIPLY", inside, fr)

    belly = ellipse(0.0, 0.27, 0.27, 0.24, 0.02, 0.16)
    face = ellipse(0.0, 0.69, 0.205, 0.165, 0.08, 0.2)
    mask = math("MAXIMUM", belly, face)
    mix = n.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    ln.new(mask, mix.inputs["Factor"])
    mix.inputs[6].default_value = L.lin("#FF7A59")
    mix.inputs[7].default_value = L.lin("#FFF4E0")
    ln.new(mix.outputs[2], bsdf.inputs["Base Color"])
    mt["lmw_kind"] = "patch"
    return mt

m = L.Model(NAME)
bone_of = {}  # object -> bone name ("BODYBLEND" = smooth body/head blend)


def part(obj, bone, mat):
    m.add(obj, "Body", "P", mat)
    bone_of[obj] = bone
    return obj


# ------------------------------------------------------------------ body (lathe pear) with belly/face patches
profile = [(0.0, 0.02), (0.2, 0.03), (0.31, 0.1), (0.365, 0.22), (0.37, 0.32), (0.345, 0.43), (0.31, 0.52),
           (0.305, 0.6), (0.31, 0.69), (0.285, 0.8), (0.2, 0.9), (0.08, 0.95), (0.0, 0.955)]
body = L.lathe("Body", profile, (0, 0, 0), axis="y", segs=40)
sub = body.modifiers.new("Sub", "SUBSURF")
sub.levels = 1
L._bake_modifiers(body, None, weighted=False)
body.data.materials.clear()
body.data.materials.append(patch_material())
m.parts.append((body, "Body", "P"))
bone_of[body] = "BODYBLEND"

# ------------------------------------------------------------------ face
for side, sx in (("L", -1), ("R", 1)):
    x = sx * 0.105
    eye = part(L.sphere(f"Eye{side}", 0.068, (x, 0.705, 0.255), scale=(1.0, 1.15, 0.55), segs=20, rings=12), f"eye_{side}", ink)
    part(L.sphere(f"EyeHi{side}", 0.022, (x + 0.022, 0.735, 0.292), segs=10, rings=6), f"eye_{side}", white)
    part(L.sphere(f"EyeHi2{side}", 0.010, (x - 0.02, 0.68, 0.29), segs=8, rings=5), f"eye_{side}", white)
    part(L.sphere(f"Cheek{side}", 0.05, (sx * 0.185, 0.61, 0.235), scale=(1.0, 0.7, 0.35), segs=14, rings=8), "head", blush)
upper = part(L.cyl("BeakUpper", 0.062, 0.12, (0, 0.625, 0.33), axis="z", segs=16, bevel=0.012, radius2=0.012), "head", sun)
lower = part(L.cyl("BeakLower", 0.045, 0.085, (0, 0.585, 0.315), axis="z", segs=14, bevel=0.01, radius2=0.01), "jaw", sun)

# ------------------------------------------------------------------ crest: treble-clef swirl
spiral = []
for i in range(34):
    a = i / 33 * math.pi * 1.75
    r = 0.16 * (1 - 0.55 * i / 33)
    spiral.append((0.0, 0.93 + 0.1 * (i / 33) + r * math.sin(a) * 0.9, 0.02 - r * math.cos(a) + 0.13))
part(L.sweep("Crest", spiral, L.circle(0.05, 12), ref=(1, 0, 0), taper=lambda t: (1.0 - 0.55 * t, 1.0 - 0.55 * t)), "crest", crest_m)
part(L.sphere("CrestTip", 0.04, spiral[-1], segs=12, rings=8), "crest", crest_m)
for sx in (-1, 1):
    part(L.sphere(f"CrestSide{sx}", 0.07, (sx * 0.06, 0.96, -0.02), scale=(0.45, 1.0, 0.6), segs=12, rings=8,
                  euler=(-20, 0, sx * 28)), "crest", crest_m)

# ------------------------------------------------------------------ wings, feet, tail
for side, sx in (("L", -1), ("R", 1)):
    w = part(L.sphere(f"Wing{side}", 0.16, (sx * 0.35, 0.33, 0.0), scale=(0.38, 1.0, 0.75), segs=18, rings=12,
                      euler=(0, 0, sx * 14)), f"wing_{side}", wing_m)
    foot = part(L.sphere(f"Foot{side}", 0.075, (sx * 0.14, 0.03, 0.09), scale=(1.0, 0.42, 1.45), segs=16, rings=8), f"foot_{side}", sun)
    for t, ang in enumerate((-28, 0, 28)):
        part(L.sphere(f"Toe{side}{t}", 0.032, (sx * 0.14 + math.sin(math.radians(ang)) * 0.09, 0.025,
                                               0.09 + math.cos(math.radians(ang)) * 0.1), scale=(1, 0.7, 1.2), segs=10, rings=6),
             f"foot_{side}", sun)
for i, ang in enumerate((-24, 0, 24)):
    part(L.sphere(f"Tail{i}", 0.09, (math.sin(math.radians(ang)) * 0.08, 0.26, -0.37), scale=(0.55, 0.35, 1.25), segs=14,
                  rings=8, euler=(-35, ang, 0)), "tail", crest_m if i == 1 else wing_m)

# ------------------------------------------------------------------ bake atlas
tex, em = L.bake(m, size=1024, ao_samples=48, tex_dir=L.ART_DIR + "/Textures")
final = L.final_material(f"M_{NAME}", tex, em, 0.6)

# ------------------------------------------------------------------ vertex groups + join into one skinned mesh
BONES = {
    # name: (head, tail, parent)  — Unity coordinates
    "root": ((0, 0, 0), (0, 0.15, 0), None),
    "body": ((0, 0.1, 0), (0, 0.5, 0), "root"),
    "head": ((0, 0.5, 0), (0, 0.9, 0), "body"),
    "crest": ((0, 0.9, 0.02), (0, 1.08, 0.08), "head"),
    "eye_L": ((-0.105, 0.66, 0.255), (-0.105, 0.75, 0.255), "head"),
    "eye_R": ((0.105, 0.66, 0.255), (0.105, 0.75, 0.255), "head"),
    "jaw": ((0, 0.6, 0.27), (0, 0.585, 0.37), "head"),
    "wing_L": ((-0.31, 0.47, 0.0), (-0.37, 0.22, 0.0), "body"),
    "wing_R": ((0.31, 0.47, 0.0), (0.37, 0.22, 0.0), "body"),
    "foot_L": ((-0.14, 0.12, 0.02), (-0.14, 0.02, 0.12), "root"),
    "foot_R": ((0.14, 0.12, 0.02), (0.14, 0.02, 0.12), "root"),
    "tail": ((0, 0.22, -0.28), (0, 0.32, -0.45), "body"),
}


def smooth(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


objs = []
for obj, node, slot in m.parts:
    obj.data.materials.clear()
    obj.data.materials.append(final)
    for bname in BONES:
        obj.vertex_groups.new(name=bname)
    b = bone_of[obj]
    if b == "BODYBLEND":
        vg_b, vg_h = obj.vertex_groups["body"], obj.vertex_groups["head"]
        for v in obj.data.vertices:
            uy = (obj.matrix_world @ v.co).z
            h = smooth(0.44, 0.6, uy)
            if h < 1:
                vg_b.add([v.index], 1.0 - h, "REPLACE")
            if h > 0:
                vg_h.add([v.index], h, "REPLACE")
    else:
        obj.vertex_groups[b].add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    objs.append(obj)
mesh = L.join(objs, f"{NAME}_Mesh")

# ------------------------------------------------------------------ armature
arm_data = bpy.data.armatures.new(f"{NAME}_Rig")
arm = bpy.data.objects.new(NAME, arm_data)
scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
for bname, (h, t, parent) in BONES.items():
    eb = arm_data.edit_bones.new(bname)
    eb.head = L.U(*h)
    eb.tail = L.U(*t)
    eb.roll = 0.0
for bname, (h, t, parent) in BONES.items():
    if parent:
        arm_data.edit_bones[bname].parent = arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")
mesh.parent = arm
mod = mesh.modifiers.new("Armature", "ARMATURE")
mod.object = arm

# ------------------------------------------------------------------ animation helpers (Unity-space intent)
REST = {b.name: b.matrix_local.to_quaternion() for b in arm_data.bones}


def q_unity(axis, deg):
    """Rotation by deg about a Unity axis, expressed in Blender armature space (U is a reflection → angle negated)."""
    return Quaternion(L.U(*axis).normalized(), math.radians(-deg))


def pose(bname, rot=None, loc=(0, 0, 0), scale=(1, 1, 1)):
    """rot: list of (unity_axis, degrees) applied in order (about the bone head, armature space)."""
    pb = arm.pose.bones[bname]
    pb.rotation_mode = "QUATERNION"
    q = Quaternion()
    for axis, deg in (rot or []):
        q = q_unity(axis, deg) @ q
    r = REST[bname]
    pb.rotation_quaternion = r.inverted() @ q @ r
    pb.location = r.inverted() @ L.U(*loc)
    sx, sy, sz = scale  # unity scale (x, y=up, z=forward) → bone local (bone Y = up for vertical bones)
    pb.scale = (sx, sy, sz)


X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)


def clip(name, seconds, fn, loop=True):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = act
    frames = max(2, int(round(seconds * FPS)))
    for f in range(frames + 1):
        t = f / frames
        for b in BONES:
            pose(b)
        fn(t)
        for b in BONES:
            pb = arm.pose.bones[b]
            pb.keyframe_insert("rotation_quaternion", frame=f + 1)
            pb.keyframe_insert("location", frame=f + 1)
            pb.keyframe_insert("scale", frame=f + 1)
    act["lmw_loop"] = loop
    arm.animation_data.action = None
    return act


S = math.sin
TAU = 2 * math.pi


def blink(t, at):
    d = abs(t - at)
    return 1.0 - 0.9 * max(0.0, 1.0 - d / 0.035)


def idle(t):
    br = S(t * TAU)
    pose("body", [(Z, 2 * S(t * TAU))], scale=(1 + 0.015 * br, 1 - 0.02 * br, 1 + 0.015 * br))
    pose("head", [(Z, -3 * S(t * TAU + 0.6)), (X, 2 * S(t * TAU * 2))])
    pose("crest", [(X, 6 * S(t * TAU - 0.8))])
    for s, sgn in (("L", -1), ("R", 1)):
        pose(f"wing_{s}", [(Z, sgn * (4 + 3 * S(t * TAU)))])
        pose(f"eye_{s}", scale=(1, blink(t, 0.72), 1))
    pose("tail", [(X, 6 * S(t * TAU * 2))])


def walk(t):
    hop = abs(S(t * TAU))
    pose("root", loc=(0, 0.06 * hop, 0))
    pose("body", [(X, 6), (Z, 7 * S(t * TAU))], scale=(1 + 0.04 * (1 - hop), 1 - 0.05 * (1 - hop), 1))
    pose("head", [(Z, -5 * S(t * TAU)), (X, -4)])
    pose("crest", [(X, 12 * S(t * TAU * 2 - 1))])
    pose("foot_L", [(X, 28 * S(t * TAU))], loc=(0, 0.05 * max(0, S(t * TAU)), 0))
    pose("foot_R", [(X, -28 * S(t * TAU))], loc=(0, 0.05 * max(0, -S(t * TAU)), 0))
    for s, sgn in (("L", -1), ("R", 1)):
        pose(f"wing_{s}", [(Z, sgn * (14 + 10 * S(t * TAU * 2)))])
    pose("tail", [(Y, 12 * S(t * TAU))])


def wave(t):
    idle(t)
    pose("wing_R", [(Z, 135 + 18 * S(t * TAU * 2)), (X, -10)])
    pose("head", [(Z, 8), (X, 3 * S(t * TAU * 2))])
    pose("body", [(Z, 4)])


def dance(t):
    beat = abs(S(t * TAU))  # two bounces per loop
    pose("root", loc=(0, 0.08 * beat, 0))
    pose("body", [(Z, 12 * S(t * TAU))], scale=(1 + 0.05 * (1 - beat), 1 - 0.06 * (1 - beat), 1 + 0.05 * (1 - beat)))
    pose("head", [(Z, -10 * S(t * TAU)), (X, 6 * S(t * TAU * 2))])
    pose("crest", [(Z, 16 * S(t * TAU - 1.2))])
    pose("wing_L", [(Z, -(60 + 50 * max(0, S(t * TAU))))])
    pose("wing_R", [(Z, 60 + 50 * max(0, -S(t * TAU)))])
    pose("foot_L", loc=(0, 0.04 * max(0, S(t * TAU)), 0))
    pose("foot_R", loc=(0, 0.04 * max(0, -S(t * TAU)), 0))
    pose("tail", [(Y, 20 * S(t * TAU))])


def celebrate(t):
    if t < 0.18:      # crouch
        k = t / 0.18
        pose("root", loc=(0, 0, 0))
        pose("body", scale=(1 + 0.12 * k, 1 - 0.16 * k, 1 + 0.12 * k))
        for s, sgn in (("L", -1), ("R", 1)):
            pose(f"wing_{s}", [(Z, sgn * 10)])
    elif t < 0.78:    # jump + spin
        k = (t - 0.18) / 0.6
        pose("root", [(Y, 360 * (k * k * (3 - 2 * k)))], loc=(0, 0.45 * S(k * math.pi), 0))
        pose("body", scale=(1 - 0.06 * S(k * math.pi), 1 + 0.1 * S(k * math.pi), 1 - 0.06 * S(k * math.pi)))
        for s, sgn in (("L", -1), ("R", 1)):
            pose(f"wing_{s}", [(Z, sgn * (120 + 30 * S(k * TAU * 2)))])
            pose(f"eye_{s}", scale=(1, 0.55, 1))
        pose("crest", [(X, -25 * S(k * math.pi))])
        pose("foot_L", [(X, -20)])
        pose("foot_R", [(X, -20)])
    else:             # land squash and settle
        k = (t - 0.78) / 0.22
        sq = S(k * math.pi) * (1 - k)
        pose("body", scale=(1 + 0.14 * sq, 1 - 0.18 * sq, 1 + 0.14 * sq))
        for s, sgn in (("L", -1), ("R", 1)):
            pose(f"wing_{s}", [(Z, sgn * 120 * (1 - k))])
        pose("crest", [(X, 20 * sq)])


def sing(t):
    idle(t)
    open_ = max(0.0, S(t * TAU * 2))
    pose("jaw", [(X, 28 * open_)])
    pose("head", [(Z, 9 * S(t * TAU)), (X, -6 - 4 * open_)])
    pose("crest", [(X, 10 * S(t * TAU * 2 - 1))])
    for s in ("L", "R"):
        pose(f"eye_{s}", scale=(1, 0.65, 1))


def talk(t):
    idle(t)
    pose("jaw", [(X, 22 * max(0.0, S(t * TAU * 3)))])
    pose("head", [(X, 4 * S(t * TAU * 3)), (Z, 3 * S(t * TAU))])
    pose("wing_R", [(Z, 30 + 15 * S(t * TAU * 1.5)), (X, -20)])


def sleep(t):
    br = S(t * TAU)
    pose("body", [(X, 10)], scale=(1 + 0.03 * br, 1 - 0.035 * br, 1 + 0.03 * br))
    pose("head", [(X, 18), (Z, 8)])
    pose("crest", [(X, 25 + 4 * br)])
    for s, sgn in (("L", -1), ("R", 1)):
        pose(f"eye_{s}", scale=(1, 0.08, 1))
        pose(f"wing_{s}", [(Z, sgn * 2)])
    pose("tail", [(X, -8)])


def point(t):
    k = min(1.0, t / 0.35)
    e = 1 - (1 - k) ** 3
    idle(t)
    pose("wing_L", [(Z, -95 * e), (Y, -40 * e)])
    pose("body", [(Y, -12 * e)])
    pose("head", [(Y, -20 * e), (X, 6 * e)])


def react(t):
    hop = S(min(1.0, t / 0.6) * math.pi)
    pose("root", loc=(0, 0.18 * hop, 0))
    pose("body", scale=(1 - 0.05 * hop, 1 + 0.08 * hop, 1 - 0.05 * hop))
    pose("crest", [(X, -30 * hop)])
    for s, sgn in (("L", -1), ("R", 1)):
        pose(f"wing_{s}", [(Z, sgn * 70 * hop)])
        pose(f"eye_{s}", scale=(1, 1 - 0.4 * hop, 1))


CLIPS = [("Idle", 2.4, idle, True), ("Walk", 0.7, walk, True), ("Wave", 1.2, wave, True), ("Dance", 1.0, dance, True),
         ("Celebrate", 1.5, celebrate, False), ("Sing", 1.0, sing, True), ("Talk", 0.6, talk, True),
         ("Sleep", 3.0, sleep, True), ("Point", 1.0, point, False), ("React", 0.6, react, False)]
for name, sec, fn, loop in CLIPS:
    clip(name, sec, fn, loop)
    print(f"[LMW] clip {name} {sec}s loop={loop}")

# rest pose for export/preview
arm.animation_data.action = bpy.data.actions["Idle"]
scene.frame_set(1)
print(f"[LMW] {NAME}: {len(mesh.data.polygons)} faces, {len(BONES)} bones, {len(CLIPS)} clips")
L.export_fbx(arm, L.ART_DIR + f"/Characters/{NAME}.fbx")
L.save_source(NAME)
if PREVIEW:
    views = [((0.0, 0.9, 2.6), (0, 0.5, 0)), ((1.8, 1.2, 1.9), (0, 0.5, 0))]
    L.render_preview(L.RENDER_DIR + f"/{NAME}", views)
    for name, frame in (("Celebrate", 26), ("Wave", 10), ("Dance", 8)):
        arm.animation_data.action = bpy.data.actions[name]
        scene.frame_set(frame)
        L.render_preview(L.RENDER_DIR + f"/{NAME}_{name}", [views[0]])
