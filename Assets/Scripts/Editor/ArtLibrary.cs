using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Turns the Blender exports (Assets/Art/**.fbx + baked atlases) into ready prefabs:
    /// URP materials from each atlas (albedo×AO in RGB, smoothness in A, optional emission), LOD groups from _LOD0/_LOD1
    /// meshes, optional colliders, and Animator controllers for characters. Re-runnable; prefabs keep their GUIDs.
    /// </summary>
    public static class ArtLibrary
    {
        public const string MaterialFolder = "Assets/Art/Materials";
        public const string PrefabFolder = "Assets/Prefabs";

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>Material for an atlas name (e.g. "GrandPiano" → T_GrandPiano.png).</summary>
        public static Material AtlasMaterial(string atlas, bool lit = true, float emission = 0f)
        {
            string key = atlas + (lit ? "" : "_simple");
            if (Cache.TryGetValue(key, out Material cached) && cached != null) return cached;
            string texPath = $"Assets/Art/Textures/T_{atlas}.png";
            ConfigureTexture(texPath, false);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            string emPath = $"Assets/Art/Textures/T_{atlas}_E.png";
            Texture2D em = null;
            if (emission > 0f && File.Exists(emPath))
            {
                ConfigureTexture(emPath, false);
                em = AssetDatabase.LoadAssetAtPath<Texture2D>(emPath);
            }
            Shader shader = Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Simple Lit");
            var m = new Material(shader) { name = "M_" + key };
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            if (lit)
            {
                m.SetFloat("_Smoothness", 1f);
                m.SetFloat("_SmoothnessTextureChannel", 1f); // albedo alpha
                m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_EnvironmentReflections", 1f);
            }
            else
            {
                m.SetFloat("_Smoothness", 0.35f);
                m.SetFloat("_SmoothnessSource", 0f); // specular alpha (off) — keep cheap
            }
            if (em != null)
            {
                m.SetTexture("_EmissionMap", em);
                m.SetColor("_EmissionColor", Color.white * emission);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            m.enableInstancing = true;
            m = E.CreateOrReplace(m, $"{MaterialFolder}/M_{key}.mat");
            Cache[key] = m;
            return m;
        }

        public static void ConfigureTexture(string path, bool normalMap)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool dirty = false;
            if (ti.textureType != (normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default)) { ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default; dirty = true; }
            if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; dirty = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = false; }
            ti.sRGBTexture = true;
            ti.anisoLevel = 2;
            ti.wrapMode = TextureWrapMode.Clamp;
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.compressionQuality = 50;
            ti.SetPlatformTextureSettings(android);
            ti.maxTextureSize = 2048;
            if (dirty || true) ti.SaveAndReimport();
        }

        /// <summary>Prefab from an FBX: every renderer gets the atlas material; LOD groups; optional collider.</summary>
        public static GameObject Prop(string fbxPath, string prefabPath, Material mat, bool collider = false, bool isStatic = true,
            float lod0 = 0.25f, float cull = 0.02f)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) { Debug.LogError("[LMW Art] missing " + fbxPath); return null; }
            GameObject go = Wrap(src, Path.GetFileNameWithoutExtension(prefabPath));
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            BuildLods(go, lod0, cull);
            if (collider)
            {
                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.name.Contains("_LOD1")) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            if (isStatic) foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            return E.SavePrefab(go, prefabPath);
        }

        /// <summary>
        /// FBX roots carry Unity's axis-conversion rotation (measured: (270,180,0) with an identity world result), so
        /// every prefab gets a clean identity root and the model lives underneath — placing/rotating the prefab can
        /// never undo the conversion.
        /// </summary>
        private static GameObject Wrap(GameObject src, string name)
        {
            var root = new GameObject(name);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetParent(root.transform, false);
            return root;
        }

        private static void BuildLods(GameObject go, float lod0, float cull)
        {
            var lod1 = go.GetComponentsInChildren<Renderer>().Where(r => r.name.EndsWith("_LOD1")).ToArray();
            if (lod1.Length == 0) return;
            var lod0r = go.GetComponentsInChildren<Renderer>().Where(r => !r.name.EndsWith("_LOD1")).ToArray();
            // The FBX importer already builds a LODGroup from _LOD0/_LOD1 names; reuse it and set our thresholds.
            LODGroup group = go.GetComponentInChildren<LODGroup>();
            if (group == null) group = go.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(lod0, lod0r), new LOD(cull, lod1) });
            group.RecalculateBounds();
        }

        // ------------------------------------------------------------------ characters

        public static readonly string[] LoopClips = { "Idle", "Walk", "Wave", "Dance", "Sing", "Talk", "Sleep", "Rest" };

        /// <summary>Character prefab with an Animator controller holding one state per FBX clip (crossfaded from code).</summary>
        public static GameObject Character(string fbxPath, string prefabPath, Material mat)
        {
            string ctrlPath = Path.ChangeExtension(prefabPath, null) + "_Animator.controller";
            ConfigureClips(fbxPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            E.EnsureFolder(Path.GetDirectoryName(ctrlPath));
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            foreach (AnimationClip c in clips.OrderBy(c => c.name == "Idle" ? 0 : 1))
            {
                string state = ClipState(c.name);
                AnimatorState s = sm.AddState(state);
                s.motion = c;
                s.writeDefaultValues = true;
                if (state == "Idle") sm.defaultState = s;
            }
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            GameObject go = Wrap(src, Path.GetFileNameWithoutExtension(prefabPath));
            GameObject model = go.transform.GetChild(0).gameObject;   // clip paths are relative to the FBX root
            Animator anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            foreach (SkinnedMeshRenderer r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.sharedMaterial = mat;
                r.quality = SkinQuality.Bone2;
                r.updateWhenOffscreen = false;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            Debug.Log($"[LMW Art] {go.name}: {clips.Count} clips → {string.Join(", ", clips.Select(c => ClipState(c.name)))}");
            return E.SavePrefab(go, prefabPath);
        }

        /// <summary>Blender exports clips as "Armature|Clip" (or "Melo|Idle"); the state is the part after '|'.</summary>
        public static string ClipState(string clipName)
        {
            int bar = clipName.LastIndexOf('|');
            return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
        }

        private static void ConfigureClips(string fbxPath)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            if (mi == null) return;
            ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            foreach (ModelImporterClipAnimation c in clips)
            {
                string state = ClipState(c.name);
                c.loopTime = LoopClips.Contains(state) || state.StartsWith("Hold");
                c.loopPose = false;
                c.name = state;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }
    }
}
