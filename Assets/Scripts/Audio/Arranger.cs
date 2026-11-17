using System;
using System.Collections.Generic;
using System.Globalization;

namespace LittleMusicWorld.Audio
{
    /// <summary>
    /// Turns a song (melody + chord symbols) into a part for any instrument, so every combination of musicians sounds
    /// like a band: the instrument's family and play style decide its role.
    ///   Melody  – winds, bowed violin, mallets, harp, keys (when no other melody player)
    ///   Harmony – a third below the melody (second melody player)
    ///   Chords  – strummed strings (on the beat), keyboards (bass + chord)
    ///   Bass    – cello, bass drum gets the downbeats
    ///   Beat    – drum kit (kick / snare / hi-hat pattern)
    ///   Groove  – shakers, tambourine, bongos, congas (off-beat patterns)
    /// All parts are clamped into each instrument's playable range.
    /// </summary>
    public static class Arranger
    {
        public enum Role { Melody, Harmony, Chords, Bass, Beat, Groove }

        public static Role RoleFor(InstrumentDefinition d, bool melodyTaken)
        {
            if (d == null) return Role.Melody;
            switch (d.id)
            {
                case "DrumKit": return Role.Beat;
                case "BassDrum": case "Snare": case "Cymbals": return Role.Beat;
                case "Maracas": case "Tambourine": case "Bongos": case "Congas": return Role.Groove;
                case "Cello": return Role.Bass;
                case "AcousticGuitar": case "ElectricGuitar": case "Ukulele": case "Banjo": return Role.Chords;
            }
            if (d.playStyle == PlayStyle.Keyboard) return melodyTaken ? Role.Chords : Role.Melody;
            return melodyTaken ? Role.Harmony : Role.Melody;
        }

        public static List<NoteEvent> Part(SongLibrary.Song song, InstrumentDefinition d, Role role)
        {
            float length = SongLength(song);
            var chords = ChordTimeline(song, length);
            List<NoteEvent> list;
            switch (role)
            {
                case Role.Melody: list = Fit(song.Melody, d, 0); break;
                case Role.Harmony: list = Harmony(song, d); break;
                case Role.Chords: list = d.playStyle == PlayStyle.Keyboard ? KeyChords(chords, d) : Strums(chords, d); break;
                case Role.Bass: list = Bass(chords, d); break;
                case Role.Beat: list = Beat(d, length); break;
                default: list = Groove(d, length); break;
            }
            list.Sort((a, b) => a.beat.CompareTo(b.beat));
            return list;
        }

        public static float SongLength(SongLibrary.Song song)
        {
            float melody = NoteUtil.LengthInBeats(song.Melody);
            float bars = (float)Math.Ceiling(melody / 4f);
            return Math.Max(4f, bars * 4f);
        }

        // ------------------------------------------------------------------ chord timeline

        private static readonly Dictionary<string, int[]> ChordTones = new Dictionary<string, int[]>
        {
            { "C", new[] { 0, 4, 7 } }, { "F", new[] { 5, 9, 0 } }, { "G", new[] { 7, 11, 2 } }, { "Am", new[] { 9, 0, 4 } },
            { "D", new[] { 2, 6, 9 } }, { "Em", new[] { 4, 7, 11 } }, { "Dm", new[] { 2, 5, 9 } }, { "G7", new[] { 7, 11, 5 } },
        };

        public struct ChordSpan { public float beat, length; public int[] pcs; }

        public static List<ChordSpan> ChordTimeline(SongLibrary.Song song, float length)
        {
            var list = new List<ChordSpan>();
            float beat = 0f;
            if (!string.IsNullOrEmpty(song.chords))
            {
                foreach (string tok in song.chords.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] p = tok.Split(':');
                    float len = p.Length > 1 ? float.Parse(p[1], CultureInfo.InvariantCulture) : 4f;
                    if (ChordTones.TryGetValue(p[0], out int[] pcs)) list.Add(new ChordSpan { beat = beat, length = len, pcs = pcs });
                    beat += len;
                }
            }
            // Songs without chord symbols: harmonise each bar from its melody notes (I, IV or V, whichever fits best).
            while (beat < length)
            {
                int[] best = ChordTones["C"];
                int bestScore = -1;
                foreach (string name in new[] { "C", "F", "G" })
                {
                    int score = 0;
                    foreach (NoteEvent e in song.Melody)
                        if (e.beat >= beat && e.beat < beat + 4f && Array.IndexOf(ChordTones[name], e.note % 12) >= 0) score++;
                    if (score > bestScore) { bestScore = score; best = ChordTones[name]; }
                }
                list.Add(new ChordSpan { beat = beat, length = 4f, pcs = best });
                beat += 4f;
            }
            return list;
        }

