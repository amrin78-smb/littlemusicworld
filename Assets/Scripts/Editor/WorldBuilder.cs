using System.Collections.Generic;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.UI;
using LittleMusicWorld.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>Scene layouts for the vertical slice: Boot, Main Menu, Hub and Piano Paradise.</summary>
    public static class WorldBuilder
    {
        private const string P = ArtLibrary.PrefabFolder;
        /// <summary>Areas that exist as scenes; routes to the others end in flower beds until their milestone.</summary>
        public static string[] BuiltAreas = { SceneFlow.PianoParadise, SceneFlow.DrumValley, SceneFlow.StringTown, SceneFlow.WindVillage,
            SceneFlow.OrchestraPark, SceneFlow.MusicStudio, SceneFlow.RhythmCarnival };
        private static readonly string[] HubProps =
            { "Bandstand", "GatePiano", "GateDrums", "GateStrings", "GateWind", "GateOrchestra", "GateStudio", "GateCarnival",
              "LampNote", "TreeRound", "TreeBlossom", "Bush", "FlowerBed", "Bench", "Plaza", "PathSegment" };

        // ------------------------------------------------------------------ prefabs from Blender exports

        public static void BuildPrefabs()
        {
            Material hub = ArtLibrary.AtlasMaterial("HubProps", true, 1.2f);
            foreach (string n in HubProps)
                ArtLibrary.Prop($"Assets/Art/Environments/Hub/{n}.fbx", $"{P}/Hub/{n}.prefab", hub,
                    collider: n == "Bandstand" || n.StartsWith("Gate") || n == "Bench" || n == "FlowerBed");
            ArtLibrary.Prop("Assets/Art/Environments/Hub/HubGround.fbx", $"{P}/Hub/HubGround.prefab", ArtLibrary.AtlasMaterial("HubGround", false));
            ArtLibrary.Prop("Assets/Art/Instruments/GrandPiano.fbx", $"{P}/Instruments/GrandPiano.prefab", ArtLibrary.AtlasMaterial("GrandPiano"),
                collider: false, isStatic: false);
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/GoldNote.fbx") != null)
                ArtLibrary.Prop("Assets/Art/Props/GoldNote.fbx", $"{P}/Props/GoldNote.prefab", ArtLibrary.AtlasMaterial("GoldNote", true, 1.5f),
                    collider: false, isStatic: false);
            ArtLibrary.Character("Assets/Art/Characters/Melo.fbx", $"{P}/Characters/Melo.prefab", ArtLibrary.AtlasMaterial("Melo", true, 0.6f));
        }

        private static GameObject Place(string prefab, Vector3 pos, float rotY = 0f, float scale = 1f, Transform parent = null)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{P}/{prefab}.prefab");
            if (src == null) { Debug.LogError("[LMW] prefab missing " + prefab); return new GameObject(prefab + " (missing)"); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
            go.transform.localScale = Vector3.one * scale;
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        private static void Atmosphere(Color fog, float start, float end)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
        }

        private static MascotController PlaceMelo(Vector3 pos, float rotY)
        {
            GameObject m = Place("Characters/Melo", pos, rotY);
            m.name = "Melo";
            var mc = m.GetComponent<MascotController>();
            return mc != null ? mc : m.AddComponent<MascotController>();
        }

        // ------------------------------------------------------------------ shared UI pieces

        private static Canvas Hud(string name) => SceneBuilder.MakeCanvas(name, 10);

        /// <summary>Hold-to-open gear + parent settings panel (with credits).</summary>
        internal static void ParentArea(Canvas c)
        {
            Button gear = UIKit.Pebble("ParentGear", c.transform, new Vector2(1, 1), new Vector2(-95, -95), new Vector2(120, 120), "#E8DCCB", "IconGear",
                0.6f, UIKit.Ink);
            Object.DestroyImmediate(gear.GetComponent<PressFeedback>());
            Image ring = UIKit.Image("HoldRing", gear.transform, "Ring", E.Hex("#FF5A8A"), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(140, 140));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillAmount = 0f;

            RectTransform panel = UIKit.Stretch("Parent Area", c.transform);
            UIKit.Image("Dim", panel, null, new Color(0.18f, 0.16f, 0.29f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), true);
            UIKit.Image("Card", panel, "Panel", UIKit.Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 860));
            UIKit.Text("Title", panel, "Grown-ups", 64, UIKit.Ink, new Vector2(0.5f, 0.5f), new Vector2(0, 340), new Vector2(1000, 90));
            var sp = panel.gameObject.AddComponent<SettingsPanel>();
            string[] rows = { "Volume", "Music", "Voice" };
            var sliders = new Slider[3];
            for (int i = 0; i < 3; i++)
            {
                UIKit.Text(rows[i] + "Label", panel, rows[i], 44, UIKit.Ink, new Vector2(0.5f, 0.5f), new Vector2(-380, 210 - i * 110), new Vector2(300, 70),
                    TextAlignmentOptions.Left);
                sliders[i] = UIKit.Slider(rows[i], panel, new Vector2(0.5f, 0.5f), new Vector2(150, 210 - i * 110), 640, i == 0 ? "#5B7CFA" : i == 1 ? "#2EC4B6" : "#FF7A59");
            }
            Button en = UIKit.Pebble("English", panel, new Vector2(0.5f, 0.5f), new Vector2(-420, -150), new Vector2(240, 120), "#5B7CFA", null);
            UIKit.Text("Label", en.transform, "English", 40, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(230, 90));
            Button th = UIKit.Pebble("Thai", panel, new Vector2(0.5f, 0.5f), new Vector2(-150, -150), new Vector2(240, 120), "#FF7A59", null);
            UIKit.Text("Label", th.transform, "ภาษาไทย", 40, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(230, 90));
            Button q = UIKit.Pebble("Quality", panel, new Vector2(0.5f, 0.5f), new Vector2(200, -150), new Vector2(380, 120), "#8BD346", null);
            TextMeshProUGUI ql = UIKit.Text("Label", q.transform, "Auto", 36, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(360, 90));
            Button cr = UIKit.Pebble("Credits", panel, new Vector2(0.5f, 0.5f), new Vector2(-230, -310), new Vector2(230, 110), "#D46CE0", null);
            UIKit.Text("Label", cr.transform, "Credits", 36, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(220, 80));
            Button close = UIKit.Pebble("Close", panel, new Vector2(0.5f, 0.5f), new Vector2(480, 330), new Vector2(140, 130), "#8BD346", "IconCheck");
            TextMeshProUGUI info = UIKit.Text("Info", panel, "", 26, new Color(0.18f, 0.16f, 0.29f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(355, -310),
                new Vector2(420, 80));
            Button tut = UIKit.Pebble("ReplayTutorial", panel, new Vector2(0.5f, 0.5f), new Vector2(15, -310), new Vector2(230, 110), "#FFC93C", null);
            UIKit.Text("Label", tut.transform, "Tutorial", 36, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(220, 80));
            E.Set(sp, "replayTutorial", tut);

            RectTransform credits = UIKit.Stretch("Credits Panel", panel);
            UIKit.Image("Bg", credits, "Panel", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 800), true);
            UIKit.Text("Text", credits,
                "<b>Little Music World</b>\nMade by Nocvault\n\n" +
                "Grand piano: Salamander Grand Piano V3 by Alexander Holm (CC BY 3.0)\n" +
                "Orchestral & percussion samples: VSCO 2 Community Edition, Versilian Studios (CC0); upright piano by Simon Dalzell\n" +
                "Guitars, banjo, saxophone, recorder, kit: FluidR3_GM by Frank Wen (MIT)\n" +
                "Fonts: Fredoka & Mitr (SIL Open Font License)\n" +
                "Voices: Kokoro (Apache-2.0), VachanaTTS (MIT)\n" +
                "Songs: traditional and public-domain melodies, arranged for this game.",
                30, UIKit.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160, 720), TextAlignmentOptions.Center);
            Button cc = UIKit.Pebble("CloseCredits", credits, new Vector2(0.5f, 0.5f), new Vector2(560, 340), new Vector2(110, 110), "#8BD346", "IconCheck");
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(cc.onClick, credits.gameObject.SetActive, false);
            credits.gameObject.SetActive(false);

            E.Set(sp, "master", sliders[0]);
            E.Set(sp, "music", sliders[1]);
            E.Set(sp, "voice", sliders[2]);
            E.Set(sp, "english", en);
            E.Set(sp, "thai", th);
            E.Set(sp, "quality", q);
            E.Set(sp, "qualityLabel", ql);
            E.Set(sp, "close", close);
            E.Set(sp, "credits", cr);
            E.Set(sp, "creditsPanel", credits.gameObject);
            E.Set(sp, "info", info);
            Button check = UIKit.Pebble("CheckDevice", panel, new Vector2(0.5f, 0.5f), new Vector2(-450, -310), new Vector2(180, 110), "#5B7CFA", null);
            UIKit.Text("Label", check.transform, "Check", 36, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(170, 80));
            RectTransform checkPanel = UIKit.Stretch("Check Panel", panel);
            UIKit.Image("Bg", checkPanel, "Panel", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 900), true);
            TextMeshProUGUI report = UIKit.Text("Report", checkPanel, "", 22, UIKit.Ink, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1420, 780),
                TextAlignmentOptions.TopLeft);
            Button closeCheck = UIKit.Pebble("CloseCheck", checkPanel, new Vector2(0.5f, 0.5f), new Vector2(680, 390), new Vector2(110, 100), "#8BD346", "IconCheck");
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeCheck.onClick, checkPanel.gameObject.SetActive, false);
            checkPanel.gameObject.SetActive(false);
            E.Set(sp, "checkDevice", check);
            E.Set(sp, "checkReport", report);
            E.Set(sp, "checkPanel", checkPanel.gameObject);
            panel.gameObject.SetActive(false);
            var gate = gear.gameObject.AddComponent<ParentGate>();
            E.Set(gate, "ring", ring);
            E.Set(gate, "panel", panel.gameObject);
        }

        // ------------------------------------------------------------------ Boot

        public static string BuildBoot()
        {
            Scene s = SceneBuilder.NewScene();
            SceneBuilder.MakeCamera(E.Hex("#8FD3FF"), new Vector3(0, 1, -10), Vector3.zero);
            Canvas c = SceneBuilder.MakeCanvas("Launch Canvas");
            RectTransform bg = UIKit.Stretch("Background", c.transform);
            var bgi = bg.gameObject.AddComponent<Image>();
            bgi.sprite = LoadSprite("Assets/Resources/Brand/LoadingBackground.png");
            bgi.color = Color.white;
            Image logo = UIKit.Image("Logo", c.transform, null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1280, 720));
            logo.sprite = LoadSprite("Assets/Resources/Brand/Logo.png");
            logo.preserveAspect = true;
            var boot = new GameObject("Boot").AddComponent<BootLoader>();
            E.Set(boot, "firstScene", SceneFlow.MainMenu);
            return SceneBuilder.Save(s, "Boot");
        }

        public static Sprite LoadSprite(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ Main menu

        public static string BuildMainMenu()
        {
            Scene s = SceneBuilder.NewScene();
            Camera cam = SceneBuilder.MakeCamera(E.Hex("#BFE6FF"), new Vector3(2.2f, 2.0f, -9.0f), new Vector3(6, -8, 0), 38f);
            SceneBuilder.MakeSun(new Vector3(42, -35, 0));
            Atmosphere(E.Hex("#DDF3FF"), 18f, 60f);
            Place("Hub/HubGround", Vector3.zero);
            Place("Hub/Plaza", Vector3.zero);
            Place("Hub/Bandstand", new Vector3(1.0f, 0.12f, 3.5f), 15f);
            Place("Hub/TreeRound", new Vector3(-6.5f, 0, 5f), 30f, 1.2f);
            Place("Hub/TreeBlossom", new Vector3(7.5f, 0, 4.5f), 80f, 1.1f);
            Place("Hub/TreeRound", new Vector3(10f, 0, 10f), 140f, 1.3f);
            Place("Hub/FlowerBed", new Vector3(-3.2f, 0.12f, -2.6f));
            Place("Hub/LampNote", new Vector3(5.6f, 0.12f, 2.8f), 200f);
            Place("Hub/Bush", new Vector3(-5.5f, 0, 1.5f), 10f, 1.2f);
            MascotController melo = PlaceMelo(new Vector3(-0.6f, 0.12f, -3.6f), 165f);
            melo.transform.localScale = Vector3.one * 1.15f;

            Canvas c = Hud("Menu Canvas");
            Image logo = UIKit.Image("Logo", c.transform, null, Color.white, new Vector2(0f, 1f), new Vector2(620, -300), new Vector2(1100, 620));
            logo.sprite = LoadSprite("Assets/Resources/Brand/Logo.png");
            logo.preserveAspect = true;
            Button play = UIKit.Pebble("Play", c.transform, new Vector2(1f, 0f), new Vector2(-330, 250), new Vector2(380, 320), "#FF5A8A", "IconPlay", 0.6f);
            UIKit.Bubble(c.transform, new Vector2(0f, 0f), new Vector2(620, 170), 900);
            ParentArea(c);
            new GameObject("Ambient Music").AddComponent<Audio.AmbientMusic>();
            var ctrl = new GameObject("Main Menu").AddComponent<MainMenuController>();
            E.Set(ctrl, "play", play);
            E.Set(ctrl, "melo", melo);
            E.Set(ctrl, "logo", logo.rectTransform);
            return SceneBuilder.Save(s, "MainMenu");
        }

        // ------------------------------------------------------------------ Hub

        public static string BuildHub()
        {
            Scene s = SceneBuilder.NewScene();
            Camera cam = SceneBuilder.MakeCamera(E.Hex("#BFE6FF"), new Vector3(0, 9.5f, -14f), new Vector3(35, 0, 0), 42f);
            cam.farClipPlane = 120f;
            SceneBuilder.MakeSun(new Vector3(48, -32, 0));
            Atmosphere(E.Hex("#DDF3FF"), 30f, 85f);
            var world = new GameObject("World").transform;
            Place("Hub/HubGround", Vector3.zero, 0, 1, world);
            Place("Hub/Plaza", Vector3.zero, 0, 1, world);
            Place("Hub/Bandstand", new Vector3(0, 0.12f, 0), 180f, 1, world);
            var ground = new GameObject("Ground Collider", typeof(BoxCollider));
            ground.transform.position = new Vector3(0, 0.07f, 0);
            ground.GetComponent<BoxCollider>().size = new Vector3(90, 0.1f, 90);

            // Seven routes (Piano Paradise north; the rest arrive in Milestone 2).
            var rnd = new System.Random(7);
            for (int i = 0; i < 7; i++)
            {
                float ang = i * 360f / 7f;
                Quaternion q = Quaternion.Euler(0, ang, 0);
                Vector3 dir = q * Vector3.forward;
                string[] areaScenes = { SceneFlow.PianoParadise, SceneFlow.DrumValley, SceneFlow.StringTown, SceneFlow.WindVillage,
                    SceneFlow.OrchestraPark, SceneFlow.MusicStudio, SceneFlow.RhythmCarnival };
                string[] areaVoices = { "area_piano", "area_drums", "area_strings", "area_wind", "area_orchestra", "area_studio", "area_carnival" };
                string[] gates = { "GatePiano", "GateDrums", "GateStrings", "GateWind", "GateOrchestra", "GateStudio", "GateCarnival" };
                bool piano = System.Array.IndexOf(BuiltAreas, areaScenes[i]) >= 0;
                int tiles = piano ? 2 : 1;
                for (int t = 0; t < tiles; t++) Place("Hub/PathSegment", dir * (7f + t * 4f) + Vector3.up * 0.02f, ang, 1, world);
                if (piano)
                {
                    string gateModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{P}/Hub/{gates[i]}.prefab") != null ? gates[i] : "GatePiano";
                    GameObject gate = Place("Hub/" + gateModel, dir * 15.4f, ang, 1, world);
                    var col = gate.AddComponent<BoxCollider>();
                    col.center = new Vector3(0, 2.4f, 0);
                    col.size = new Vector3(5f, 5f, 1.6f);
                    var portal = gate.AddComponent<AreaPortal>();
                    portal.sceneName = areaScenes[i];
                    portal.voiceKey = areaVoices[i];
                    var approach = new GameObject("Approach").transform;
                    approach.SetParent(gate.transform, false);
                    approach.localPosition = new Vector3(0, 0, -1.6f);
                    portal.approach = approach;
                }
                else
                {
                    Place("Hub/FlowerBed", dir * 11.8f + Vector3.up * 0.0f, ang, 1.2f, world);
                    Place("Hub/Bush", dir * 13.2f + q * Vector3.right * 1.6f, ang + 40, 1.1f, world);
                    Place("Hub/Bush", dir * 13.2f - q * Vector3.right * 1.6f, ang - 30, 1.0f, world);
                }
                // Lamps between routes, benches facing the bandstand.
                Quaternion mid = Quaternion.Euler(0, ang + 180f / 7f, 0);
                float midAng = ang + 180f / 7f;
                if (Mathf.Abs(Mathf.DeltaAngle(midAng, 180f)) > 30f) // keep the camera's line to Melo clear
                    Place("Hub/LampNote", mid * Vector3.forward * 7.7f, ang + 90, 1, world);
                if (i % 2 == 1) Place("Hub/Bench", mid * Vector3.forward * 9.2f, ang + 180f / 7f + 180f, 1, world);
            }
            // Tree ring, kept clear of the routes.
            for (int i = 0; i < 34; i++)
            {
                float ang = (float)(rnd.NextDouble() * 360.0);
                float near = 99f;
                for (int k = 0; k < 7; k++) near = Mathf.Min(near, Mathf.Abs(Mathf.DeltaAngle(ang, k * 360f / 7f)));
                if (near < 13f) continue;
                float r = 12.5f + (float)rnd.NextDouble() * 9f;
                string kind = rnd.NextDouble() < 0.3 ? "Hub/TreeBlossom" : "Hub/TreeRound";
                Place(kind, Quaternion.Euler(0, ang, 0) * Vector3.forward * r, (float)rnd.NextDouble() * 360f, 0.9f + (float)rnd.NextDouble() * 0.5f, world);
            }

            // Optional golden notes to find around the hub (between the routes, near benches and trees).
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{P}/Props/GoldNote.prefab") != null)
                for (int i = 0; i < 10; i++)
                {
                    float a = i * 36f + 18f;
                    float r = i % 2 == 0 ? 9.6f : 12.2f;
                    GameObject n = Place("Props/GoldNote", Quaternion.Euler(0, a, 0) * Vector3.forward * r + Vector3.up * 1.1f, 0, 1.4f, world);
                    n.name = "GoldNote" + i;
                    var sc = n.AddComponent<SphereCollider>();
                    sc.center = new Vector3(0.1f, 0.4f, 0f);
                    sc.radius = 0.6f;   // generous for small fingers
                    n.AddComponent<Collectible>().id = "note_" + i;
                }
            MascotController melo = PlaceMelo(new Vector3(0, 0.12f, -4.2f), 180f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "Tap Marker";
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.rotation = Quaternion.Euler(90, 0, 0);
            var mm = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mm.SetTexture("_BaseMap", UISprites.Get("Ring").texture);
            mm.SetFloat("_Surface", 1f);
            mm.SetFloat("_Blend", 0f);
            mm.SetOverrideTag("RenderType", "Transparent");
            mm.renderQueue = (int)RenderQueue.Transparent;
            mm.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mm.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mm.SetInt("_ZWrite", 0);
            mm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mm = E.CreateOrReplace(mm, "Assets/Art/Materials/M_TapMarker.mat");
            marker.GetComponent<MeshRenderer>().sharedMaterial = mm;
            marker.SetActive(false);

            Canvas c = Hud("Hub Canvas");
            Button home = UIKit.Pebble("Home", c.transform, new Vector2(0, 1), new Vector2(110, -100), new Vector2(160, 150), "#FFC93C", "IconHome");
            home.gameObject.AddComponent<SceneButton>().scene = SceneFlow.MainMenu;
            UIKit.Bubble(c.transform, new Vector2(0.5f, 0f), new Vector2(0, 120), 1000); // bottom: the way ahead (gates) stays visible
            ActivityBuilder.AddStickerBook(c);
            ParentArea(c);
            new GameObject("Ambient Music").AddComponent<Audio.AmbientMusic>();
            var hub = new GameObject("Hub").AddComponent<HubController>();
            E.Set(hub, "melo", melo);
            E.Set(hub, "cam", cam);
            E.Set(hub, "tapMarker", marker.transform);
            return SceneBuilder.Save(s, "Hub");
        }

    }
}
