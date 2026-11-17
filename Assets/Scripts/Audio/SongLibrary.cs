using System;
using System.Collections.Generic;
using System.Globalization;

namespace LittleMusicWorld.Audio
{
    /// <summary>
    /// Built-in songs. Melodies are traditional/public-domain tunes transcribed note by note for this game (no
    /// recordings or published arrangements are copied); accompaniments are our own simple chord patterns.
    /// Provenance is listed in Documentation/AUDIO_LICENSES.md. All in C major so the colour-coded keys line up.
    /// </summary>
    public static class SongLibrary
    {
        public sealed class Song
        {
            public string id;
            public string title;      // shown to grown-ups only; children see the picture
            public string picture;    // icon id (star, sheep, bus…)
            public float tempo;
            public string melody;     // NoteUtil.ParseMelody format
            public string chords;     // "C:4 G:4 …" (beats per chord), optional
            public string origin;     // provenance note

            private List<NoteEvent> parsed;
            public List<NoteEvent> Melody => parsed ??= NoteUtil.ParseMelody(melody);
            public int NoteCount => Melody.Count;
        }

        public static readonly List<Song> Songs = new List<Song>
        {
            new Song { id = "hotcrossbuns", title = "Hot Cross Buns", picture = "bun", tempo = 96,
                melody = "E4 D4 C4:2 | E4 D4 C4:2 | C4:0.5 C4:0.5 C4:0.5 C4:0.5 D4:0.5 D4:0.5 D4:0.5 D4:0.5 | E4 D4 C4:2",
                chords = "C:4 C:4 C:2 G:2 C:4", origin = "English traditional street cry (18th c.)" },
            new Song { id = "mary", title = "Mary Had a Little Lamb", picture = "lamb", tempo = 104,
                melody = "E4 D4 C4 D4 | E4 E4 E4:2 | D4 D4 D4:2 | E4 G4 G4:2 | E4 D4 C4 D4 | E4 E4 E4 E4 | D4 D4 E4 D4 | C4:4",
                chords = "C:4 C:4 G:4 C:4 C:4 C:4 G:4 C:4", origin = "Sarah Josepha Hale / Lowell Mason, 1830 (public domain)" },
            new Song { id = "twinkle", title = "Twinkle Twinkle Little Star", picture = "star", tempo = 100,
                melody = "C4 C4 G4 G4 | A4 A4 G4:2 | F4 F4 E4 E4 | D4 D4 C4:2 | G4 G4 F4 F4 | E4 E4 D4:2 | G4 G4 F4 F4 | E4 E4 D4:2 | C4 C4 G4 G4 | A4 A4 G4:2 | F4 F4 E4 E4 | D4 D4 C4:2",
                chords = "C:2 C:2 F:2 C:2 F:2 C:2 G:2 C:2 C:2 F:2 C:2 G:2 C:2 F:2 C:2 G:2 C:2 C:2 F:2 C:2 F:2 C:2 G:2 C:2",
                origin = "French melody 'Ah! vous dirai-je, maman' (1761); words Jane Taylor (1806)" },
            new Song { id = "abc", title = "The Alphabet Song", picture = "abc", tempo = 100,
                melody = "C4 C4 G4 G4 | A4 A4 G4:2 | F4 F4 E4 E4 | D4:0.5 D4:0.5 D4:0.5 D4:0.5 C4:2 | G4 G4 F4:2 | E4 E4 D4:2 | G4:0.5 G4:0.5 G4 F4:2 | E4 E4 D4:2 | C4 C4 G4 G4 | A4 A4 G4:2 | F4 F4 E4 E4 | D4 D4 C4:2",
                chords = "C:2 C:2 F:2 C:2 F:2 C:2 G:2 C:2 C:2 F:2 C:2 G:2 C:2 F:2 C:2 G:2 C:2 C:2 F:2 C:2 F:2 C:2 G:2 C:2",
                origin = "Same 1761 French melody; alphabet words (C. Bradlee, 1835)" },
            new Song { id = "frerejacques", title = "Frère Jacques", picture = "bell", tempo = 108,
                melody = "C4 D4 E4 C4 | C4 D4 E4 C4 | E4 F4 G4:2 | E4 F4 G4:2 | G4:0.5 A4:0.5 G4:0.5 F4:0.5 E4 C4 | G4:0.5 A4:0.5 G4:0.5 F4:0.5 E4 C4 | C4 G3 C4:2 | C4 G3 C4:2",
                chords = "C:4 C:4 C:4 C:4 C:4 C:4 C:2 G:2 C:4", origin = "French traditional round (18th c.)" },
            new Song { id = "rowboat", title = "Row, Row, Row Your Boat", picture = "boat", tempo = 92,
                melody = "C4:1.5 C4:1.5 C4:1 D4:0.5 E4:1.5 | E4:1 D4:0.5 E4:1 F4:0.5 G4:3 | C5:0.5 C5:0.5 C5:0.5 G4:0.5 G4:0.5 G4:0.5 E4:0.5 E4:0.5 E4:0.5 C4:0.5 C4:0.5 C4:0.5 | G4:1 F4:0.5 E4:1 D4:0.5 C4:3",
                chords = "C:6 C:6 C:6 G:3 C:3", origin = "American traditional (published 1852)" },
            new Song { id = "london", title = "London Bridge", picture = "bridge", tempo = 108,
                melody = "G4:1.5 A4:0.5 G4 F4 | E4 F4 G4:2 | D4 E4 F4:2 | E4 F4 G4:2 | G4:1.5 A4:0.5 G4 F4 | E4 F4 G4:2 | D4:2 G4:2 | E4 C4:3",
                chords = "C:4 C:4 G:4 C:4 C:4 C:4 G:4 C:4", origin = "English traditional nursery rhyme" },
            new Song { id = "oldmacdonald", title = "Old MacDonald Had a Farm", picture = "cow", tempo = 112,
                melody = "C4 C4 C4 G3 | A3 A3 G3:2 | E4 E4 D4 D4 | C4:3 G3 | C4 C4 C4 G3 | A3 A3 G3:2 | E4 E4 D4 D4 | C4:4",
                chords = "C:4 F:2 C:2 C:2 G:2 C:4 C:4 F:2 C:2 C:2 G:2 C:4", origin = "American/English folk song (published 1917, traditional earlier)" },
            new Song { id = "itsybitsy", title = "Itsy Bitsy Spider", picture = "spider", tempo = 104,
                melody = "G3:0.5 C4 C4:0.5 C4 D4:0.5 E4:1.5 E4 | E4:0.5 D4 C4:0.5 D4 E4:0.5 C4:3 | E4:1.5 E4:0.5 F4 G4:2 | G4:1.5 F4:0.5 E4 F4 G4 E4:2 | C4:1.5 C4:0.5 D4 E4:2 | E4:1.5 D4:0.5 C4 D4 E4 C4:2 | G3:0.5 G3:0.5 C4 C4:0.5 C4 D4:0.5 E4:1.5 E4 | E4:0.5 D4 C4:0.5 D4 E4:0.5 C4:3",
                origin = "American traditional nursery song" },
            new Song { id = "odetojoy", title = "Ode to Joy", picture = "sun", tempo = 104,
                melody = "E4 E4 F4 G4 | G4 F4 E4 D4 | C4 C4 D4 E4 | E4:1.5 D4:0.5 D4:2 | E4 E4 F4 G4 | G4 F4 E4 D4 | C4 C4 D4 E4 | D4:1.5 C4:0.5 C4:2",
                chords = "C:4 G:4 C:4 G:4 C:4 G:4 C:2 G:2 C:4", origin = "Ludwig van Beethoven, Symphony No. 9 (1824), public domain" },
            new Song { id = "jinglebells", title = "Jingle Bells", picture = "bells", tempo = 120,
                melody = "E4 E4 E4:2 | E4 E4 E4:2 | E4 G4 C4:1.5 D4:0.5 | E4:4 | F4 F4 F4:1.5 F4:0.5 | F4 E4 E4 E4:0.5 E4:0.5 | E4 D4 D4 E4 | D4:2 G4:2 | E4 E4 E4:2 | E4 E4 E4:2 | E4 G4 C4:1.5 D4:0.5 | E4:4 | F4 F4 F4 F4 | F4 E4 E4 E4:0.5 E4:0.5 | G4 G4 F4 D4 | C4:4",
                chords = "C:4 C:4 C:4 C:4 F:4 C:4 D:4 G:4 C:4 C:4 C:4 C:4 F:4 C:4 G:4 C:4", origin = "James Lord Pierpont (1857), public domain" },
            new Song { id = "thisoldman", title = "This Old Man", picture = "drum", tempo = 112,
                melody = "G4 E4 G4:2 | G4 E4 G4:2 | A4 G4 F4 E4 | D4 E4 F4:2 | E4 F4 G4 C4 | C4 C4:0.5 C4:0.5 C4 D4 | E4 F4 G4:2 | G4 D4 F4 E4 | D4 C4:3",
                chords = "C:4 C:4 F:4 G:4 C:4 C:4 C:4 G:4 C:4", origin = "English traditional counting song" },
        };

