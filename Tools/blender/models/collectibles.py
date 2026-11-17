"""Collectible golden notes for the hub (optional discovery objects, GDD 5.1). Small own atlas (T_Collectibles)."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L  # noqa: E402

L.reset()
M = L.Mats()
gold = M.get("gold", "#FFD23F", rough=0.15, emit=0.35)
m = L.Model("GoldNote")
m.add(L.sphere("Head", 0.16, (0.0, 0.18, 0.0), scale=(1.25, 0.9, 0.7), segs=24, rings=14, euler=(0, 0, 25)), "Body", "P", gold)
m.add(L.cyl("Stem", 0.028, 0.5, (0.17, 0.42, 0.0), axis="y", segs=12, bevel=0.01), "Body", "P", gold)
m.add(L.sweep("Flag", [(0.17, 0.66, 0.0), (0.28, 0.6, 0.0), (0.34, 0.48, 0.0), (0.3, 0.38, 0.0)], L.circle(0.035, 10),
              ref=(0, 0, 1), taper=lambda t: (1.0, 1.0 - 0.5 * t)), "Body", "P", gold)
m.cull_bottom = False
tex, em = L.bake(m, size=256, ao_samples=16, tex_dir=L.ART_DIR + "/Textures")
mat = L.final_material("M_Collectibles", tex, em, 1.0)
for o, n, s in m.parts:
    o.data.materials.clear()
    o.data.materials.append(mat)
root, _, _ = L.build_hierarchy(m, "GoldNote")
L.export_fbx(root, L.ART_DIR + "/Props/GoldNote.fbx")
L.save_source("GoldNote")
print("[LMW] GoldNote: done")
