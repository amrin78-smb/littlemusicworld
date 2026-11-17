using System.Collections.Generic;
using LittleMusicWorld.Audio;
using NUnit.Framework;
using UnityEngine;

namespace LittleMusicWorld.Tests
{
    public class ArrangerTests
    {
        private InstrumentLibrary lib;

        [SetUp] public void SetUp()
        {
            lib = Resources.Load<InstrumentLibrary>("InstrumentLibrary");
            Assert.IsNotNull(lib, "run Tools/setup.sh first");
        }

        [Test] public void EveryInstrumentGetsAPlayablePartForEverySong()
        {
            foreach (SongLibrary.Song song in SongLibrary.Songs)
            foreach (InstrumentDefinition d in lib.instruments)
            {
                Arranger.Role role = Arranger.RoleFor(d, false);
                List<NoteEvent> part = Arranger.Part(song, d, role);
                Assert.IsNotEmpty(part, $"{d.id} / {song.id}");
                float len = Arranger.SongLength(song);
                foreach (NoteEvent e in part)
                {
                    Assert.GreaterOrEqual(e.beat, 0f);
                    Assert.Less(e.beat, len + 0.1f, $"{d.id} / {song.id} note after the end");
                    Assert.That(e.velocity, Is.InRange(1, 127));
                    if (d.playStyle != PlayStyle.DrumPads)
                        Assert.That(e.note, Is.InRange(d.lowestNote - 12, d.highestNote + 12), $"{d.id} / {song.id} out of range");
                }
            }
        }

        [Test] public void MelodyKeepsItsShapeOnEveryInstrument()
        {
            SongLibrary.Song s = SongLibrary.Get("twinkle");
            foreach (InstrumentDefinition d in lib.instruments)
            {
                if (d.playStyle == PlayStyle.DrumPads) continue;
                List<NoteEvent> part = Arranger.Part(s, d, Arranger.Role.Melody);
                Assert.AreEqual(s.Melody.Count, part.Count, d.id);
                int shift = part[0].note - s.Melody[0].note;
                Assert.AreEqual(0, shift % 12, d.id + " moves by whole octaves");
                for (int i = 0; i < part.Count; i++)
                {
                    int expect = s.Melody[i].note + shift;
                    if (expect >= d.playableNotes[0] && expect <= d.playableNotes[d.playableNotes.Length - 1])
                        Assert.AreEqual(expect, part[i].note, $"{d.id} note {i}");
                }
            }
        }

        [Test] public void DrumKitPlaysABackbeat()
        {
            List<NoteEvent> beat = Arranger.Part(SongLibrary.Get("mary"), lib.Get("DrumKit"), Arranger.Role.Beat);
            Assert.IsTrue(beat.Exists(e => e.note == 36 && e.beat == 0f), "kick on 1");
            Assert.IsTrue(beat.Exists(e => e.note == 38 && e.beat == 1f), "snare on 2");
            Assert.IsTrue(beat.Exists(e => e.note == 42 && e.beat == 0.5f), "hi-hat eighths");
        }

        [Test] public void HarmonyIsADiatonicThirdBelow()
        {
            SongLibrary.Song s = SongLibrary.Get("odetojoy");
            List<NoteEvent> h = Arranger.Part(s, lib.Get("Flute"), Arranger.Role.Harmony);
            int[] cMajor = { 0, 2, 4, 5, 7, 9, 11 };
            foreach (NoteEvent e in h) Assert.Contains(e.note % 12, cMajor, "harmony stays in C major");
        }

        [Test] public void ChordsFollowTheSong()
        {
            SongLibrary.Song s = SongLibrary.Get("twinkle");
            var chords = Arranger.ChordTimeline(s, Arranger.SongLength(s));
            Assert.AreEqual(new[] { 0, 4, 7 }, chords[0].pcs, "Twinkle starts on C");
            List<NoteEvent> strum = Arranger.Part(s, lib.Get("AcousticGuitar"), Arranger.Role.Chords);
            foreach (NoteEvent e in strum)
                if (e.beat < 2f) Assert.Contains(e.note % 12, new[] { 0, 4, 7 }, "strummed notes belong to the chord");
        }
    }
}