        public static Song Get(string id) => Songs.Find(s => s.id == id);

        /// <summary>A guided-melody lesson: a song, a difficulty tier and the notes the child plays.</summary>
        public sealed class Lesson
        {
            public Song song;
            public int tier;                 // 0 beginner (3–4 notes) … 3 advanced (whole song)
            public List<NoteEvent> notes;
            public string Key => song.id + ":" + tier;
        }

        public static readonly string[] TierNames = { "Beginner", "Easy", "Intermediate", "Advanced" };

        /// <summary>
        /// Lessons in difficulty order. Beginner = first 3–4 notes, Easy = 5–6, Intermediate = 7–10, Advanced = the
        /// whole song (as the GDD's Follow the Melody tiers).
        /// </summary>
        public static List<Lesson> Lessons()
        {
            var list = new List<Lesson>();
            int[] caps = { 4, 6, 10, int.MaxValue };
            int[] mins = { 3, 5, 7, 11 };
            for (int tier = 0; tier < 4; tier++)
            {
                foreach (Song s in Songs)
                {
                    List<NoteEvent> m = s.Melody;
                    int count = PhraseLength(m, mins[tier], caps[tier]);
                    if (count < mins[tier] || (tier == 3 && m.Count < 11)) continue;
                    list.Add(new Lesson { song = s, tier = tier, notes = m.GetRange(0, Math.Min(count, m.Count)) });
                }
            }
            return list;
        }

