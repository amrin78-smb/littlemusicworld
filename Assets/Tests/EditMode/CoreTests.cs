using System.Collections.Generic;
using System.IO;
using LittleMusicWorld.Audio;
using LittleMusicWorld.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace LittleMusicWorld.Tests
{
    public class NoteTests
    {
        [Test] public void ParsesNoteNames()
        {
            Assert.AreEqual(60, NoteUtil.Parse("C4"));
            Assert.AreEqual(61, NoteUtil.Parse("C#4"));
            Assert.AreEqual(70, NoteUtil.Parse("Bb4"));
            Assert.AreEqual(21, NoteUtil.Parse("A0"));
            Assert.AreEqual(-1, NoteUtil.Parse("R"));
        }

        [Test] public void ParsesMelodiesWithRestsAndChords()
        {
            List<NoteEvent> m = NoteUtil.ParseMelody("C4 D4:2 R:1 C4+E4+G4:4");
            Assert.AreEqual(5, m.Count);
            Assert.AreEqual(0f, m[0].beat);
            Assert.AreEqual(1f, m[1].beat);
            Assert.AreEqual(2f, m[1].length);
            Assert.AreEqual(4f, m[2].beat, "rest advances time");
            Assert.AreEqual(4f, m[4].beat, "chord notes share a start");
            Assert.AreEqual(8f, NoteUtil.LengthInBeats(m));
        }

        [Test] public void BlackKeys()
        {
            Assert.IsTrue(NoteUtil.IsBlackKey(61));
            Assert.IsFalse(NoteUtil.IsBlackKey(64));
            Assert.IsFalse(NoteUtil.IsBlackKey(65));
        }
    }

    public class SongTests
    {
        [Test] public void EverySongParsesAndStaysInCMajor()
        {
            int[] cMajor = { 0, 2, 4, 5, 7, 9, 11 };
            foreach (SongLibrary.Song s in SongLibrary.Songs)
            {
                Assert.Greater(s.NoteCount, 2, s.id);
                foreach (NoteEvent e in s.Melody)
                    Assert.Contains(e.note % 12, cMajor, $"{s.id} has {NoteUtil.Name(e.note)}");
                Assert.Greater(s.tempo, 40f);
            }
        }

        [Test] public void LessonsMatchTheGddTiers()
        {
            List<SongLibrary.Lesson> lessons = SongLibrary.Lessons();
            int[] min = { 3, 5, 7, 11 }, max = { 4, 6, 10, 999 };
            var perTier = new int[4];
            foreach (SongLibrary.Lesson l in lessons)
            {
                perTier[l.tier]++;
                Assert.GreaterOrEqual(l.notes.Count, min[l.tier], l.Key);
                Assert.LessOrEqual(l.notes.Count, max[l.tier], l.Key);
            }
            for (int t = 0; t < 4; t++) Assert.GreaterOrEqual(perTier[t], 3, $"tier {t} needs songs");
        }

        [Test] public void AccompanimentStaysUnderTheMelody()
        {
            foreach (SongLibrary.Song s in SongLibrary.Songs)
                foreach (NoteEvent e in SongLibrary.Accompaniment(s))
                    Assert.Less(e.note, 60, s.id);
        }
    }

    public class SaveTests
    {
        [Test] public void RoundTrip()
        {
            var d = new SaveData();
            d.settings.language = "th";
            d.progress.completedSongs.Add("twinkle:0");
            d.progress.stars = 3;
            SaveData r = SaveSerializer.Deserialize(SaveSerializer.Serialize(d), out LoadOutcome o);
            Assert.AreEqual(LoadOutcome.Loaded, o);
            Assert.AreEqual("th", r.settings.language);
            Assert.AreEqual(3, r.progress.stars);
            Assert.Contains("twinkle:0", r.progress.completedSongs);
        }

        [Test] public void CorruptAndMissingAreSafe()
        {
            SaveSerializer.Deserialize("{{{ not json", out LoadOutcome o1);
            Assert.AreEqual(LoadOutcome.Corrupt, o1);
            SaveData d = SaveSerializer.Deserialize("", out LoadOutcome o2);
            Assert.AreEqual(LoadOutcome.Missing, o2);
            Assert.IsNotNull(d.settings);
        }

        [Test] public void SanitisesBadValues()
        {
            SaveData d = SaveSerializer.Deserialize("{\"version\":1,\"settings\":{\"masterVolume\":7,\"language\":\"xx\"},\"progress\":{\"stars\":-4,\"completedSongs\":[\"a\",\"a\",\"\"]}}", out _);
            Assert.AreEqual(1f, d.settings.masterVolume);
            Assert.AreEqual("en", d.settings.language);
            Assert.AreEqual(0, d.progress.stars);
            Assert.AreEqual(1, d.progress.completedSongs.Count);
        }

        [Test] public void NewerVersionLoadsBestEffort()
        {
            SaveSerializer.Deserialize("{\"version\":99,\"progress\":{\"stars\":2}}", out LoadOutcome o);
            Assert.AreEqual(LoadOutcome.NewerVersion, o);
        }

        [Test] public void InterruptedWriteRecoversFromBackup()
        {
            string dir = Path.Combine(Path.GetTempPath(), "lmw_save_test_" + System.Guid.NewGuid().ToString("N"));
            var store = new SaveStore(dir);
            store.Data.progress.stars = 5;
            store.Save();
            store.Data.progress.stars = 6;
            store.Save(); // previous good file is now save.json.bak
            File.WriteAllText(Path.Combine(dir, "save.json"), "{\"version\":1, truncated");
            var again = new SaveStore(dir);
            Assert.AreEqual(5, again.Data.progress.stars, "falls back to the last good backup");
            Directory.Delete(dir, true);
        }

        [Test] public void CompositionsSaveLoadAndSanitise()
        {
            string dir = Path.Combine(Path.GetTempPath(), "lmw_comp_test_" + System.Guid.NewGuid().ToString("N"));
            var store = new SaveStore(dir);
            var c = new CompositionData { tempo = 120f, lengthBeats = 16f };
            c.tracks.Add(new TrackData { instrumentId = "GrandPiano", notes = new List<NoteEvent> { new NoteEvent(0, 1, 60), new NoteEvent(-1, 1, 200) } });
            store.SaveComposition(c);
            CompositionData r = store.LoadComposition(c.id);
            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.tracks[0].notes.Count, "invalid notes removed");
            Assert.AreEqual(120f, r.tempo);
            Directory.Delete(dir, true);
        }
    }
}
