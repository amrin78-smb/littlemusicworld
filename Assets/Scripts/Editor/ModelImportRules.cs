using UnityEditor;
using UnityEngine;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Import rules for Blender FBX exports under Assets/Art. Models are authored in Unity axes/metres by the
    /// Blender scripts (lmw_blender.U), exported with axis_forward=Z / axis_up=Y; Unity's axis conversion is baked so
    /// transforms come in with identity rotations. Materials are not imported: the generator assigns the shared
    /// URP materials built from each model's baked atlas. Characters (Art/Characters) import their animations.
    /// </summary>
    public sealed class ModelImportRules : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Art/") || !assetPath.EndsWith(".fbx")) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.None;
            mi.importBlendShapes = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.isReadable = false;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.meshOptimizationFlags = MeshOptimizationFlags.Everything;
            mi.weldVertices = true;
            mi.generateSecondaryUV = false;
            mi.addCollider = false;
            bool character = assetPath.StartsWith("Assets/Art/Characters/");
            mi.importAnimation = character;
            mi.animationType = character ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            if (character)
            {
                mi.optimizeGameObjects = false;
                mi.skinWeights = ModelImporterSkinWeights.Custom;
                mi.maxBonesPerVertex = 2; // cheap skinning on budget GPUs
                mi.resampleCurves = true;
                mi.animationCompression = ModelImporterAnimationCompression.Optimal;
            }
        }
    }

    public static class FbxAxisCheck
    {
        public static void RunBatch()
        {
            AssetDatabase.ImportAsset("Assets/Art/Props/AxisTest.fbx", ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/AxisTest.fbx");
            if (prefab == null) { Debug.LogError("[LMW Axis] no AxisTest.fbx"); EditorApplication.Exit(1); return; }
            GameObject go = Object.Instantiate(prefab);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                Debug.Log($"[LMW Axis] {r.name} centre={r.bounds.center:F2} size={r.bounds.size:F2} rot={r.transform.rotation.eulerAngles:F1} scale={r.transform.lossyScale:F2}");
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
                Debug.Log($"[LMW Axis] node {t.name} pos={t.position:F2} localRot={t.localEulerAngles:F1}");
            EditorApplication.Exit(0);
        }
    }
}
