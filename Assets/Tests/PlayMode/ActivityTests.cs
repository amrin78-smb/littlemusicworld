using System.Collections;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.MusicStudio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LittleMusicWorld.Tests
{
    public class ActivityTests
    {
        [UnityTest]
        public IEnumerator OrchestraPlaysAndMusiciansMoveOnTheirNotes()
        {
            yield return SceneManager.LoadSceneAsync("OrchestraPark", LoadSceneMode.Single);
            for (int i = 0; i < 20; i++) yield return null;
            var mgr = Object.FindFirstObjectByType<OrchestraManager>();
            var musicians = Object.FindObjectsByType<MusicianController>(FindObjectsSortMode.None);
            Assert.AreEqual(6, musicians.Length, "six animal musicians");
            foreach (MusicianController m in musicians) Assert.IsNotNull(m.Instrument, m.animalId + " has an instrument");
            var play = GameObject.Find("Play").GetComponent<Button>();
            play.onClick.Invoke();
            Assert.IsTrue(mgr.Playing, "performance starts");
            int hits = 0;
            float t = 0f;
            var animators = new System.Collections.Generic.List<Animator>();
            foreach (MusicianController m in musicians) if (m.Performing) animators.Add(m.GetComponentInChildren<Animator>());
            Assert.GreaterOrEqual(animators.Count, 2, "the two starting musicians perform");
            while (t < 4f)
            {
                t += Time.deltaTime;
                foreach (Animator a in animators)
                    if (a.GetCurrentAnimatorStateInfo(0).IsName("PlayKeys") || a.GetCurrentAnimatorStateInfo(0).IsName("PlayDrums")) hits++;
                yield return null;
            }
            Assert.Greater(hits, 0, "musicians play their instrument animation in time with notes");
            GameObject.Find("Stop").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(mgr.Playing);
        }

        [UnityTest]
        public IEnumerator StudioRecordsSavesAndLoads()
        {
            yield return SceneManager.LoadSceneAsync("MusicStudio", LoadSceneMode.Single);
            for (int i = 0; i < 20; i++) yield return null;
            var studio = Object.FindFirstObjectByType<StudioManager>();
            Assert.AreEqual(2, studio.Song.tracks.Count, "starts with piano + drums");
            GameObject.Find("Record").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(studio.Recording);
            yield return new WaitForSeconds(3.2f); // count-in (4 beats @ 100 bpm) then the loop starts
            InstrumentDefinition piano = Services.Instruments.Get(studio.Song.tracks[0].instrumentId);
            Services.Engine.NoteOn(piano, 60, 100);
            yield return new WaitForSeconds(0.3f);
            Services.Engine.NoteOff(piano, 60);
            GameObject.Find("Record").GetComponent<Button>().onClick.Invoke(); // stop recording
            Assert.IsFalse(studio.Recording);
            Assert.AreEqual(1, studio.Song.tracks[0].notes.Count, "the played note was recorded");
            GameObject.Find("Save").GetComponent<Button>().onClick.Invoke();
            string id = studio.Song.id;
            Assert.IsNotNull(Services.Save.LoadComposition(id), "saved to disk");
            Assert.AreEqual(1, Services.Save.LoadComposition(id).tracks[0].notes.Count);
            Services.Save.DeleteComposition(id);
        }

        [UnityTest]
        public IEnumerator EveryCarnivalGameStartsAndStopsCleanly()
        {
            yield return SceneManager.LoadSceneAsync("RhythmCarnival", LoadSceneMode.Single);
            for (int i = 0; i < 20; i++) yield return null;
            for (int g = 0; g < 7; g++)
            {
                GameObject.Find("Booth" + g).GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSeconds(4f);
                Assert.IsNotNull(Object.FindFirstObjectByType<MiniGame>(), "game " + g + " is running");
                GameObject.Find("Back").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.IsNull(Object.FindFirstObjectByType<MiniGame>(), "game " + g + " cleaned up");
            }
        }
    }
}
