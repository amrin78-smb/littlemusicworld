using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Renders instrument and character prefabs into transparent 256² sprites (Assets/Art/UI/Portraits/Portrait_&lt;id&gt;.png)
    /// from a friendly three-quarter angle with the game's lighting — the pictures children tap on every card.
    /// </summary>
    public static class PortraitRenderer
    {
        public const string Folder = "Assets/Art/UI/Portraits";
        public const int Size = 256;

        public static void RenderBatch() { RenderAll(); EditorApplication.Exit(0); }

        public static List<Sprite> RenderAll()
        {
            E.EnsureFolder(Folder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("PortraitCam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = E.Hex("#FFF1D6");
            sun.transform.rotation = Quaternion.Euler(40, -35, 0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = E.Hex("#DDEFFF");
            RenderSettings.ambientEquatorColor = E.Hex("#FFEAD0");
            RenderSettings.ambientGroundColor = E.Hex("#C8B8A0");
            RenderSettings.fog = false;

            var ids = new List<(string id, string prefab)>();
            foreach (string id in AreaBuilder.AtlasOf.Keys) ids.Add((id, $"{ArtLibrary.PrefabFolder}/Instruments/{id}.prefab"));
            foreach (string a in new[] { "Melo", "Bear", "Rabbit", "Fox", "Elephant", "Cat", "Penguin" })
                ids.Add((a, $"{ArtLibrary.PrefabFolder}/Characters/{a}.prefab"));

            var sprites = new List<Sprite>();
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            foreach (var (id, path) in ids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds b = AreaBuilder.WorldBounds(go);
                float radius = b.extents.magnitude;
                // Instruments face −Z, characters face +Z: look at the front from a friendly three-quarter angle, a bit above.
                bool character = path.Contains("/Characters/");
                Vector3 offset = new Vector3(0.45f, 0.35f, character ? 1f : -1f).normalized;
                cam.transform.position = b.center + offset * radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.95f;
                cam.transform.LookAt(b.center);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                string file = $"{Folder}/Portrait_{id}.png";
                File.WriteAllBytes(file, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(go);
                UISprites.ConfigureSprite(file, Vector4.zero);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(file));
            }
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Debug.Log($"[LMW] portraits rendered: {sprites.Count}");
            return sprites;
        }

        public static Sprite[] All()
        {
            var list = new List<Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { Folder }))
                list.Add(AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)));
            return list.ToArray();
        }
    }
}
