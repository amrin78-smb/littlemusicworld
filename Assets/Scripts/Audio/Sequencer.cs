using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleMusicWorld.Audio
{
    /// <summary>A part in a sequence: one instrument on one mixer channel.</summary>
    public sealed class SequenceTrack
    {
        public InstrumentDefinition instrument;
        public int channel;
        public float pan;
        public List<NoteEvent> events = new List<NoteEvent>();
        internal int nextIndex;
    }

    /// <summary>
    /// Plays parts in beat time through <see cref="MusicalAudioEngine"/> with a short look-ahead so notes are
    /// sample-accurate even when the frame rate dips. Fires <see cref="NoteVisual"/> on the main thread when each
    /// note is actually heard, so animations (musicians, keys, timeline playhead) stay in sync with the sound.
    /// Used by guided demonstrations, Studio playback, Orchestra performances and mini-games.
    /// </summary>
    public sealed class Sequencer : MonoBehaviour
    {
        public const double LookAhead = 0.12;

        public event Action<int, NoteEvent> NoteVisual; // track index, note
        public event Action<int> BeatTick;              // whole-beat index, on the beat
        public event Action Finished;

        public readonly List<SequenceTrack> Tracks = new List<SequenceTrack>();
        public bool IsPlaying { get; private set; }
        public bool Loop { get; set; }
        public float LengthBeats { get; set; } // 0 = auto (end of last note)

        private float bpm = 100f;
        private double anchorDsp;   // dsp time at which anchorBeat sounds
        private double anchorBeat;
        private double scheduledUntilBeat;
        private int lastTickBeat = -1;

        private struct Pending { public double time; public int track; public NoteEvent ev; }
        private readonly List<Pending> visuals = new List<Pending>(256);

        public float Tempo
        {
            get => bpm;
            set
            {
                float v = Mathf.Clamp(value, 40f, 200f);
                if (IsPlaying)
                {
                    // Re-anchor at the scheduling horizon so already-scheduled notes keep their time.
                    double horizonTime = BeatToDsp(scheduledUntilBeat);
                    anchorBeat = scheduledUntilBeat;
                    anchorDsp = horizonTime;
                }
                bpm = v;
            }
        }

        public double CurrentBeat => IsPlaying ? anchorBeat + (AudioSettings.dspTime - anchorDsp) * bpm / 60.0 : 0.0;

        private double BeatToDsp(double beat) => anchorDsp + (beat - anchorBeat) * 60.0 / bpm;

        public SequenceTrack AddTrack(InstrumentDefinition instrument, int channel, IEnumerable<NoteEvent> events, float pan = 0f)
        {
            var t = new SequenceTrack { instrument = instrument, channel = channel, pan = pan };
            if (events != null) t.events.AddRange(events);
            t.events.Sort((a, b) => a.beat.CompareTo(b.beat));
            Tracks.Add(t);
            MusicalAudioEngine.Instance?.Load(instrument);
            return t;
        }

        public void Clear()
        {
            Stop();
            Tracks.Clear();
        }

        public void Play(float fromBeat = 0f, double startDelay = 0.08)
        {
            if (MusicalAudioEngine.Instance == null) return;
            Stop();
            anchorBeat = fromBeat;
            anchorDsp = AudioSettings.dspTime + startDelay;
            scheduledUntilBeat = fromBeat;
            lastTickBeat = Mathf.FloorToInt(fromBeat) - 1;
            foreach (SequenceTrack t in Tracks)
            {
                t.nextIndex = 0;
                while (t.nextIndex < t.events.Count && t.events[t.nextIndex].beat < fromBeat) t.nextIndex++;
            }
            IsPlaying = true;
        }

        public void Stop()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            visuals.Clear();
            MusicalAudioEngine e = MusicalAudioEngine.Instance;
            if (e != null) foreach (SequenceTrack t in Tracks) e.StopChannel(t.channel);
        }

        private float EffectiveLength()
        {
            if (LengthBeats > 0f) return LengthBeats;
            float end = 0f;
            foreach (SequenceTrack t in Tracks) end = Mathf.Max(end, NoteUtil.LengthInBeats(t.events));
            return Mathf.Max(1f, Mathf.Ceil(end));
        }

        private void Update()
        {
            if (!IsPlaying) return;
            MusicalAudioEngine e = MusicalAudioEngine.Instance;
            if (e == null) return;
            double now = AudioSettings.dspTime;
            double horizonBeat = anchorBeat + (now + LookAhead - anchorDsp) * bpm / 60.0;
            float length = EffectiveLength();

            // Schedule notes up to the horizon (wrapping when looping).
            int guard = 0;
            while (scheduledUntilBeat < horizonBeat && guard++ < 8)
            {
                double windowEnd = Math.Min(horizonBeat, length);
                for (int ti = 0; ti < Tracks.Count; ti++)
                {
                    SequenceTrack t = Tracks[ti];
                    while (t.nextIndex < t.events.Count && t.events[t.nextIndex].beat < windowEnd)
                    {
                        NoteEvent ev = t.events[t.nextIndex++];
                        double on = BeatToDsp(ev.beat);
                        e.NoteOnAt(t.instrument, ev.note, ev.velocity, t.channel, on, t.pan);
                        if (ev.length > 0f) e.NoteOffAt(t.instrument, ev.note, t.channel, BeatToDsp(ev.beat + ev.length * 0.95f));
                        visuals.Add(new Pending { time = on, track = ti, ev = ev });
                    }
                }
                scheduledUntilBeat = windowEnd;
                if (scheduledUntilBeat >= length)
                {
                    if (!Loop) break;
                    // Wrap: shift the anchor so beat 0 of the next pass follows the end of this one.
                    anchorDsp = BeatToDsp(length);
                    anchorBeat = 0;
                    horizonBeat = (now + LookAhead - anchorDsp) * bpm / 60.0;
                    scheduledUntilBeat = 0;
                    foreach (SequenceTrack t in Tracks) t.nextIndex = 0;
                }
            }

            // Fire visuals whose sound has started.
            for (int i = 0; i < visuals.Count;)
            {
                if (visuals[i].time <= now)
                {
                    Pending p = visuals[i];
                    visuals.RemoveAt(i);
                    NoteVisual?.Invoke(p.track, p.ev);
                }
                else i++;
            }

            int beatNow = (int)Math.Floor(CurrentBeat);
            if (beatNow != lastTickBeat && beatNow >= 0)
            {
                lastTickBeat = beatNow;
                BeatTick?.Invoke(beatNow);
            }

            if (!Loop && scheduledUntilBeat >= length && visuals.Count == 0 && CurrentBeat >= length)
            {
                IsPlaying = false;
                Finished?.Invoke();
            }
        }
    }
}
