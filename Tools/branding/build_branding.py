"""Little Music World visual identity — rendered in Blender from the game's own assets (headless, offline).

Produces (editable sources = this script + Blender/SourceModels/Branding_*.blend):
  Assets/Art/Branding/Logo_Master.png        2400×1350 transparent, 3D wordmark + Melo + notes
  Assets/Resources/Brand/Logo.png            1600 px wide (loading curtain, Boot, menu)
  Assets/Art/Branding/Splash_Logo.png        logo for Unity's splash sequence
  Assets/Resources/Brand/LoadingBackground.png  1920×1080 sky/hills background
  Assets/Resources/Brand/Note.png            gold note sprite (loading animation)
  Assets/Art/Branding/Icon_Master.png        1024² full icon (legacy + store)
  Assets/Art/Branding/Icon_Background.png    432² adaptive background layer
  Assets/Art/Branding/Icon_Foreground.png    432² adaptive foreground layer (safe zone respected)
  Blender/Renders/Icon_SmallSizes.png        legibility check at 48/72/96/144 px
Usage: blender -b --factory-startup --python Tools/branding/build_branding.py
"""
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "blender"))
import lmw_blender as L  # noqa: E402

REPO = L.REPO
FONT = REPO + "/Assets/Art/UI/Fonts/Fredoka-SemiBold.ttf"
BRAND = REPO + "/Assets/Art/Branding"
RES = REPO + "/Assets/Resources/Brand"
os.makedirs(BRAND, exist_ok=True)
os.makedirs(RES, exist_ok=True)
PITCH = ["#FF5A5F", "#FFA62B", "#FFE156", "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0"]


# ================================================================================================ helpers
def save_rgba(arr, path):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    img.pixels[:] = np.flipud(arr).astype(np.float32).ravel()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    print("[LMW] wrote", path)


def load_rgba(path):
    img = bpy.data.images.load(path)
    w, h = img.size
    a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return np.flipud(a)


def resize(a, w, h):
    """Area-average downscale (good for small icon previews)."""
    H, W = a.shape[:2]
    ys = (np.arange(h + 1) * H / h).astype(int)
    xs = (np.arange(w + 1) * W / w).astype(int)
    out = np.zeros((h, w, a.shape[2]), np.float32)
    for j in range(h):
        row = a[ys[j]:max(ys[j] + 1, ys[j + 1])].mean(axis=0)
        for i in range(w):
            out[j, i] = row[xs[i]:max(xs[i] + 1, xs[i + 1])].mean(axis=0)
    return out


def srgb(hexstr):
    h = hexstr.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)


def over(dst, src, x, y):
    """Alpha-composite src (h,w,4 straight alpha) onto dst at (x,y) top-left."""
    h, w = src.shape[:2]
    H, W = dst.shape[:2]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
    if x0 >= x1 or y0 >= y1:
        return
    s = src[y0 - y:y1 - y, x0 - x:x1 - x]
    d = dst[y0:y1, x0:x1]
    a = s[..., 3:4]
    d[..., :3] = s[..., :3] * a + d[..., :3] * (1 - a)
    d[..., 3:4] = a + d[..., 3:4] * (1 - a)


# ================================================================================================ 2D paintings
def sky_background(w, h, hills=True):
    y = np.linspace(0, 1, h)[:, None]
    top, bottom = srgb("#7CC8FF"), srgb("#E3F5FF")
    img = np.ones((h, w, 4), np.float32)
    img[..., :3] = top * (1 - y[..., None]) + bottom * y[..., None]
    xx = np.arange(w)[None, :]
    yy = np.arange(h)[:, None]
    rng = np.random.default_rng(3)
    for _ in range(7):  # soft clouds
        cx, cy, r = rng.uniform(0, w), rng.uniform(0.08 * h, 0.4 * h), rng.uniform(0.05, 0.1) * w
        for k in range(5):
            ox, oy, rr = cx + (k - 2) * r * 0.6, cy + rng.uniform(-0.2, 0.2) * r, r * rng.uniform(0.55, 0.9)
            d = np.sqrt((xx - ox) ** 2 + ((yy - oy) * 1.5) ** 2) / rr
            a = np.clip((1.0 - d) * 4, 0, 1) * 0.85
            img[..., :3] = img[..., :3] * (1 - a[..., None]) + a[..., None]
    if hills:
        for base, amp, freq, phase, col in ((0.74, 0.05, 1.6, 0.3, "#9BDB7A"), (0.8, 0.06, 2.3, 1.7, "#7CCB5A"),
                                            (0.88, 0.04, 3.1, 0.9, "#5FB54E")):
            ridge = (base + amp * np.sin(np.linspace(0, freq * math.pi, w) + phase)) * h
            mask = np.clip((yy - ridge[None, :]) / 2.0, 0, 1)
            shade = 1 - 0.12 * np.clip((yy - ridge[None, :]) / (0.2 * h), 0, 1)
            c = srgb(col)[None, None, :] * shade[..., None]
            img[..., :3] = img[..., :3] * (1 - mask[..., None]) + c * mask[..., None]
    return img