        /// <summary>Longest musically sensible prefix (ends on a longer note when possible) within the bounds.</summary>
        private static int PhraseLength(List<NoteEvent> m, int min, int max)
        {
            if (max == int.MaxValue) return m.Count;
            int best = Math.Min(max, m.Count);
            for (int n = Math.Min(max, m.Count); n >= min; n--)
                if (m[n - 1].length >= 2f || n == m.Count) return n;
            return best;
        }

        // ------------------------------------------------------------------ accompaniment

        private static readonly Dictionary<string, int[]> Chords = new Dictionary<string, int[]>
        {
            { "C", new[] { 48, 52, 55 } }, { "F", new[] { 41, 45, 48 } }, { "G", new[] { 43, 47, 50 } },
            { "Am", new[] { 45, 48, 52 } }, { "D", new[] { 50, 54, 57 } }, { "Em", new[] { 40, 43, 47 } },
            { "Dm", new[] { 50, 53, 57 } }, { "G7", new[] { 43, 47, 53 } },
        };

        /// <summary>Bass on beat 1 and a soft chord on beats 2–4 (or bass + chord for 2-beat chords).</summary>
        public static List<NoteEvent> Accompaniment(Song s, int velocity = 62)
        {
            var list = new List<NoteEvent>();
            if (string.IsNullOrEmpty(s.chords)) return list;
            float beat = 0f;
            foreach (string token in s.chords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = token.Split(':');
                float len = p.Length > 1 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 4f;
                if (!Chords.TryGetValue(p[0], out int[] tones)) { beat += len; continue; }
                list.Add(new NoteEvent(beat, Math.Min(len, 1.5f), tones[0] - 12, velocity + 6));
                for (float b = 1f; b < len; b += 1f)
                    foreach (int t in tones) list.Add(new NoteEvent(beat + b, 0.8f, t, velocity - 8));
                if (len < 2f) foreach (int t in tones) list.Add(new NoteEvent(beat, len * 0.9f, t, velocity - 8));
                beat += len;
            }
            return list;
        }
    }
}
