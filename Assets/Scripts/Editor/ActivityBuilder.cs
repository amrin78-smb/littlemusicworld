using System.Collections.Generic;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.MusicStudio;
using LittleMusicWorld.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>Scenes for Orchestra Park, the Music Studio and Rhythm Carnival, plus animal prefabs and confetti.</summary>
    public static class ActivityBuilder
    {
        public static readonly (string animal, string instrument)[] Band =
        {
            ("Bear", "DrumKit"), ("Rabbit", "GrandPiano"), ("Fox", "Violin"), ("Elephant", "Trumpet"), ("Cat", "AcousticGuitar"), ("Penguin", "Maracas"),
        };

        public static void BuildAnimalPrefabs()
        {
            foreach (var (a, _) in Band)
            {
                string fbx = $"Assets/Art/Characters/{a}.fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null) { Debug.LogWarning("[LMW] animal model missing: " + a); continue; }
                ArtLibrary.Character(fbx, $"{ArtLibrary.PrefabFolder}/Characters/{a}.prefab", ArtLibrary.AtlasMaterial(a, true, 0.5f));
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

        private static Camera World(string sky, string fog, Vector3 camPos, Vector3 look, float fov = 42f)
        {
            Camera cam = SceneBuilder.MakeCamera(E.Hex(sky), camPos, Vector3.zero, fov);
            cam.transform.LookAt(look);
            SceneBuilder.MakeSun(new Vector3(48, -30, 0), 1.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = E.Hex(fog);
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 60f;
            Place("Hub/HubGround", Vector3.zero);
            return cam;
        }

        private static MascotController Melo(Vector3 pos, float rot)
        {
            GameObject m = Place("Characters/Melo", pos, rot);
            if (m == null) return null;
            m.name = "Melo";
            return m.AddComponent<MascotController>();
        }

        private static void Trees(int seed, float minR, float maxR, int count, float arcFrom = -80f, float arcTo = 80f)
        {
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(arcFrom, arcTo, (float)rnd.NextDouble());
                float r = Mathf.Lerp(minR, maxR, (float)rnd.NextDouble());
                Place(rnd.NextDouble() < 0.4 ? "Hub/TreeBlossom" : "Hub/TreeRound", Quaternion.Euler(0, a, 0) * Vector3.forward * r,
                    (float)rnd.NextDouble() * 360f, 0.9f + (float)rnd.NextDouble() * 0.5f);
            }
        }

        public static ParticleSystem Confetti(Vector3 pos)
        {
            var go = new GameObject("Confetti", typeof(ParticleSystem));
            go.transform.position = pos;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.5f;
            main.startLifetime = 2.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 160;
            main.startColor = new ParticleSystem.MinMaxGradient(E.Hex("#FF5A5F"), E.Hex("#5B7CFA"));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", UISprites.Get("Pebble").texture);
            r.sharedMaterial = E.CreateOrReplace(m, "Assets/Art/Materials/M_Confetti.mat");
            return ps;
        }

        private static Button Card(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string colour, Sprite icon)
        {
            Button b = UIKit.Pebble("Card", parent, anchor, pos, size, colour, null);
            Image i = UIKit.Image("Icon", b.transform, null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 8), size * 0.78f);
            i.sprite = icon;
            i.preserveAspect = true;
            return b;
        }

        private static Sprite Portrait(string id) => AssetDatabase.LoadAssetAtPath<Sprite>($"{PortraitRenderer.Folder}/Portrait_{id}.png");

        private static RectTransform Overlay(Canvas c, string name, Vector2 size)
        {
            RectTransform panel = UIKit.Stretch(name, c.transform);
            UIKit.Image("Dim", panel, null, new Color(0.18f, 0.16f, 0.29f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), true);
            UIKit.Image("Card", panel, "Panel", UIKit.Cream, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return panel;
        }

        private static RectTransform Grid(RectTransform parent, Vector2 pos, Vector2 size, Vector2 cell, int columns)
        {
            RectTransform g = UIKit.Rect("Grid", parent, new Vector2(0.5f, 0.5f), pos, size);
            var gl = g.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = cell;
            gl.spacing = new Vector2(24, 24);
            gl.childAlignment = TextAnchor.MiddleCenter;
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = columns;
            return g;
        }

        private static Object[] Icons(params string[] names)
        {
            var l = new List<Object>();
            foreach (string n in names) l.Add(UISprites.Get(n));
            return l.ToArray();
        }

        // ================================================================== Orchestra Park
        public static string BuildOrchestra()
        {
            Scene s = SceneBuilder.NewScene();
            Camera cam = World("#FFE0D6", "#FFEDE6", new Vector3(0, 3.0f, -8.2f), new Vector3(0, 0.9f, 0.6f), 40f);
            Place("Hub/Plaza", new Vector3(0, 0, 0.8f), 0, 0.72f);
            GameObject shell = Place("Hub/GateOrchestra", new Vector3(0, 0, 3.6f), 0, 1.45f) ?? Place("Hub/Bandstand", new Vector3(0, 0, 5.5f), 0, 1f);
            Place("Hub/LampNote", new Vector3(-5.2f, 0.12f, 1.5f), 90f);
            Place("Hub/LampNote", new Vector3(5.2f, 0.12f, 1.5f), -90f);
            Place("Hub/FlowerBed", new Vector3(-4.2f, 0.12f, -1.6f));
            Place("Hub/FlowerBed", new Vector3(4.2f, 0.12f, -1.6f));
            Trees(11, 9f, 15f, 16);
            var spots = new List<Transform>();
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Lerp(-58f, 58f, i / 5f);
                var t = new GameObject("Spot" + i).transform;
                t.position = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 2.6f, 0.13f, 1.4f - Mathf.Cos(a * Mathf.Deg2Rad) * 1.2f + (i == 0 || i == 5 ? 0.4f : 0f));
                spots.Add(t);
            }
            var seats = new List<OrchestraManager.Seat>();
            Canvas c = SceneBuilder.MakeCanvas("Orchestra Canvas", 10);
            RectTransform cardsBar = UIKit.Rect("Cards", c.transform, new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(1300, 220));
            for (int i = 0; i < Band.Length; i++)
            {
                var (animal, instrument) = Band[i];
                GameObject a = Place($"Characters/{animal}", new Vector3(i < 3 ? -7f - i * 0.9f : 7f + (i - 3) * 0.9f, 0.13f, -0.5f + (i % 3) * 0.8f), 180f);
                if (a == null) continue;
                a.name = animal;
                var mc = a.AddComponent<MusicianController>();
                mc.animalId = animal;
                mc.defaultInstrument = instrument;
                var box = a.AddComponent<BoxCollider>();
                box.center = new Vector3(0, 0.55f, 0);
                box.size = new Vector3(0.9f, 1.2f, 0.9f);
                var wing = new GameObject(animal + " Wing").transform;
                wing.position = a.transform.position;
                Button card = Card(cardsBar, new Vector2(0.5f, 0.5f), new Vector2((i - 2.5f) * 215f, 0), new Vector2(190, 190), "#FFF4E0", Portrait(animal));
                Image lockImg = UIKit.Image("Lock", card.transform, "IconLock", new Color(0.18f, 0.16f, 0.29f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(55, -55), new Vector2(70, 70));
                seats.Add(new OrchestraManager.Seat { musician = mc, card = card, cardLock = lockImg, wing = wing });
            }
            MascotController melo = Melo(new Vector3(-4.6f, 0.13f, -2.2f), 150f);
            ParticleSystem confetti = Confetti(new Vector3(0, 0.5f, 0.5f));

            Button home = UIKit.Pebble("Home", c.transform, new Vector2(0, 1), new Vector2(110, -100), new Vector2(160, 150), "#FFC93C", "IconHome");
            home.gameObject.AddComponent<SceneButton>().scene = SceneFlow.Hub;
            Button song = UIKit.Pebble("Song", c.transform, new Vector2(0, 1), new Vector2(300, -100), new Vector2(170, 150), "#FF5A8A", null);
            Image songIcon = UIKit.Image("SongIcon", song.transform, "IconStar", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(110, 110));
            Button play = UIKit.Pebble("Play", c.transform, new Vector2(1, 0), new Vector2(-190, 140), new Vector2(260, 230), "#8BD346", "IconPlay");
            Button stop = UIKit.Pebble("Stop", c.transform, new Vector2(1, 0), new Vector2(-190, 140), new Vector2(260, 230), "#FF5A5F", "IconStop");
            Slider tempo = UIKit.Slider("Tempo", c.transform, new Vector2(1, 1), new Vector2(-480, -100), 420, "#FFC93C");
            UIKit.Image("Slow", c.transform, "IconDown", UIKit.Ink, new Vector2(1, 1), new Vector2(-740, -100), new Vector2(60, 60));
            UIKit.Image("Fast", c.transform, "IconUp", UIKit.Ink, new Vector2(1, 1), new Vector2(-220, -100), new Vector2(60, 60));

            RectTransform songPanel = Overlay(c, "Song Panel", new Vector2(1500, 760));
            RectTransform songGrid = Grid(songPanel, new Vector2(0, -20), new Vector2(1400, 640), new Vector2(200, 200), 6);
            Button closeSongs = UIKit.Pebble("Close", songPanel, new Vector2(0.5f, 0.5f), new Vector2(680, 320), new Vector2(120, 110), "#8BD346", "IconBack");
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeSongs.onClick, songPanel.gameObject.SetActive, false);
            RectTransform instPanel = Overlay(c, "Instrument Panel", new Vector2(1500, 760));
            RectTransform instGrid = Grid(instPanel, new Vector2(0, -20), new Vector2(1400, 640), new Vector2(200, 200), 6);
            Button closeInst = UIKit.Pebble("Close", instPanel, new Vector2(0.5f, 0.5f), new Vector2(680, 320), new Vector2(120, 110), "#8BD346", "IconBack");
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeInst.onClick, instPanel.gameObject.SetActive, false);
            UIKit.Bubble(c.transform, new Vector2(0.5f, 1f), new Vector2(0, -280), 1000);

            var mgr = new GameObject("Orchestra").AddComponent<OrchestraManager>();
            var so = new SerializedObject(mgr);
            SerializedProperty seatsProp = so.FindProperty("seats");
            seatsProp.arraySize = seats.Count;
            for (int i = 0; i < seats.Count; i++)
            {
                SerializedProperty e = seatsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("musician").objectReferenceValue = seats[i].musician;
                e.FindPropertyRelative("card").objectReferenceValue = seats[i].card;
                e.FindPropertyRelative("cardLock").objectReferenceValue = seats[i].cardLock;
                e.FindPropertyRelative("wing").objectReferenceValue = seats[i].wing;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            E.SetArray(mgr, "stageSpots", spots.ToArray());
            E.Set(mgr, "playButton", play);
            E.Set(mgr, "stopButton", stop);
            E.Set(mgr, "songButton", song);
            E.Set(mgr, "tempo", tempo);
            E.Set(mgr, "songGrid", songGrid);
            E.Set(mgr, "songPanel", songPanel.gameObject);
            E.Set(mgr, "instrumentGrid", instGrid);
            E.Set(mgr, "instrumentPanel", instPanel.gameObject);
            E.SetArray(mgr, "instrumentIcons", PortraitRenderer.All());
            E.SetArray(mgr, "songIcons", Icons("IconStar", "IconLamb", "IconBell", "IconBun", "IconSun", "IconBoat", "IconHeart"));
            var prefabs = new List<Object>();
            foreach (string id in AreaBuilder.AtlasOf.Keys)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtLibrary.PrefabFolder}/Instruments/{id}.prefab");
                if (p != null) prefabs.Add(p);
            }
            E.SetArray(mgr, "instrumentPrefabs", prefabs.ToArray());
            if (melo != null) E.Set(mgr, "melo", melo);
            E.Set(mgr, "confetti", confetti);
            E.Set(mgr, "songCardIcon", songIcon);
            WorldBuilder.ParentArea(c);
            return SceneBuilder.Save(s, SceneFlow.OrchestraPark);
        }

        // ================================================================== Music Studio
        public static string BuildStudio()
        {
            Scene s = SceneBuilder.NewScene();
            World("#E3D9FF", "#EEE8FF", new Vector3(0, 2.6f, -7.5f), new Vector3(0, 1.6f, 2f), 40f);
            Place("Hub/GateStudio", new Vector3(0, 0, 5f), 0, 1.3f);
            Place("Hub/Plaza", new Vector3(0, 0, 1f), 0, 0.8f);
            Trees(23, 9f, 14f, 12);
            MascotController melo = Melo(new Vector3(-3.4f, 0.13f, -0.8f), 160f);
            Canvas c = SceneBuilder.MakeCanvas("Studio Canvas", 10);
            Button home = UIKit.Pebble("Home", c.transform, new Vector2(0, 1), new Vector2(100, -80), new Vector2(140, 130), "#FFC93C", "IconHome");
            home.gameObject.AddComponent<SceneButton>().scene = SceneFlow.Hub;
            Button songs = UIKit.Pebble("Songs", c.transform, new Vector2(0, 1), new Vector2(260, -80), new Vector2(150, 130), "#D46CE0", "IconStar");
            Button newSong = UIKit.Pebble("New", c.transform, new Vector2(0, 1), new Vector2(430, -80), new Vector2(150, 130), "#2EC4B6", "Note");
            Button save = UIKit.Pebble("Save", c.transform, new Vector2(0, 1), new Vector2(600, -80), new Vector2(150, 130), "#8BD346", "IconCheck");
            Slider tempo = UIKit.Slider("Tempo", c.transform, new Vector2(1, 1), new Vector2(-360, -80), 380, "#FFC93C");
            UIKit.Image("Slow", c.transform, "IconDown", UIKit.Ink, new Vector2(1, 1), new Vector2(-590, -80), new Vector2(54, 54));
            UIKit.Image("Fast", c.transform, "IconUp", UIKit.Ink, new Vector2(1, 1), new Vector2(-130, -80), new Vector2(54, 54));

            // timeline: 4 lanes
            RectTransform board = UIKit.Rect("Timeline", c.transform, new Vector2(0.5f, 1f), new Vector2(-110, -360), new Vector2(1500, 400));
            var lanes = new List<StudioManager.Lane>();
            string[] laneCols = { "#FF5A5F", "#FFA62B", "#2EC4B6", "#5B7CFA" };
            for (int i = 0; i < 4; i++)
            {
                float y = 150 - i * 100;
                RectTransform lr = UIKit.Rect("Lane" + i, board, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(1500, 90));
                Image hl = UIKit.Image("Highlight", lr, "Panel", new Color(1f, 0.82f, 0.25f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1512, 100));
                Button select = UIKit.Pebble("Select", lr, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1500, 90), laneCols[i], null);
                Object.DestroyImmediate(select.GetComponent<PressFeedback>());
                select.GetComponent<Image>().color = Color.Lerp(E.Hex(laneCols[i]), Color.white, 0.6f);
                Button inst = UIKit.Pebble("Instrument", lr, new Vector2(0f, 0.5f), new Vector2(60, 0), new Vector2(110, 86), laneCols[i], null);
                Image instIcon = UIKit.Image("Icon", inst.transform, null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(80, 80));
                instIcon.preserveAspect = true;
                Button mute = UIKit.Pebble("Mute", lr, new Vector2(1f, 0.5f), new Vector2(-250, 0), new Vector2(90, 80), "#5B7CFA", "IconListen", 0.6f);
                Button clear = UIKit.Pebble("Clear", lr, new Vector2(1f, 0.5f), new Vector2(-50, 0), new Vector2(90, 80), "#FF5A5F", "IconTrash", 0.6f);
                Slider vol = UIKit.Slider("Volume", lr, new Vector2(1f, 0.5f), new Vector2(-150, 0), 100, laneCols[i]);
                RectTransform notes = UIKit.Rect("Notes", lr, new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(1060, 80));
                lanes.Add(new StudioManager.Lane
                {
                    root = lr, select = select, instrument = inst, mute = mute, clear = clear, instrumentIcon = instIcon,
                    muteIcon = mute.transform.Find("Icon").GetComponent<Image>(), highlight = hl, volume = vol, notes = notes
                });
            }
            Image playhead = UIKit.Image("Playhead", board, "Panel", UIKit.Ink, new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(6, 410));
            playhead.rectTransform.SetParent(lanes[0].notes.parent.parent, true);
            RectTransform phParent = UIKit.Rect("PlayheadTrack", board, new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(1060, 400));
            playhead.rectTransform.SetParent(phParent, false);
            playhead.rectTransform.anchorMin = playhead.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            playhead.rectTransform.anchoredPosition = Vector2.zero;

            Button play = UIKit.Pebble("Play", c.transform, new Vector2(1, 1), new Vector2(-110, -260), new Vector2(170, 150), "#8BD346", "IconPlay");
            Button stop = UIKit.Pebble("Stop", c.transform, new Vector2(1, 1), new Vector2(-110, -260), new Vector2(170, 150), "#3D405B", "IconStop");
            Button rec = UIKit.Pebble("Record", c.transform, new Vector2(1, 1), new Vector2(-110, -430), new Vector2(170, 150), "#FF5A5F", "IconRecord");
            Image recGlow = UIKit.Image("RecGlow", rec.transform, "GlowRect", new Color(1, 0.3f, 0.3f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 210));
            recGlow.transform.SetAsFirstSibling();
            recGlow.enabled = false;
            RectTransform count = UIKit.Rect("CountIn", c.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(500, 120));
            for (int i = 0; i < 4; i++) UIKit.Image("Dot" + i, count, "Circle", E.Hex("#FF5A5F"), new Vector2(0.5f, 0.5f), new Vector2((i - 1.5f) * 120, 0), new Vector2(90, 90));

            RectTransform panelArea = UIKit.Rect("Instrument Panel", c.transform, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            panelArea.anchorMin = new Vector2(0f, 0f);
            panelArea.anchorMax = new Vector2(1f, 0f);
            panelArea.pivot = new Vector2(0.5f, 0f);
            panelArea.offsetMin = new Vector2(60, 20);
            panelArea.offsetMax = new Vector2(-60, 20 + 430);
            var panel = panelArea.gameObject.AddComponent<InstrumentPanel>();
            E.Set(panel, "area", panelArea);

            RectTransform picker = Overlay(c, "Instrument Picker", new Vector2(1500, 760));
            RectTransform pickerGrid = Grid(picker, new Vector2(0, -20), new Vector2(1400, 640), new Vector2(190, 190), 6);
            Button closePicker = UIKit.Pebble("Close", picker, new Vector2(0.5f, 0.5f), new Vector2(680, 320), new Vector2(120, 110), "#8BD346", "IconBack");
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closePicker.onClick, picker.gameObject.SetActive, false);
            RectTransform songsPanel = Overlay(c, "My Songs", new Vector2(1500, 760));
            RectTransform songsGrid = Grid(songsPanel, new Vector2(0, -20), new Vector2(1400, 640), new Vector2(200, 200), 6);
            Button closeSongs = UIKit.Pebble("Close", songsPanel, new Vector2(0.5f, 0.5f), new Vector2(680, 320), new Vector2(120, 110), "#8BD346", "IconBack");
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeSongs.onClick, songsPanel.gameObject.SetActive, false);
            UIKit.Bubble(c.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), 900);

            var mgr = new GameObject("Studio").AddComponent<StudioManager>();
            var so = new SerializedObject(mgr);
            SerializedProperty lp = so.FindProperty("lanes");
            lp.arraySize = lanes.Count;
            for (int i = 0; i < lanes.Count; i++)
            {
                SerializedProperty e = lp.GetArrayElementAtIndex(i);
                StudioManager.Lane l = lanes[i];
                e.FindPropertyRelative("root").objectReferenceValue = l.root;
                e.FindPropertyRelative("select").objectReferenceValue = l.select;
                e.FindPropertyRelative("instrument").objectReferenceValue = l.instrument;
                e.FindPropertyRelative("mute").objectReferenceValue = l.mute;
                e.FindPropertyRelative("clear").objectReferenceValue = l.clear;
                e.FindPropertyRelative("instrumentIcon").objectReferenceValue = l.instrumentIcon;
                e.FindPropertyRelative("muteIcon").objectReferenceValue = l.muteIcon;
                e.FindPropertyRelative("highlight").objectReferenceValue = l.highlight;
                e.FindPropertyRelative("volume").objectReferenceValue = l.volume;
                e.FindPropertyRelative("notes").objectReferenceValue = l.notes;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            E.Set(mgr, "panel", panel);
            E.Set(mgr, "playButton", play);
            E.Set(mgr, "stopButton", stop);
            E.Set(mgr, "recordButton", rec);
            E.Set(mgr, "saveButton", save);
            E.Set(mgr, "songsButton", songs);
            E.Set(mgr, "newButton", newSong);
            E.Set(mgr, "recordGlow", recGlow);
            E.Set(mgr, "tempoSlider", tempo);
            E.Set(mgr, "playhead", playhead.rectTransform);
            E.Set(mgr, "countIn", count);
            E.Set(mgr, "pickerPanel", picker.gameObject);
            E.Set(mgr, "pickerGrid", pickerGrid);
            E.Set(mgr, "songsPanel", songsPanel.gameObject);
            E.Set(mgr, "songsGrid", songsGrid);
            E.SetArray(mgr, "portraits", PortraitRenderer.All());
            E.Set(mgr, "blockSprite", UISprites.Get("Pebble"));
            if (melo != null) E.Set(mgr, "melo", melo);
            WorldBuilder.ParentArea(c);
            return SceneBuilder.Save(s, SceneFlow.MusicStudio);
        }

        // ================================================================== Rhythm Carnival
        public static string BuildCarnival()
        {
            Scene s = SceneBuilder.NewScene();
            World("#FFE3EE", "#FFF0F5", new Vector3(0, 2.8f, -8f), new Vector3(0, 1.8f, 2f), 40f);
            Place("Hub/GateCarnival", new Vector3(0, 0, 5f), 0, 1.4f);
            Place("Hub/Plaza", new Vector3(0, 0, 1f), 0, 0.9f);
            Place("Hub/LampNote", new Vector3(-4.5f, 0.1f, 2f), 90f);
            Place("Hub/LampNote", new Vector3(4.5f, 0.1f, 2f), -90f);
            Trees(37, 9f, 14f, 14);
            MascotController melo = Melo(new Vector3(-4.2f, 0.13f, -1.6f), 150f);
            ParticleSystem confetti = Confetti(new Vector3(0, 0.6f, 0f));
            Canvas c = SceneBuilder.MakeCanvas("Carnival Canvas", 10);
            RectTransform booths = UIKit.Stretch("Booths", c.transform);
            Button home = UIKit.Pebble("Home", booths, new Vector2(0, 1), new Vector2(110, -100), new Vector2(160, 150), "#FFC93C", "IconHome");
            home.gameObject.AddComponent<SceneButton>().scene = SceneFlow.Hub;
            string[] icons = { "Note", "IconListen", "IconHeart", "IconBell", "IconUp", "IconStar", "IconSun" };
            string[] cols = { "#FF5A5F", "#5B7CFA", "#FF7BAC", "#FFC93C", "#2EC4B6", "#D46CE0", "#8BD346" };
            var buttons = new Button[7];
            var stars = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                int row = i < 4 ? 0 : 1;
                float x = row == 0 ? (i - 1.5f) * 300f : (i - 4 - 1f) * 300f;
                buttons[i] = UIKit.Pebble("Booth" + i, booths, new Vector2(0.5f, 0.5f), new Vector2(x, row == 0 ? 120 : -200), new Vector2(260, 260), cols[i], icons[i], 0.55f);
                stars[i] = UIKit.Image("Star", buttons[i].transform, "IconStar", E.Hex("#FFD23F"), new Vector2(0.5f, 0.5f), new Vector2(95, 95), new Vector2(80, 80));
            }
            RectTransform stage = UIKit.Rect("Stage", c.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1700, 820));
            RectTransform gameBar = UIKit.Stretch("Game Bar", c.transform);
            Button back = UIKit.Pebble("Back", gameBar, new Vector2(0, 1), new Vector2(110, -100), new Vector2(160, 150), "#FF7BAC", "IconBack");
            Button again = UIKit.Pebble("Again", gameBar, new Vector2(1, 0), new Vector2(-180, 160), new Vector2(240, 210), "#8BD346", "IconPlay");
            RectTransform dots = UIKit.Rect("Score", gameBar, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(800, 60));
            for (int i = 0; i < 8; i++) UIKit.Image("Dot" + i, dots, "IconStar", Color.white, new Vector2(0.5f, 0.5f), new Vector2((i - 3.5f) * 80, 0), new Vector2(60, 60)).gameObject.SetActive(false);
            UIKit.Bubble(c.transform, new Vector2(0.5f, 1f), new Vector2(0, -230), 1000);
            var mgr = new GameObject("Carnival").AddComponent<CarnivalManager>();
            E.Set(mgr, "stage", stage);
            E.Set(mgr, "boothPanel", booths.gameObject);
            E.SetArray(mgr, "boothButtons", buttons);
            E.SetArray(mgr, "boothStars", stars);
            E.Set(mgr, "gameBar", gameBar.gameObject);
            E.Set(mgr, "backButton", back);
            E.Set(mgr, "againButton", again);
            E.Set(mgr, "scoreDots", dots);
            E.SetArray(mgr, "portraits", PortraitRenderer.All());
            if (melo != null) E.Set(mgr, "melo", melo);
            E.Set(mgr, "confetti", confetti);
            WorldBuilder.ParentArea(c);
            return SceneBuilder.Save(s, SceneFlow.RhythmCarnival);
        }

        // ================================================================== Sticker book (added to the hub canvas)
        public static void AddStickerBook(Canvas c)
        {
            Button open = UIKit.Pebble("Stickers", c.transform, new Vector2(0, 1), new Vector2(290, -100), new Vector2(160, 150), "#D46CE0", "IconStar");
            RectTransform book = Overlay(c, "Sticker Book", new Vector2(1720, 960));
            RectTransform inst = Grid(book, new Vector2(0, 150), new Vector2(1600, 560), new Vector2(120, 120), 13);
            RectTransform mus = Grid(book, new Vector2(-330, -360), new Vector2(860, 130), new Vector2(120, 120), 6);
            RectTransform areas = Grid(book, new Vector2(470, -360), new Vector2(760, 130), new Vector2(96, 96), 7);
            UIKit.Image("StarIcon", book, "IconStar", E.Hex("#FFD23F"), new Vector2(0.5f, 0.5f), new Vector2(-720, 400), new Vector2(110, 110));
            TextMeshProUGUI count = UIKit.Text("Stars", book, "0", 64, UIKit.Ink, new Vector2(0.5f, 0.5f), new Vector2(-600, 400), new Vector2(160, 90));
            Button close = UIKit.Pebble("Close", book, new Vector2(0.5f, 0.5f), new Vector2(790, 410), new Vector2(120, 110), "#8BD346", "IconCheck");
            var sb = book.gameObject.AddComponent<StickerBook>();
            E.Set(sb, "instrumentGrid", inst);
            E.Set(sb, "musicianGrid", mus);
            E.Set(sb, "areaGrid", areas);
            E.Set(sb, "starCount", count);
            E.SetArray(sb, "portraits", PortraitRenderer.All());
            E.SetArray(sb, "areaIcons", Icons("KeyWhite", "Circle", "Note", "IconListen", "IconStar", "IconRecord", "IconSun"));
            E.Set(sb, "close", close);
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(open.onClick, book.gameObject.SetActive, true);
            book.gameObject.SetActive(false);
        }
    }
}