def note_sprite(size, colour="#FFFFFF"):
    """Anti-aliased eighth note (white, tinted in-game)."""
    s = size
    yy, xx = np.mgrid[0:s, 0:s].astype(np.float32) + 0.5
    yy = s - yy
    c, sn = math.cos(-0.45), math.sin(-0.45)
    hx, hy = xx - s * 0.36, yy - s * 0.27
    rx, ry = hx * c - hy * sn, hx * sn + hy * c
    head = np.sqrt((rx / (s * 0.2)) ** 2 + (ry / (s * 0.14)) ** 2) - 1
    stem = np.maximum(np.abs(xx - s * 0.54) - s * 0.045, np.abs(yy - s * 0.58) - s * 0.33)
    flag = np.sqrt(((xx - s * 0.6) / (s * 0.2)) ** 2 + ((yy - s * 0.74) / (s * 0.16)) ** 2) - 1
    inner = np.sqrt(((xx - s * 0.55) / (s * 0.17)) ** 2 + ((yy - s * 0.66) / (s * 0.12)) ** 2) - 1
    flagd = np.maximum(flag, -inner)
    flagd[(xx < s * 0.54) | (yy < s * 0.56)] = 1
    d = np.minimum(np.minimum(head * s * 0.14, stem), flagd * s * 0.14)
    a = np.clip(0.5 - d, 0, 1)
    out = np.zeros((s, s, 4), np.float32)
    out[..., :3] = srgb(colour)
    out[..., 3] = a
    return out


# ================================================================================================ 3D scene parts
def scene_setup(res_x, res_y, samples=64):
    L.reset()
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = res_x, res_y
    sc.view_settings.view_transform = "Standard"
    sc.world.use_nodes = True
    bg = sc.world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = L.lin("#FFF4E6")
    bg.inputs["Strength"].default_value = 0.75
    key = bpy.data.objects.new("Key", bpy.data.lights.new("Key", "SUN"))
    key.data.energy = 3.2
    key.data.angle = math.radians(12)
    key.rotation_euler = (math.radians(50), math.radians(-18), math.radians(-20))
    sc.collection.objects.link(key)
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "SUN"))
    fill.data.energy = 1.2
    fill.data.color = (1.0, 0.92, 0.85)
    fill.rotation_euler = (math.radians(70), math.radians(30), math.radians(140))
    sc.collection.objects.link(fill)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    return sc, cam


def glossy(name, hexstr, rough=0.25):
    m = bpy.data.materials.new(name)
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = L.lin(hexstr)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Coat Weight"].default_value = 0.35
    return m


def text3d(s, size, pos, mat, extrude=0.12, bevel=0.035):
    cu = bpy.data.curves.new(s, "FONT")
    cu.body = s
    cu.font = bpy.data.fonts.load(FONT)
    cu.size = size
    cu.extrude = extrude
    cu.bevel_depth = bevel
    cu.bevel_resolution = 4
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    ob = bpy.data.objects.new(s, cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = pos
    ob.rotation_euler = (math.radians(90), 0, 0)  # stand upright facing -Y (camera)
    ob.data.materials.append(mat)
    return ob


def outline_text(s, size, pos, extrude, bevel, grow):
    """Thick ink outline behind letters (sticker look), pushed back slightly."""
    ob = text3d(s, size, (pos[0], pos[1] + 0.06, pos[2]), glossy("Ink_" + s, "#2E2A4A", 0.4), extrude * 0.6, bevel + grow)
    return ob


def note3d(pos, scale, mat, rot=0.0):
    head = bpy.data.objects.new("NoteHead", bpy.data.meshes.new("nh"))
    bpy.context.scene.collection.objects.link(head)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.22 * scale, location=pos)
    h = bpy.context.active_object
    h.scale = (1.25, 0.6, 0.9)
    h.rotation_euler = (0, math.radians(-25 + rot), 0)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.045 * scale, depth=0.85 * scale,
                                        location=(pos[0] + 0.24 * scale, pos[1], pos[2] + 0.4 * scale))
    st = bpy.context.active_object
    bpy.ops.mesh.primitive_torus_add(major_radius=0.22 * scale, minor_radius=0.05 * scale,
                                     location=(pos[0] + 0.42 * scale, pos[1], pos[2] + 0.66 * scale))
    fl = bpy.context.active_object
    fl.rotation_euler = (math.radians(90), math.radians(30), 0)
    fl.scale = (1.0, 0.7, 1.0)
    for o in (h, st, fl):
        o.data.materials.append(mat)
        for p in o.data.polygons:
            p.use_smooth = True
    bpy.data.objects.remove(head)
    return [h, st, fl]


