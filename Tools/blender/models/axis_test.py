"""Pipeline check: markers at Unity +X, +Y, +Z and a pivoted child. Unity reports where they land (FbxAxisCheck)."""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
import lmw_blender as L

L.reset()
mats = L.Mats()
m = L.Model("AxisTest")
m.node("Child", "Body", (0.0, 1.0, 2.0))
m.add(L.rbox("RightX", (0.2, 0.2, 0.2), (1.0, 0.1, 0.0), 0.02), "Body", "P", mats.get("r", "#FF0000"))
m.add(L.rbox("UpY", (0.2, 0.2, 0.2), (0.0, 1.0, 0.0), 0.02), "Body", "P", mats.get("g", "#00FF00"))
m.add(L.rbox("FwdZ", (0.2, 0.2, 0.6), (0.0, 0.1, 1.0), 0.02), "Child", "P", mats.get("b", "#0000FF"))
root, empties, meshes = L.build_hierarchy(m, "AxisTest")
L.export_fbx(root, L.REPO + "/Assets/Art/Props/AxisTest.fbx")
