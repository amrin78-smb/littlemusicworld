using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using LittleMusicWorld.Instruments;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// Piano Paradise controller: free play on the multi-touch keyboard, switching between the grand piano, upright
    /// and digital keyboard, sliding the keyboard up/down an octave, and hearing a song demonstrated (the sequencer
    /// lights the keys as it plays). The guided Follow-the-Melody mode lives in <see cref="MelodyGuide"/>.
    /// </summary>
    public sealed class PianoActivity : MonoBehaviour
    {
        [SerializeField] private KeyboardSurface keyboard;
        [SerializeField] private string[] instrumentIds = { "GrandPiano", "UprightPiano", "DigitalKeyboard" };
        [SerializeField] private Button[] instrumentButtons;
        [SerializeField] private Button octaveDown, octaveUp, demoButton;
        [SerializeField] private Text statsLabel;
        [SerializeField] private int whiteKeys = 15;

        public KeyboardSurface Keyboard => keyboard;
        public InstrumentDefinition Current { get; private set; }
        public event System.Action<InstrumentDefinition> InstrumentChanged;

        private int lowNote = 60;
        private float fpsTimer;
        private int fpsFrames;
        private float fps;
        private Sequencer demo;

        private void Start()
        {
            demo = gameObject.AddComponent<Sequencer>();
            demo.NoteVisual += (track, ev) => keyboard.Flash(ev.note);
            for (int i = 0; i < instrumentButtons.Length && i < instrumentIds.Length; i++)
            {
                int k = i;
                instrumentButtons[i].onClick.AddListener(() => Select(instrumentIds[k]));
            }
            if (octaveDown != null) octaveDown.onClick.AddListener(() => Shift(-12));
            if (octaveUp != null) octaveUp.onClick.AddListener(() => Shift(12));
            if (demoButton != null) demoButton.onClick.AddListener(ToggleDemo);
            // Decode all three pianos up front (behind the loading curtain) so switching is instant.
            foreach (string id in instrumentIds) Services.Engine?.Load(Services.Instruments?.Get(id));
            Select(instrumentIds[0]);
            Services.Discover(Services.Save?.Data.progress.visitedAreas, SceneFlow.PianoParadise);
        }

        public void Select(string id)
        {
            InstrumentDefinition def = Services.Instruments?.Get(id);
            if (def == null) { Debug.LogError("[LMW] instrument missing: " + id); return; }
            Current = def;
            keyboard.Configure(def, lowNote, whiteKeys);
            for (int i = 0; i < instrumentButtons.Length && i < instrumentIds.Length; i++)
                Tween.Punch(instrumentButtons[i].transform, instrumentIds[i] == id ? 0.12f : 0f);
            Services.Discover(Services.Save?.Data.progress.discoveredInstruments, id);
            InstrumentChanged?.Invoke(def);
        }

        private void Shift(int semis)
        {
            int next = Mathf.Clamp(lowNote + semis, 36, 72);
            if (next == lowNote) return;
            lowNote = next;
            keyboard.Configure(Current, lowNote, whiteKeys);
        }

        private void ToggleDemo()
        {
            if (demo.IsPlaying) { demo.Stop(); return; }
            SongLibrary.Song song = SongLibrary.Get("twinkle");
            demo.Clear();
            demo.Tempo = song.tempo;
            var melody = new List<NoteEvent>();
            foreach (NoteEvent e in song.Melody)
            {
                NoteEvent m = e;
                while (m.note < lowNote) m.note += 12;
                while (m.note > keyboard.HighNote) m.note -= 12;
                melody.Add(m);
            }
            demo.AddTrack(Current, 1, melody);
            demo.AddTrack(Services.Instruments.Get("UprightPiano"), 2, SongLibrary.Accompaniment(song), -0.2f);
            demo.Play();
        }

        private void Update()
        {
            fpsFrames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsFrames / fpsTimer;
                fpsFrames = 0;
                fpsTimer = 0f;
                if (statsLabel != null && Services.Engine != null)
                    statsLabel.text = $"{fps:0} fps  voices {Services.Engine.ActiveVoices}  fingers {TouchRouter.Instance?.ActiveFingers ?? 0}  " +
                                      $"dsp {Services.DspBuffer()} @ {Services.Engine.OutputRate}  {QualityTier.Current}";
            }
        }
    }
}