def import_melo(location, rot_z=0.0, scale=1.0, action="Wave", frame=10):
    path = REPO + "/Blender/SourceModels/Melo.blend"
    if not os.path.exists(path):
        print("[LMW] Melo.blend missing; logo without mascot")
        return None
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in ("Melo", "Melo_Mesh")]
        dst.actions = list(src.actions)
    arm = None
    for o in dst.objects:
        bpy.context.scene.collection.objects.link(o)
        if o.type == "ARMATURE":
            arm = o
    arm.location = location
    arm.rotation_euler = (0, 0, rot_z)
    arm.scale = (scale, scale, scale)
    arm.animation_data_create()
    if action in bpy.data.actions:
        arm.animation_data.action = bpy.data.actions[action]
        bpy.context.scene.frame_set(frame)
    return arm


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[LMW] rendered", path)


def look(cam, pos, target, lens=50):
    cam.location = pos
    cam.data.lens = lens
    d = Vector(target) - Vector(pos)
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()


# ================================================================================================ 1. logo
def word3d(text, size, pos, colours, extrude, bevel, outline=0.06):
    """One text object (proper kerning) with a material per character, plus an ink sticker outline behind it."""
    mats = [glossy(f"W_{text}_{i}", c, 0.22) for i, c in enumerate(colours)]
    ob = text3d(text, size, pos, mats[0], extrude, bevel)
    for m in mats[1:]:
        ob.data.materials.append(m)
    ci = 0
    for i, ch in enumerate(text):
        if ch == " ":
            continue
        ob.data.body_format[i].material_index = ci % len(mats)
        ci += 1
    ink = text3d(text, size, (pos[0], pos[1] + 0.08, pos[2]), glossy("Ink_" + text, "#2E2A4A", 0.4), extrude * 0.6, bevel + outline)
    return ob, ink