        // ------------------------------------------------------------------ parts

        private static int Range(InstrumentDefinition d, out int high)
        {
            int[] play = d.playableNotes;
            int low = play != null && play.Length > 0 ? play[0] : d.lowestNote;
            high = play != null && play.Length > 0 ? play[play.Length - 1] : d.highestNote;
            if (d.playStyle == PlayStyle.Keyboard || d.playStyle == PlayStyle.Strum) { low = Math.Max(d.lowestNote, 36); high = Math.Min(d.highestNote, 96); }
            return low;
        }

        /// <summary>Moves the whole melody by octaves to sit inside the instrument's range (keeps its shape).</summary>
        public static List<NoteEvent> Fit(List<NoteEvent> src, InstrumentDefinition d, int transpose)
        {
            int low = Range(d, out int high);
            int lo = int.MaxValue, hi = int.MinValue;
            foreach (NoteEvent e in src) { lo = Math.Min(lo, e.note + transpose); hi = Math.Max(hi, e.note + transpose); }
            int shift = 0; // octaves added on top of the transpose
            while (lo + shift < low && shift < 48) shift += 12;
            while (hi + shift > high && lo + shift - 12 >= low - 2) shift -= 12;
            var list = new List<NoteEvent>(src.Count);
            foreach (NoteEvent e in src)
            {
                NoteEvent n = e;
                n.note = Math.Max(low, Math.Min(high, e.note + transpose + shift));
                list.Add(n);
            }
            return list;
        }

        private static readonly int[] CMajor = { 0, 2, 4, 5, 7, 9, 11 };

        private static List<NoteEvent> Harmony(SongLibrary.Song song, InstrumentDefinition d)
        {
            var third = new List<NoteEvent>();
            foreach (NoteEvent e in song.Melody)
            {
                // diatonic third below in C major
                int pc = ((e.note % 12) + 12) % 12;
                int idx = Array.IndexOf(CMajor, pc);
                NoteEvent n = e;
                if (idx >= 0)
                {
                    int down = CMajor[(idx + 5) % 7];
                    int diff = (pc - down + 12) % 12;
                    n.note = e.note - diff;
                }
                n.velocity = Math.Max(60, e.velocity - 15);
                third.Add(n);
            }
            return Fit(third, d, 0);
        }

        private static int Nearest(int pc, int around)
        {
            int n = around - ((around % 12 - pc + 12) % 12);
            if (around - n > 6) n += 12;
            return n;
        }

        private static List<NoteEvent> Strums(List<ChordSpan> chords, InstrumentDefinition d)
        {
            var list = new List<NoteEvent>();
            int[] open = d.openStrings != null && d.openStrings.Length > 0 ? d.openStrings : new[] { 52, 57, 62, 67 };
            foreach (ChordSpan c in chords)
            {
                for (float b = 0; b < c.length; b += 1f)
                {
                    bool down = ((int)(c.beat + b)) % 2 == 0;
                    for (int s = 0; s < open.Length; s++)
                    {
                        int str = down ? s : open.Length - 1 - s;
                        int note = open[str];
                        for (int up = 0; up <= 7; up++) if (Array.IndexOf(c.pcs, (open[str] + up) % 12) >= 0) { note = open[str] + up; break; }
                        list.Add(new NoteEvent(c.beat + b + s * 0.018f, 0f, note, b == 0 ? 92 : 72));
                    }
                }
            }
            return list;
        }

