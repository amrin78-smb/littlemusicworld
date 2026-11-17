using System;
using System.Collections;
using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using LittleMusicWorld.Instruments;
using UnityEngine;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// "Follow the Melody" (GDD 6.3). The phrase is demonstrated first (keys light in time with the music), then the
    /// next key glows and the child presses keys in order. There is no timer and no failure: a wrong key simply
    /// sounds; after a few wrong keys or a long pause the next note is shown again. Finishing a phrase celebrates
    /// and plays the whole song back with accompaniment as the reward.
    /// </summary>
    public sealed class MelodyGuide : MonoBehaviour
    {
        public enum Phase { Idle, Demonstrating, Following, Celebrating }

        public Phase State { get; private set; } = Phase.Idle;
        public SongLibrary.Lesson Lesson { get; private set; }
        public int Index { get; private set; }
        public int Total => notes.Count;

        public event Action<Phase> PhaseChanged;
        public event Action<int, int> Progress;         // index, total
        public event Action<int> CorrectNote;            // note
        public event Action<int> HintShown;              // note
        public event Action<SongLibrary.Lesson> Completed;

        [SerializeField] private float hintAfterSeconds = 6f;
        [SerializeField] private int hintAfterWrong = 3;

        private KeyboardSurface keyboard;
        private Sequencer seq;
        private readonly List<NoteEvent> notes = new List<NoteEvent>();
        private int wrongCount;
        private float idleTime;
        private Coroutine running;

        public void Begin(KeyboardSurface kb, SongLibrary.Lesson lesson)
        {
            Stop();
            keyboard = kb;
            Lesson = lesson;
            notes.Clear();
            notes.AddRange(FitToKeyboard(lesson.notes, kb));
            Index = 0;
            if (seq == null)
            {
                seq = gameObject.AddComponent<Sequencer>();
                seq.NoteVisual += (track, ev) => { if (track == 0) keyboard.Flash(ev.note); };
            }
            keyboard.KeyDown += OnKey;
            running = StartCoroutine(Demonstrate(true));
        }

        public void Stop()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            if (seq != null) seq.Stop();
            if (keyboard != null)
            {
                keyboard.KeyDown -= OnKey;
                keyboard.ClearGuides();
                keyboard.interactable = true;
            }
            SetPhase(Phase.Idle);
        }

        public void Replay()
        {
            if (State == Phase.Following || State == Phase.Idle)
            {
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(Demonstrate(false));
            }
        }

        /// <summary>Moves the melody by octaves so every note is on the visible keyboard.</summary>
        public static List<NoteEvent> FitToKeyboard(List<NoteEvent> src, KeyboardSurface kb)
        {
            var list = new List<NoteEvent>(src);
            if (list.Count == 0) return list;
            int lo = int.MaxValue, hi = int.MinValue;
            foreach (NoteEvent e in list) { lo = Math.Min(lo, e.note); hi = Math.Max(hi, e.note); }
            int shift = 0;
            while (lo + shift < kb.LowNote) shift += 12;
            while (hi + shift > kb.HighNote && lo + shift - 12 >= kb.LowNote) shift -= 12;
            for (int i = 0; i < list.Count; i++)
            {
                NoteEvent e = list[i];
                e.note += shift;
                list[i] = e;
            }
            return list;
        }

        private void SetPhase(Phase p)
        {
            if (State == p) return;
            State = p;
            PhaseChanged?.Invoke(p);
        }

        private IEnumerator Demonstrate(bool first)
        {
            SetPhase(Phase.Demonstrating);
            keyboard.ClearGuides();
            keyboard.interactable = false;
            yield return new WaitForSeconds(first ? 1.0f : 0.3f);
            seq.Clear();
            float tempo = Lesson.song.tempo * (Lesson.tier == 0 ? 0.75f : Lesson.tier == 1 ? 0.85f : 0.95f);
            seq.Tempo = tempo;
            seq.AddTrack(keyboard.Instrument, 2, notes);
            seq.Play();
            while (seq.IsPlaying) yield return null;
            yield return new WaitForSeconds(0.35f);
            keyboard.interactable = true;
            BeginFollowing();
        }

        private void BeginFollowing()
        {
            SetPhase(Phase.Following);
            wrongCount = 0;
            idleTime = 0f;
            ShowTarget();
            Progress?.Invoke(Index, notes.Count);
        }

        private void ShowTarget()
        {
            keyboard.ClearGuides();
            if (Index < notes.Count) keyboard.SetGuide(notes[Index].note, true);
        }

        private void OnKey(int note, int velocity)
        {
            if (State != Phase.Following) return;
            idleTime = 0f;
            if (note == notes[Index].note)
            {
                CorrectNote?.Invoke(note);
                Index++;
                wrongCount = 0;
                Progress?.Invoke(Index, notes.Count);
                if (Index >= notes.Count)
                {
                    running = StartCoroutine(Celebrate());
                    return;
                }
                ShowTarget();
            }
            else if (++wrongCount >= hintAfterWrong)
            {
                Hint();
            }
        }

        private void Hint()
        {
            wrongCount = 0;
            idleTime = 0f;
            int n = notes[Index].note;
            keyboard.Flash(n);
            double t = AudioSettings.dspTime + 0.05;
            Services.Engine?.NoteOnAt(keyboard.Instrument, n, 70, 2, t);
            Services.Engine?.NoteOffAt(keyboard.Instrument, n, 2, t + 0.5);
            HintShown?.Invoke(n);
        }

        private void Update()
        {
            if (State != Phase.Following) return;
            idleTime += Time.deltaTime;
            if (idleTime >= hintAfterSeconds) Hint();
        }

        private IEnumerator Celebrate()
        {
            SetPhase(Phase.Celebrating);
            keyboard.ClearGuides();
            UI.UISound.Sparkle();
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            if (p != null && p.Add(p.completedSongs, Lesson.Key))
            {
                p.stars++;
                Services.Save.Save();
            }
            Completed?.Invoke(Lesson);
            yield return new WaitForSeconds(1.4f);

            // Reward: the whole song with accompaniment, keys lighting along.
            seq.Clear();
            seq.Tempo = Lesson.song.tempo;
            seq.AddTrack(keyboard.Instrument, 2, FitToKeyboard(Lesson.song.Melody, keyboard));
            InstrumentDefinition acc = Services.Instruments.Get("UprightPiano");
            if (acc == keyboard.Instrument) acc = Services.Instruments.Get("GrandPiano");
            seq.AddTrack(acc, 3, SongLibrary.Accompaniment(Lesson.song), -0.25f);
            seq.Play();
            while (seq.IsPlaying) yield return null;
            SetPhase(Phase.Idle);
            keyboard.KeyDown -= OnKey;
            running = null;
        }
    }
}
