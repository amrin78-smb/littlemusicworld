using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>Generates every scene from code (re-runnable). Layout reference resolution 1920×1080.</summary>
    public static partial class SceneBuilder
    {
        public const string SceneFolder = "Assets/Scenes";

        public static void BuildAll()
        {
            E.EnsureFolder(SceneFolder);
            WorldBuilder.BuildPrefabs();
            AreaBuilder.BuildInstrumentPrefabs();
            ActivityBuilder.BuildAnimalPrefabs();
            PortraitRenderer.RenderAll();
            BuildSkin();
            var scenes = new System.Collections.Generic.List<string> { WorldBuilder.BuildBoot(), WorldBuilder.BuildMainMenu(), WorldBuilder.BuildHub() };
            scenes.AddRange(AreaBuilder.BuildAll());
            scenes.Add(ActivityBuilder.BuildOrchestra());
            scenes.Add(ActivityBuilder.BuildStudio());
            scenes.Add(ActivityBuilder.BuildCarnival());
            ProjectConfigurator.ConfigureBuildScenes(scenes.ToArray());
        }

        // ------------------------------------------------------------------ shared pieces

        /// <summary>Resources/UISkin: sprites + font for UI built at runtime.</summary>
        public static void BuildSkin()
        {
            var skin = E.LoadOrCreateSO<LittleMusicWorld.UI.UISkin>("Assets/Resources/UISkin.asset");
            skin.pebble = UISprites.Get("Pebble"); skin.panel = UISprites.Get("Panel"); skin.circle = UISprites.Get("Circle");
            skin.ring = UISprites.Get("Ring"); skin.glow = UISprites.Get("Glow"); skin.glowRect = UISprites.Get("GlowRect");
            skin.keyWhite = UISprites.Get("KeyWhite"); skin.keyBlack = UISprites.Get("KeyBlack"); skin.note = UISprites.Get("Note");
            skin.sparkle = UISprites.Get("Sparkle");
            skin.iconPlay = UISprites.Get("IconPlay"); skin.iconStop = UISprites.Get("IconStop"); skin.iconRecord = UISprites.Get("IconRecord");
            skin.iconBack = UISprites.Get("IconBack"); skin.iconHome = UISprites.Get("IconHome"); skin.iconStar = UISprites.Get("IconStar");
            skin.iconListen = UISprites.Get("IconListen"); skin.iconCheck = UISprites.Get("IconCheck"); skin.iconTrash = UISprites.Get("IconTrash");
            skin.iconUp = UISprites.Get("IconUp"); skin.iconDown = UISprites.Get("IconDown"); skin.iconLock = UISprites.Get("IconLock");
            skin.font = UIKit.Font();
            EditorUtility.SetDirty(skin);
        }

        public static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        public static string Save(Scene s, string name)
        {
            string path = $"{SceneFolder}/{name}.unity";
            EditorSceneManager.SaveScene(s, path);
            return path;
        }

        public static Camera MakeCamera(Color background, Vector3 pos, Vector3 euler, float fov = 40f)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 250f;
            return cam;
        }

        public static Light MakeSun(Vector3 euler, float intensity = 1.15f)
        {
            var go = new GameObject("Sun", typeof(Light));
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.GetComponent<Light>();
            l.type = LightType.Directional;
            l.color = E.Hex("#FFF1D6");
            l.intensity = intensity;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.65f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = E.Hex("#BFE6FF");
            RenderSettings.ambientEquatorColor = E.Hex("#FFE9C7");
            RenderSettings.ambientGroundColor = E.Hex("#7CCB5A");
            return l;
        }

        public static Canvas MakeCanvas(string name, int order = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 1f;
            go.AddComponent<UI.CanvasAspect>();
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Button PebbleButton(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color colour, string icon = null)
        {
            RectTransform rt = Rect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.Get("Pebble");
            img.type = Image.Type.Sliced;
            img.color = colour;
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<UI.PressFeedback>();
            if (icon != null)
            {
                RectTransform ir = Rect("Icon", rt, new Vector2(0.5f, 0.5f), new Vector2(0, 6), size * 0.55f);
                var ii = ir.gameObject.AddComponent<Image>();
                ii.sprite = UISprites.Get(icon);
                ii.preserveAspect = true;
                ii.raycastTarget = false;
            }
            return b;
        }

        public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, Color c, TextAnchor align = TextAnchor.MiddleCenter)
        {
            RectTransform rt = Rect(name, parent, anchor, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

}
}