        private static List<NoteEvent> KeyChords(List<ChordSpan> chords, InstrumentDefinition d)
        {
            var list = new List<NoteEvent>();
            foreach (ChordSpan c in chords)
            {
                list.Add(new NoteEvent(c.beat, Math.Min(c.length, 2f), Nearest(c.pcs[0], 48), 80));
                for (float b = 1f; b < c.length; b += 1f)
                    foreach (int pc in c.pcs) list.Add(new NoteEvent(c.beat + b, 0.8f, Nearest(pc, 62), 62));
            }
            return list;
        }

        private static List<NoteEvent> Bass(List<ChordSpan> chords, InstrumentDefinition d)
        {
            var list = new List<NoteEvent>();
            int low = Range(d, out int high);
            foreach (ChordSpan c in chords)
            {
                int root = Nearest(c.pcs[0], Math.Max(low + 5, 43));
                int fifth = Nearest(c.pcs[2], root + 7);
                for (float b = 0; b < c.length; b += 2f)
                    list.Add(new NoteEvent(c.beat + b, 1.8f, b % 4 == 0 ? root : fifth, 88));
            }
            return list;
        }

        private static List<NoteEvent> Beat(InstrumentDefinition d, float length)
        {
            var list = new List<NoteEvent>();
            for (float b = 0; b < length; b += 0.5f)
            {
                int beatInBar = (int)b % 4;
                bool onBeat = Math.Abs(b - Math.Floor(b)) < 0.01;
                switch (d.id)
                {
                    case "DrumKit":
                        list.Add(new NoteEvent(b, 0f, 42, onBeat ? 80 : 58));                         // hi-hat eighths
                        if (onBeat && (beatInBar == 0 || beatInBar == 2)) list.Add(new NoteEvent(b, 0f, 36, 110)); // kick
                        if (onBeat && (beatInBar == 1 || beatInBar == 3)) list.Add(new NoteEvent(b, 0f, 38, 100)); // snare
                        if (b > 0 && b % 16 == 0) list.Add(new NoteEvent(b, 0f, 49, 100));       // crash every 4 bars
                        break;
                    case "BassDrum":
                        if (onBeat && (beatInBar == 0 || beatInBar == 2)) list.Add(new NoteEvent(b, 0f, 36, 110));
                        break;
                    case "Snare":
                        if (onBeat && (beatInBar == 1 || beatInBar == 3)) list.Add(new NoteEvent(b, 0f, 38, 100));
                        break;
                    case "Cymbals":
                        if (onBeat && beatInBar == 0 && ((int)b / 4) % 2 == 0) list.Add(new NoteEvent(b, 0f, 49, 90));
                        else if (onBeat) list.Add(new NoteEvent(b, 0f, 51, 70));
                        break;
                }
            }
            return list;
        }

        private static List<NoteEvent> Groove(InstrumentDefinition d, float length)
        {
            var list = new List<NoteEvent>();
            for (float b = 0; b < length; b += 0.5f)
            {
                bool offBeat = Math.Abs(b - Math.Floor(b) - 0.5f) < 0.01;
                int beatInBar = (int)b % 4;
                switch (d.id)
                {
                    case "Maracas": list.Add(new NoteEvent(b, 0f, offBeat ? 70 : 82, offBeat ? 90 : 60)); break;
                    case "Tambourine": if (offBeat || beatInBar == 1 || beatInBar == 3) list.Add(new NoteEvent(b, 0f, 54, offBeat ? 70 : 95)); break;
                    case "Bongos": if (!offBeat || beatInBar == 3) list.Add(new NoteEvent(b, 0f, offBeat || beatInBar % 2 == 1 ? 60 : 61, 90)); break;
                    case "Congas":
                        if (!offBeat) list.Add(new NoteEvent(b, 0f, beatInBar == 0 ? 65 : beatInBar == 2 ? 64 : 63, 95));
                        else if (beatInBar == 1 || beatInBar == 3) list.Add(new NoteEvent(b, 0f, 62, 70));
                        break;
                    default: if (offBeat) list.Add(new NoteEvent(b, 0f, d.zones.Count > 0 ? d.zones[0].rootNote : 60, 80)); break;
                }
            }
            return list;
        }
    }
}
