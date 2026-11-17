using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleMusicWorld.Audio
{
    public enum InstrumentFamily { Keys, Percussion, Strings, Wind }

    /// <summary>How a child plays the instrument; selects the on-screen interface.</summary>
    public enum PlayStyle { Keyboard, DrumPads, Strum, Bow, Pluck, Blow, Mallets }

    /// <summary>One recorded sample covering a key and velocity range.</summary>
    [Serializable]
    public sealed class SampleZone
    {
        public AudioClip clip;
        [Tooltip("MIDI note the recording was made at.")] public int rootNote = 60;
        public int lowNote = 0;
        public int highNote = 127;
        public int lowVelocity = 0;
        public int highVelocity = 127;
        [Tooltip("Sustain loop in samples (source rate). loopEnd <= loopStart disables the loop.")]
        public int loopStart = -1;
        public int loopEnd = -1;
        [Tooltip("Fine tuning measured by the build script, in cents.")] public float tuneCents;
        public float gain = 1f;
        [Tooltip("Optional label for drum/percussion pads (e.g. Kick, Snare).")] public string label;
    }

    /// <summary>
    /// Data for one playable instrument: its samples, envelope and the way it is played. Built by the editor
    /// generator from Tools/audio output (Assets/Audio/Instruments/*/instrument.json); never hand-edited.
    /// </summary>
    [CreateAssetMenu(menuName = "Little Music World/Instrument")]
    public sealed class InstrumentDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public InstrumentFamily family;
        public PlayStyle playStyle;
        public Color color = Color.white;
        public Sprite icon;

        [Header("Range")]
        public int lowestNote = 48;
        public int highestNote = 84;
        [Tooltip("Notes shown on the child-sized interface (keys, strings, holes or pads).")]
        public int[] playableNotes = Array.Empty<int>();

        [Header("Envelope")]
        [Tooltip("Seconds. Short fade-in avoids clicks; longer for bowed/blown swells.")] public float attack = 0.004f;
        [Tooltip("Seconds of fade after note-off.")] public float release = 0.35f;
        [Tooltip("Percussion and plucked sounds ring out regardless of note-off.")] public bool oneShot;
        [Tooltip("A held note loops its sustain region (bowed, blown, organ).")] public bool sustainLoop;
        [Tooltip("Loudness trim so instruments sit together in a mix.")] public float volume = 1f;
        [Tooltip("Max voices this instrument may use at once (oldest stolen first).")] public int maxVoices = 16;
        [Tooltip("Notes in the same non-zero choke group cut each other (open/closed hi-hat).")]
        public int[] chokeGroups = Array.Empty<int>();

        public List<SampleZone> zones = new List<SampleZone>();

        public bool IsPercussion => family == InstrumentFamily.Percussion && playStyle == PlayStyle.DrumPads;

        public int ChokeGroupOf(int note)
        {
            if (chokeGroups == null || chokeGroups.Length == 0) return 0;
            int i = note - lowestNote;
            return i >= 0 && i < chokeGroups.Length ? chokeGroups[i] : 0;
        }

        public SampleZone FindZone(int note, int velocity)
        {
            SampleZone best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < zones.Count; i++)
            {
                SampleZone z = zones[i];
                if (velocity < z.lowVelocity || velocity > z.highVelocity) continue;
                if (note >= z.lowNote && note <= z.highNote) return z;
                int d = Math.Min(Math.Abs(note - z.lowNote), Math.Abs(note - z.highNote));
                if (d < bestDistance) { bestDistance = d; best = z; }
            }
            return best;
        }
    }
}
