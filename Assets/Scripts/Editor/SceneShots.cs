using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Art review: renders every build scene from its main camera (with overlay canvases switched to camera space so
    /// static UI shows) into Logs/Shots/&lt;Scene&gt;.png at 1920×1080 and 2316×1080 (S23 aspect). Runtime-built UI
    /// (e.g. piano keys) is not visible here — device screenshots cover that. Batch: Tools/shots.sh
    /// </summary>
    public static class SceneShots
    {
        public static void RunBatch()
        {
            Directory.CreateDirectory("Logs/Shots");
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                EditorSceneManager.OpenScene(s.path);
                Camera cam = Camera.main;
                if (cam == null) continue;
                foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = cam.nearClipPlane + 0.05f;
                }
                foreach (var (w, h, tag) in new[] { (1920, 1080, ""), (2316, 1080, "_wide") })
                {
                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                    cam.targetTexture = rt;
                    Canvas.ForceUpdateCanvases();
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    string name = Path.GetFileNameWithoutExtension(s.path);
                    File.WriteAllBytes($"Logs/Shots/{name}{tag}.png", tex.EncodeToPNG());
                    RenderTexture.active = null;
                    cam.targetTexture = null;
                    Object.DestroyImmediate(rt);
                    Object.DestroyImmediate(tex);
                    Debug.Log($"[LMW Shots] {name}{tag}");
                }
                // Instrument close-up framings (area scenes): render each station's view at the phone aspect.
                foreach (var st in Object.FindObjectsByType<LittleMusicWorld.World.InstrumentStation>(FindObjectsSortMode.None))
                {
                    if (st.view == null) continue;
                    cam.transform.SetPositionAndRotation(st.view.position, st.view.rotation);
                    var rt2 = new RenderTexture(1158, 540, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                    cam.targetTexture = rt2;
                    cam.Render();
                    RenderTexture.active = rt2;
                    var t2 = new Texture2D(1158, 540, TextureFormat.RGB24, false);
                    t2.ReadPixels(new Rect(0, 0, 1158, 540), 0, 0);
                    t2.Apply();
                    File.WriteAllBytes($"Logs/Shots/View_{st.instrumentId}.png", t2.EncodeToPNG());
                    RenderTexture.active = null;
                    cam.targetTexture = null;
                    Object.DestroyImmediate(rt2);
                    Object.DestroyImmediate(t2);
                }
            }
            EditorApplication.Exit(0);
        }
    }
}