def fit_ortho(cam, objs, margin=0.06, aspect=16 / 9):
    """Orthographic front camera (looking +Y) framing the objects' bounds."""
    bpy.context.view_layer.update()
    xs, zs = [], []
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            xs.append(w.x)
            zs.append(w.z)
    cx, cz = (min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2
    w, h = max(xs) - min(xs), max(zs) - min(zs)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(w, h * aspect) * (1 + 2 * margin)
    cam.location = (cx, -20, cz)
    cam.rotation_euler = (math.radians(90), 0, 0)


def crop_to_content(a, aspect, pad=0.03):
    """Crops transparent margins, then centres the content on a canvas of the given aspect ratio."""
    ys, xs = np.nonzero(a[..., 3] > 0.02)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    c = a[y0:y1, x0:x1]
    h, w = c.shape[:2]
    W = int(max(w, h * aspect) * (1 + 2 * pad))
    H = int(W / aspect)
    out = np.zeros((H, W, 4), np.float32)
    out[(H - h) // 2:(H - h) // 2 + h, (W - w) // 2:(W - w) // 2 + w] = c
    return resize(out, 2400, 1350) if W != 2400 else out


def build_logo():
    sc, cam = scene_setup(2400, 1350, samples=48)
    objs = []
    objs += word3d("Little", 1.0, (-0.9, 0, 1.3), ["#FFF4E0"], 0.14, 0.04)
    objs += word3d("Music World", 1.35, (0, 0, -0.05), PITCH, 0.2, 0.05)
    objs += note3d((3.65, -0.1, 1.05), 1.05, glossy("Gold", "#F2C14E", 0.2), 10)
    objs += note3d((-3.85, -0.1, 0.75), 0.7, glossy("Pink", "#FF5A8A", 0.25), -15)
    melo = import_melo((1.75, 0.3, 0.72), rot_z=math.radians(168), scale=1.15, action="Wave", frame=12)
    if melo is not None:
        objs += [o for o in bpy.data.objects if o.type == "MESH" and o.parent == melo]
    fit_ortho(cam, objs, margin=0.04)
    tmp = BRAND + "/_logo_render.png"
    render(tmp)
    a = crop_to_content(load_rgba(tmp), 16 / 9, pad=0.03)
    os.remove(tmp)
    save_rgba(a, BRAND + "/Logo_Master.png")
    save_rgba(resize(a, 1600, 900), RES + "/Logo.png")
    save_rgba(resize(a, 1200, 675), BRAND + "/Splash_Logo.png")
    L.save_source("Branding_Logo")
    return a


# ================================================================================================ 2. icon
def build_icon():
    sc, cam = scene_setup(1024, 1024, samples=64)
    import_melo((0, 0, 0), rot_z=math.radians(18), scale=1.0, action="Sing", frame=8)
    gold = glossy("Gold", "#F2C14E", 0.2)
    note3d((-0.46, 0.2, 0.92), 0.42, gold, -10)
    # Melo faces Unity +Z = Blender +Y; camera in front (Blender +Y side) looking back.
    look(cam, (0.3, 2.6, 0.82), (0.0, 0.0, 0.66), lens=85)
    tmp = BRAND + "/_icon_fg.png"
    render(tmp)
    fg = load_rgba(tmp)
    os.remove(tmp)
    L.save_source("Branding_Icon")

    # Background: warm radial gradient + soft rings of notes (no tiny details).
    n = 1024
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    r = np.sqrt((xx - n * 0.5) ** 2 + (yy - n * 0.42) ** 2) / (n * 0.75)
    inner, outer = srgb("#FFE9A8"), srgb("#FF8A5B")
    bg = np.ones((n, n, 4), np.float32)
    t = np.clip(r, 0, 1)[..., None]
    bg[..., :3] = inner * (1 - t) + outer * t
    ring = np.abs(r - 0.62)
    bg[..., :3] += (np.clip(1 - ring / 0.02, 0, 1) * 0.08)[..., None]
    for i in range(7):
        a = i / 7 * math.tau + 0.4
        ns = note_sprite(90, "#FFFFFF")
        ns[..., 3] *= 0.28
        over(bg, ns, int(n / 2 + math.cos(a) * n * 0.4) - 45, int(n * 0.45 + math.sin(a) * n * 0.4) - 45)

    # Adaptive icon: 108dp canvas, foreground must sit in the inner 66 % safe zone.
    save_rgba(resize(bg, 432, 432), BRAND + "/Icon_Background.png")
    fg_canvas = np.zeros((n, n, 4), np.float32)
    safe = int(n * 0.66)
    fg_small = resize(fg, safe, safe)
    over(fg_canvas, fg_small, (n - safe) // 2, (n - safe) // 2 + 10)
    save_rgba(resize(fg_canvas, 432, 432), BRAND + "/Icon_Foreground.png")

    # Legacy/full icon: background + larger foreground, rounded square mask.
    full = bg.copy()
    over(full, resize(fg, int(n * 0.86), int(n * 0.86)), int(n * 0.07), int(n * 0.1))
    rr = n * 0.22
    qx = np.maximum(np.abs(xx - n / 2 + 0.5) - (n / 2 - rr), 0)
    qy = np.maximum(np.abs(yy - n / 2 + 0.5) - (n / 2 - rr), 0)
    mask = np.clip(rr - np.sqrt(qx ** 2 + qy ** 2) + 0.5, 0, 1)
    full[..., 3] = mask
    save_rgba(full, BRAND + "/Icon_Master.png")

    # Small-size legibility preview (launcher sizes 48/72/96/144 px) on a phone-like wallpaper.
    sheet = sky_background(900, 260, hills=False)
    x = 30
    for s in (48, 72, 96, 144):
        icon = resize(full, s, s)
        over(sheet, icon, x, 260 - s - 40)
        x += s + 60
    # adaptive circle mask preview
    circ = resize(bg, 144, 144)
    over(circ, resize(fg_canvas, 144, 144), 0, 0)
    yy2, xx2 = np.mgrid[0:144, 0:144].astype(np.float32)
    circ[..., 3] = np.clip(72 - np.sqrt((xx2 - 71.5) ** 2 + (yy2 - 71.5) ** 2) + 0.5, 0, 1)
    over(sheet, circ, x + 20, 260 - 144 - 40)
    save_rgba(sheet, L.RENDER_DIR + "/Icon_SmallSizes.png")


# ================================================================================================ 3. backgrounds
def build_backgrounds():
    save_rgba(sky_background(1920, 1080), RES + "/LoadingBackground.png")
    save_rgba(note_sprite(128), RES + "/Note.png")


if __name__ == "__main__":
    build_backgrounds()
    build_logo()
    build_icon()
    print("[LMW] branding done")
