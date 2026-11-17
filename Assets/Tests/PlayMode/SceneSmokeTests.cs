using System.Collections;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LittleMusicWorld.Tests
{
    /// <summary>Every build scene loads without errors and has its essential parts; the piano reaches the engine.</summary>
    public class SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator AllScenesLoadCleanly([Values("Boot", "MainMenu", "Hub", "PianoParadise", "DrumValley", "StringTown", "WindVillage")] string scene)
        {
            LogAssert.ignoreFailingMessages = false;
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsNotNull(Services.Engine, "services boot before the first scene");
            Assert.IsNotNull(Camera.main, scene + " has a main camera");
            Assert.IsNotNull(Object.FindFirstObjectByType<Canvas>(), scene + " has UI");
        }

        [UnityTest]
        public IEnumerator PianoKeysReachTheEngine()
        {
            yield return SceneManager.LoadSceneAsync("PianoParadise", LoadSceneMode.Single);
            for (int i = 0; i < 10; i++) yield return null;
            var area = Object.FindFirstObjectByType<AreaController>();
            area.Enter(area.Stations[0]);
            for (int i = 0; i < 10; i++) yield return null;
            var kb = Object.FindFirstObjectByType<KeyboardSurface>();
            Assert.IsNotNull(kb);
            Assert.IsNotNull(kb.Instrument, "an instrument is selected");
            int started = 0;
            Services.Engine.NoteStarted += (d, n, v, c) => started++;
            int idx = kb.HitTest(RectTransformUtility.WorldToScreenPoint(null, kb.KeyWorldPosition(kb.LowNote)));
            Assert.GreaterOrEqual(idx, 0, "lowest key is hit-testable at its own position");
            kb.Press(idx, 77, RectTransformUtility.WorldToScreenPoint(null, kb.KeyWorldPosition(kb.LowNote)));
            kb.Release(idx, 77);
            Assert.AreEqual(1, started);
            Assert.IsTrue(Services.Engine.IsLoaded(kb.Instrument));
        }

        [UnityTest]
        public IEnumerator FollowTheMelodyCompletesWithCorrectKeys()
        {
            yield return SceneManager.LoadSceneAsync("PianoParadise", LoadSceneMode.Single);
            for (int i = 0; i < 10; i++) yield return null;
            var area = Object.FindFirstObjectByType<AreaController>();
            area.Enter(area.Stations[0]);
            for (int i = 0; i < 10; i++) yield return null;
            var kb = Object.FindFirstObjectByType<KeyboardSurface>();
            var guide = Object.FindFirstObjectByType<MelodyGuide>();
            SongLibrary.Lesson lesson = SongLibrary.Lessons().Find(l => l.tier == 0);
            bool done = false;
            guide.Completed += l => done = true;
            guide.Begin(kb, lesson);
            float t = 0;
            while (guide.State != MelodyGuide.Phase.Following && t < 20f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(MelodyGuide.Phase.Following, guide.State, "demonstration ends and the child's turn begins");
            var notes = MelodyGuide.FitToKeyboard(lesson.notes, kb);
            int wrong = notes[0].note == kb.LowNote ? kb.LowNote + 2 : kb.LowNote;
            Play(kb, wrong); // a wrong key is harmless
            Assert.AreEqual(0, guide.Index);
            foreach (NoteEvent n in notes) Play(kb, n.note);
            Assert.IsTrue(done, "playing every shining key completes the lesson");
        }

        [UnityTest]
        public IEnumerator EveryAreaInstrumentBuildsAPlayablePanel([Values("PianoParadise", "DrumValley", "StringTown", "WindVillage")] string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            for (int i = 0; i < 10; i++) yield return null;
            var area = Object.FindFirstObjectByType<AreaController>();
            Assert.Greater(area.Stations.Length, 2, scene + " has instruments");
            foreach (InstrumentStation st in area.Stations)
            {
                Assert.IsNotNull(st.Definition, st.instrumentId + " is a known instrument");
                area.Enter(st);
                yield return null;
                yield return null;
                var panel = Object.FindFirstObjectByType<InstrumentPanel>(FindObjectsInactive.Include);
                Assert.IsTrue(panel.Keyboard != null || (panel.Pads != null && panel.Pads.pads.Count > 0), st.instrumentId + " has a play surface");
                int started = 0;
                System.Action<InstrumentDefinition, int, int, int> h = (d, n, v, c) => { if (d == st.Definition) started++; };
                Services.Engine.NoteStarted += h;
                if (panel.Keyboard != null)
                {
                    Vector2 p = RectTransformUtility.WorldToScreenPoint(null, panel.Keyboard.KeyWorldPosition(panel.Keyboard.LowNote));
                    int k = panel.Keyboard.HitTest(p);
                    panel.Keyboard.Press(k, 9, p);
                    panel.Keyboard.Release(k, 9);
                }
                else
                {
                    panel.Pads.Press(0, 9, RectTransformUtility.WorldToScreenPoint(null, panel.Pads.pads[0].rect.position));
                    panel.Pads.Release(0, 9);
                }
                Services.Engine.NoteStarted -= h;
                Assert.AreEqual(1, started, st.instrumentId + " sounds when touched");
                area.Leave();
                yield return null;
            }
        }

        private static void Play(KeyboardSurface kb, int note)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(null, kb.KeyWorldPosition(note));
            int idx = kb.HitTest(p);
            kb.Press(idx, 5, p);
            kb.Release(idx, 5);
        }
    }
}
