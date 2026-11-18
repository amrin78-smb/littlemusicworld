using System.Collections.Generic;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.UI;
using LittleMusicWorld.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Instrument areas from a declarative spec: environment, instrument stations (each with a computed camera view
    /// that frames the instrument above its play panel), Melo, and the shared area UI.
    /// </summary>
    public static class AreaBuilder
    {
        public sealed class Station
        {
            public string id;          // instrument id = prefab name
            public Vector3 pos;
            public float rotY;
            public float scale = 1f;
            public float viewYaw;      // camera angle relative to the instrument's front (profile views)
        }

        public sealed class Spec
        {
            public string scene, areaId, voice;
            public string sky, fog, accent;
            public Vector3 overviewPos, overviewLook;
            public Vector3 meloPos;
            public float meloRot;
            public List<Station> stations = new List<Station>();
            public System.Action<Transform> decorate;
        }

        public static readonly Dictionary<string, string> AtlasOf = new Dictionary<string, string>
        {
            { "GrandPiano", "GrandPiano" }, { "UprightPiano", "KeyInstruments" }, { "DigitalKeyboard", "KeyInstruments" },
            { "DrumKit", "DrumInstruments" }, { "Snare", "DrumInstruments" }, { "BassDrum", "DrumInstruments" },
            { "Cymbals", "DrumInstruments" }, { "Bongos", "DrumInstruments" }, { "Congas", "DrumInstruments" },
            { "Tambourine", "DrumInstruments" }, { "Maracas", "DrumInstruments" }, { "Xylophone", "DrumInstruments" },
            { "AcousticGuitar", "StringInstruments" }, { "ElectricGuitar", "StringInstruments" }, { "Ukulele", "StringInstruments" },
            { "Violin", "StringInstruments" }, { "Cello", "StringInstruments" }, { "Harp", "StringInstruments" }, { "Banjo", "StringInstruments" },
            { "Flute", "WindInstruments" }, { "Trumpet", "WindInstruments" }, { "Saxophone", "WindInstruments" },
            { "Clarinet", "WindInstruments" }, { "Trombone", "WindInstruments" }, { "Recorder", "WindInstruments" },
        };

        public static void BuildInstrumentPrefabs()
        {
            foreach (var kv in AtlasOf)
            {
                string fbx = $"Assets/Art/Instruments/{kv.Key}.fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null) { Debug.LogWarning("[LMW] instrument model missing: " + kv.Key); continue; }
                ArtLibrary.Prop(fbx, $"{ArtLibrary.PrefabFolder}/Instruments/{kv.Key}.prefab", ArtLibrary.AtlasMaterial(kv.Value, true, 1f),
                    collider: false, isStatic: false);
            }
        }

        private static GameObject Place(string prefab, Vector3 pos, float rotY = 0f, float scale = 1f, Transform parent = null)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtLibrary.PrefabFolder}/{prefab}.prefab");
            if (src == null) { Debug.LogWarning("[LMW] prefab missing " + prefab); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotY, 0));
            go.transform.localScale = Vector3.one * scale;
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        public static string Build(Spec spec)
        {
            Scene s = SceneBuilder.NewScene();
            Camera cam = SceneBuilder.MakeCamera(E.Hex(spec.sky), spec.overviewPos, Vector3.zero, 44f);
            cam.transform.LookAt(spec.overviewLook);
            SceneBuilder.MakeSun(new Vector3(48, -30, 0), 1.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = E.Hex(spec.fog);
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 60f;
            var world = new GameObject("World").transform;
            Place("Hub/HubGround", Vector3.zero, 0, 1, world);
            Place("Hub/Plaza", Vector3.zero, 0, 1.1f, world);
            spec.decorate?.Invoke(world);

            var overview = new GameObject("Overview").transform;
            overview.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);

            var stations = new List<InstrumentStation>();
            foreach (Station st in spec.stations)
            {
                GameObject go = Place($"Instruments/{st.id}", st.pos, st.rotY, st.scale);
                if (go == null) continue;
                go.name = st.id;
                Bounds b = WorldBounds(go);
                var box = go.AddComponent<BoxCollider>();
                box.center = go.transform.InverseTransformPoint(b.center);
                box.size = new Vector3(Mathf.Max(0.5f, b.size.x / st.scale), Mathf.Max(0.6f, b.size.y / st.scale), Mathf.Max(0.5f, b.size.z / st.scale)) * 1.1f;
                var station = go.AddComponent<InstrumentStation>();
                station.instrumentId = st.id;
                station.model = go.AddComponent<InstrumentModel3D>();
                station.bounce = go.transform;
                // Camera view: in front of the instrument, a little above, framing it in the top half of the screen
                // (the play panel covers the bottom half).
                // While playing, the visible 3D band is between the top buttons and the play panel (~32 % of the
                // screen height, centred ~63 % up). Fit the instrument into that band by height and by width.
                float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float visH = 2f * tanHalf;                    // visible height per metre of distance
                float d = Mathf.Max(b.size.y / (0.5f * visH), b.size.x / (0.55f * visH * 2.14f), 1.3f);
                Vector3 front = Quaternion.Euler(0, st.rotY + st.viewYaw, 0) * Vector3.back;
                var view = new GameObject("View").transform;
                view.SetParent(go.transform, true);
                view.position = b.center + front * d + Vector3.up * (0.08f * d);
                // aim below the centre so the instrument appears at ~63 % of the screen height
                view.rotation = Quaternion.LookRotation(b.center + Vector3.down * (0.3f * tanHalf * d) - view.position);
                station.view = view;
                stations.Add(station);
            }

            MascotController melo = null;
            var meloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtLibrary.PrefabFolder}/Characters/Melo.prefab");
            if (meloPrefab != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(meloPrefab);
                m.name = "Melo";
                m.transform.SetPositionAndRotation(spec.meloPos, Quaternion.Euler(0, spec.meloRot, 0));
                melo = m.AddComponent<MascotController>();
            }

            // ---------------------------------------------------------------- UI
            Canvas c = SceneBuilder.MakeCanvas("Area Canvas", 10);
            RectTransform panelArea = UIKit.Rect("Instrument Panel", c.transform, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            panelArea.anchorMin = new Vector2(0f, 0f);
            panelArea.anchorMax = new Vector2(1f, 0f);
            panelArea.pivot = new Vector2(0.5f, 0f);
            panelArea.offsetMin = new Vector2(60, 28);
            panelArea.offsetMax = new Vector2(-60, 28 + 500);
            var panel = panelArea.gameObject.AddComponent<InstrumentPanel>();
            E.Set(panel, "area", panelArea);

            RectTransform overviewBar = UIKit.Rect("Overview Bar", c.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overviewBar.anchorMin = Vector2.zero;
            overviewBar.anchorMax = Vector2.one;
            Button home = UIKit.Pebble("Home", overviewBar, new Vector2(0, 1), new Vector2(110, -100), new Vector2(160, 150), "#FFC93C", "IconHome");
            home.gameObject.AddComponent<SceneButton>().scene = SceneFlow.Hub;

            RectTransform playBar = UIKit.Rect("Play Bar", c.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            playBar.anchorMin = Vector2.zero;
            playBar.anchorMax = Vector2.one;
            Button back = UIKit.Pebble("Back", playBar, new Vector2(0, 1), new Vector2(110, -100), new Vector2(160, 150), spec.accent, "IconBack");
            Button songs = UIKit.Pebble("Songs", playBar, new Vector2(1, 1), new Vector2(-300, -100), new Vector2(170, 150), "#FF5A8A", "IconStar");
            Button listen = UIKit.Pebble("Listen", playBar, new Vector2(1, 1), new Vector2(-480, -100), new Vector2(160, 150), "#5B7CFA", "IconListen");
            RectTransform bar = UIKit.Rect("Progress", playBar, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(720, 40));
            UIKit.Image("Track", bar, "Panel", new Color(1, 1, 1, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 40));
            Image fill = UIKit.Image("Fill", bar, "Panel", E.Hex("#FFC93C"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 26));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.sprite = UISprites.Get("Circle");

            SongPicker picker = BuildSongPicker(c);
            UIKit.Bubble(c.transform, new Vector2(0f, 1f), new Vector2(700, -100), 860); // in the top bar, never over the instrument

            var guideGo = new GameObject("Melody Guide");
            MelodyGuide guide = guideGo.AddComponent<MelodyGuide>();
            var ctrl = new GameObject("Area").AddComponent<AreaController>();
            E.Set(ctrl, "areaId", spec.areaId);
            E.Set(ctrl, "introVoice", spec.voice);
            E.Set(ctrl, "cam", cam);
            E.Set(ctrl, "overview", overview);
            if (melo != null) E.Set(ctrl, "melo", melo);
            E.SetArray(ctrl, "stations", stations.ToArray());
            E.Set(ctrl, "panel", panel);
            E.Set(ctrl, "playBar", playBar.gameObject);
            E.Set(ctrl, "overviewBar", overviewBar.gameObject);
            E.Set(ctrl, "backButton", back);
            E.Set(ctrl, "songsButton", songs);
            E.Set(ctrl, "listenButton", listen);
            E.Set(ctrl, "guide", guide);
            E.Set(ctrl, "songPicker", picker);
            E.Set(ctrl, "progressFill", fill);
            WorldBuilder.ParentArea(c);
            return SceneBuilder.Save(s, spec.scene);
        }

        public static SongPicker BuildSongPicker(Canvas c)
        {
            RectTransform panel = UIKit.Stretch("Song Picker", c.transform);
            UIKit.Image("Dim", panel, null, new Color(0.18f, 0.16f, 0.29f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), true);
            UIKit.Image("Card", panel, "Panel", UIKit.Cream, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1600, 820));
            var tiers = new Button[4];
            string[] tierCols = { "#8BD346", "#2EC4B6", "#5B7CFA", "#D46CE0" };
            for (int t = 0; t < 4; t++)
            {
                tiers[t] = UIKit.Pebble("Tier" + t, panel, new Vector2(0.5f, 0.5f), new Vector2(-360 + t * 240, 290), new Vector2(200, 130), tierCols[t], null);
                for (int d = 0; d <= t; d++)
                    UIKit.Image("Dot", tiers[t].transform, "Circle", Color.white, new Vector2(0.5f, 0.5f), new Vector2((d - t * 0.5f) * 34f, 6), new Vector2(26, 26));
            }
            RectTransform grid = UIKit.Rect("Cards", panel, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1480, 520));
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(190, 190);
            gl.spacing = new Vector2(24, 24);
            gl.childAlignment = TextAnchor.MiddleCenter;
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 7; // 14 cards per tier
            Button close = UIKit.Pebble("Close", panel, new Vector2(0.5f, 0.5f), new Vector2(720, 330), new Vector2(130, 120), "#8BD346", "IconBack");
            var picker = panel.gameObject.AddComponent<SongPicker>();
            E.Set(picker, "cardGrid", grid);
            E.SetArray(picker, "tierButtons", tiers);
            E.Set(picker, "closeButton", close);
            var icons = new List<Object>();
            foreach (string n in new[] { "IconStar", "IconLamb", "IconBell", "IconBun", "IconSun", "IconBoat", "IconHeart", "IconBus" }) icons.Add(UISprites.Get(n));
            E.SetArray(picker, "songIcons", icons.ToArray());
            panel.gameObject.SetActive(false);
            return picker;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // ------------------------------------------------------------------ the four instrument areas

        private static Station St(string id, float x, float z, float rot = 0f, float scale = 1f) =>
            new Station { id = id, pos = new Vector3(x, 0.13f, z), rotY = rot, scale = scale };

        public static List<string> BuildAll()
        {
            var scenes = new List<string>();
            scenes.Add(Build(new Spec
            {
                scene = SceneFlow.PianoParadise, areaId = SceneFlow.PianoParadise, voice = "area_piano",
                sky = "#D9CCFF", fog = "#E6DDFF", accent = "#5B5FC7",
                overviewPos = new Vector3(0, 4.2f, -7.4f), overviewLook = new Vector3(0, 0.6f, 0.6f),
                meloPos = new Vector3(-0.2f, 0.13f, -2.4f), meloRot = 180f,
                stations = { St("GrandPiano", 0f, 1.2f, 0f), St("UprightPiano", -3.3f, 0.6f, 25f), St("DigitalKeyboard", 3.3f, 0.4f, -25f) },
                decorate = w => Decor(w, "TreeBlossom", "LampNote")
            }));
            scenes.Add(Build(new Spec
            {
                scene = SceneFlow.DrumValley, areaId = SceneFlow.DrumValley, voice = "area_drums",
                sky = "#FFD9B8", fog = "#FFE6CF", accent = "#E2553F",
                overviewPos = new Vector3(0, 5.0f, -8.4f), overviewLook = new Vector3(0, 0.5f, 0.8f),
                meloPos = new Vector3(0f, 0.13f, -2.8f), meloRot = 180f,
                stations =
                {
                    St("DrumKit", 0f, 1.6f, 0f, 1.25f), St("Snare", -2.0f, 0.9f, 20f), St("BassDrum", 2.1f, 0.9f, -20f),
                    St("Cymbals", -3.4f, 1.9f, 35f), St("Congas", 3.5f, 1.9f, -35f), St("Bongos", -3.9f, -0.4f, 55f),
                    St("Xylophone", 3.9f, -0.4f, -55f), St("Tambourine", -1.6f, -1.0f, 15f), St("Maracas", 1.6f, -1.0f, -15f),
                },
                decorate = w => Decor(w, "TreeRound", "LampNote")
            }));
            scenes.Add(Build(new Spec
            {
                scene = SceneFlow.StringTown, areaId = SceneFlow.StringTown, voice = "area_strings",
                sky = "#FFE9B8", fog = "#FFF1D2", accent = "#C68B59",
                overviewPos = new Vector3(0, 4.4f, -7.6f), overviewLook = new Vector3(0, 0.6f, 0.8f),
                meloPos = new Vector3(0f, 0.13f, -2.6f), meloRot = 180f,
                stations =
                {
                    St("Harp", 0f, 1.8f, 0f), St("AcousticGuitar", -1.6f, 1.0f, 15f), St("ElectricGuitar", 1.6f, 1.0f, -15f),
                    St("Cello", -3.0f, 1.6f, 30f), St("Violin", 3.0f, 1.6f, -30f), St("Ukulele", -2.6f, -0.4f, 40f), St("Banjo", 2.6f, -0.4f, -40f),
                },
                decorate = w => Decor(w, "TreeRound", "LampNote")
            }));
            scenes.Add(Build(new Spec
            {
                scene = SceneFlow.WindVillage, areaId = SceneFlow.WindVillage, voice = "area_wind",
                sky = "#C8F1E8", fog = "#DDF7F1", accent = "#2A9D8F",
                overviewPos = new Vector3(0, 3.6f, -6.4f), overviewLook = new Vector3(0, 0.5f, 0.6f),
                meloPos = new Vector3(0f, 0.13f, -2.2f), meloRot = 180f,
                stations =
                {
                    St("Saxophone", -0.6f, 1.2f, 10f), new Station { id = "Trumpet", pos = new Vector3(0.6f, 0.13f, 1.2f), rotY = 80f, viewYaw = -90f }, St("Flute", -2.0f, 0.6f, 25f),
                    new Station { id = "Trombone", pos = new Vector3(2.1f, 0.13f, 0.6f), rotY = 65f, viewYaw = -90f }, St("Clarinet", -2.6f, -0.6f, 35f), St("Recorder", 2.6f, -0.6f, -35f),
                },
                decorate = w => Decor(w, "TreeBlossom", "LampNote")
            }));
            return scenes;
        }

        /// <summary>Area dressing from the shared prop library: a ring of trees, lamps and flowers behind the stage.</summary>
        private static void Decor(Transform w, string tree, string lamp)
        {
            var rnd = new System.Random(w.GetInstanceID() & 0xffff);
            for (int i = 0; i < 18; i++)
            {
                float a = -70f + i * 140f / 17f + (float)rnd.NextDouble() * 6f;
                float r = 9f + (float)rnd.NextDouble() * 5f;
                Place("Hub/" + (rnd.NextDouble() < 0.6 ? tree : (tree == "TreeRound" ? "TreeBlossom" : "TreeRound")),
                    Quaternion.Euler(0, a, 0) * Vector3.forward * r, (float)rnd.NextDouble() * 360f, 0.9f + (float)rnd.NextDouble() * 0.5f, w);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = -60f + i * 40f;
                Place("Hub/" + lamp, Quaternion.Euler(0, a, 0) * Vector3.forward * 6.6f, a + 180f, 1f, w);
                Place("Hub/FlowerBed", Quaternion.Euler(0, a + 20f, 0) * Vector3.forward * 7.4f, 0, 1f, w);
            }
            Place("Hub/Bush", new Vector3(-6.5f, 0, -2f), 30f, 1.3f, w);
            Place("Hub/Bush", new Vector3(6.8f, 0, -1.5f), 120f, 1.2f, w);
        }
    }
}
