using System;
using System.Collections.Generic;
using System.Globalization;

namespace LittleMusicWorld.Audio
{
    /// <summary>One note in beat time. Shared by songs, Studio recordings and Orchestra parts.</summary>
    [Serializable]
    public struct NoteEvent
    {
        public float beat;     // start, in beats from the beginning
        public float length;   // beats (0 = let ring / one-shot)
        public int note;       // MIDI note number
        public int velocity;   // 1..127

        public NoteEvent(float beat, float length, int note, int velocity = 100)
        {
            this.beat = beat; this.length = length; this.note = note; this.velocity = velocity;
        }
    }

    public static class NoteUtil
    {
        private static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>Child-friendly colour per pitch class (do-re-mi rainbow), shared by keys, bells and the Studio.</summary>
        public static readonly UnityEngine.Color[] PitchColors =
        {
            Hex(0xFF5A5F), Hex(0xFF7F50), Hex(0xFFA62B), Hex(0xFFC93C), Hex(0xFFE156), Hex(0x8BD346),
            Hex(0x3CC48A), Hex(0x2EC4B6), Hex(0x3FA7F5), Hex(0x5B7CFA), Hex(0x8C6CF2), Hex(0xD46CE0)
        };

        public static UnityEngine.Color ColorOf(int note) => PitchColors[((note % 12) + 12) % 12];

        private static UnityEngine.Color Hex(int rgb) =>
            new UnityEngine.Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        public static string Name(int note) => Names[((note % 12) + 12) % 12] + (note / 12 - 1);

        public static bool IsBlackKey(int note)
        {
            int pc = ((note % 12) + 12) % 12;
            return pc == 1 || pc == 3 || pc == 6 || pc == 8 || pc == 10;
        }

        /// <summary>Parses "C4", "F#3", "Bb2" → MIDI number (C4 = 60). Returns -1 for rests ("R"/"-").</summary>
        public static int Parse(string s)
        {
            s = s.Trim();
            if (s == "R" || s == "-" || s.Length == 0) return -1;
            int pc = "C D EF G A B".IndexOf(char.ToUpperInvariant(s[0]));
            if (pc < 0) throw new FormatException("bad note " + s);
            int i = 1;
            if (i < s.Length && s[i] == '#') { pc++; i++; }
            else if (i < s.Length && s[i] == 'b') { pc--; i++; }
            int octave = int.Parse(s.Substring(i), CultureInfo.InvariantCulture);
            return (octave + 1) * 12 + pc;
        }

        /// <summary>
        /// Parses a compact melody: "C4:1 C4:1 G4:1 G4:1 A4:1 A4:1 G4:2 R:1". Lengths are in beats; ":" length
        /// defaults to 1. Chords are written C4+E4+G4:2.
        /// </summary>
        public static List<NoteEvent> ParseMelody(string text, int velocity = 100)
        {
            var list = new List<NoteEvent>();
            float beat = 0f;
            foreach (string token in text.Split(new[] { ' ', '\n', '\t', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = token.Split(':');
                float len = parts.Length > 1 ? float.Parse(parts[1], CultureInfo.InvariantCulture) : 1f;
                foreach (string n in parts[0].Split('+'))
                {
                    int note = Parse(n);
                    if (note >= 0) list.Add(new NoteEvent(beat, len, note, velocity));
                }
                beat += len;
            }
            return list;
        }

        public static float LengthInBeats(IReadOnlyList<NoteEvent> events)
        {
            float end = 0f;
            for (int i = 0; i < events.Count; i++) end = Math.Max(end, events[i].beat + Math.Max(events[i].length, 0.25f));
            return end;
        }
    }
}
