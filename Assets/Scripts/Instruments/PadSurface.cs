using System;
using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Instruments
{
    /// <summary>
    /// General multi-touch instrument interface: a set of pads, each mapped to a note.
    ///  - Drums/percussion: tap pads (one-shot), velocity from distance to the pad centre (centre = louder).
    ///  - Mallets (xylophone): bars; sliding across bars plays each (glissando).
    ///  - Strings (guitar, ukulele, banjo, harp): strings as long pads; swiping across them strums each string once,
    ///    tapping plucks. With a chord selected, strings retune to the chord (child-friendly guitar).
    ///  - Bowed strings / winds: holding a pad sustains the note (looped samples) until the finger lifts.
    /// Every pad gives visual feedback (glow, squash, string wobble) the instant it sounds.
    /// </summary>
    public sealed class PadSurface : MonoBehaviour, ITouchSurface, ISongSurface
    {
        public enum Mode { Tap, Hold, Strum }

        [Serializable]
        public sealed class Pad
        {
            public RectTransform rect;
            public Image image;
            public Image glow;
            public int note;
            public Color colour = Color.white;
            [NonSerialized] public int fingers;
            [NonSerialized] public float lit, wobble;
            [NonSerialized] public Vector2 rest;
            [NonSerialized] public bool guide;
        }

        [SerializeField] private InstrumentDefinition instrument;
        [SerializeField] private Mode mode = Mode.Tap;
        [SerializeField] private int channel = MusicalAudioEngine.FreePlayChannel;
        [SerializeField] private bool vertical; // strings drawn vertically (harp) vs horizontally (guitar)
        public List<Pad> pads = new List<Pad>();

        public event Action<int, int> PadDown;   // pad index, velocity
        public event Action<int> PadUp;
        public bool interactable = true;

        public bool Vertical { get => vertical; set => vertical = value; }

        // ---- ISongSurface (melodies on wind holes, bars, harp strings)
        public event Action<int, int> NoteDown;
        public event Action<int> Flashed;
        public bool Interactable { get => interactable; set => interactable = value; }
        public bool Has(int note) => IndexOfNote(note) >= 0;
        public int LowNote { get { int m = int.MaxValue; foreach (Pad p in pads) m = Mathf.Min(m, p.note); return m; } }
        public int HighNote { get { int m = int.MinValue; foreach (Pad p in pads) m = Mathf.Max(m, p.note); return m; } }
        void ISongSurface.SetGuide(int note, bool on) => SetGuide(IndexOfNote(note), on);
        void ISongSurface.Flash(int note) { Flash(IndexOfNote(note)); Flashed?.Invoke(note); }

        public InstrumentDefinition Instrument
        {
            get => instrument;
            set { ReleaseAll(); instrument = value; if (value != null) Services.Engine?.Load(value); }
        }
        public Mode PlayMode { get => mode; set { ReleaseAll(); mode = value; } }

        public bool SurfaceActive => isActiveAndEnabled && instrument != null && interactable;
        public int Priority => 10;
        public bool SlideBetweenTargets => mode != Mode.Hold;

        private readonly Dictionary<int, int> fingerPad = new Dictionary<int, int>();
        private Camera uiCamera;

        private void Start()
        {
            foreach (Pad p in pads) if (p.rect != null) p.rest = p.rect.anchoredPosition;
            if (instrument != null) Services.Engine?.Load(instrument);
            TouchRouter.Ensure().Register(this);
            Canvas c = GetComponentInParent<Canvas>();
            uiCamera = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
        }

        private void OnDisable() => ReleaseAll();
        private void OnDestroy() { if (TouchRouter.Instance != null) TouchRouter.Instance.Unregister(this); }

        /// <summary>Retunes pads (e.g. strings to a chord). Notes stay in pad order.</summary>
        public void SetNotes(IList<int> notes)
        {
            for (int i = 0; i < pads.Count && i < notes.Count; i++) pads[i].note = notes[i];
        }

        public int HitTest(Vector2 screen)
        {
            // Smallest pad first, so overlapping decorative areas never steal a touch.
            int best = -1;
            float bestArea = float.MaxValue;
            for (int i = 0; i < pads.Count; i++)
            {
                RectTransform r = pads[i].rect;
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(r, screen, uiCamera)) continue;
                float area = r.rect.width * r.rect.height;
                if (area < bestArea) { bestArea = area; best = i; }
            }
            return best;
        }

        public void Press(int target, int finger, Vector2 screen)
        {
            if (target < 0 || target >= pads.Count) return;
            Pad p = pads[target];
            int velocity = 100;
            if (mode == Mode.Tap)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(p.rect, screen, uiCamera, out Vector2 local);
                Rect r = p.rect.rect;
                float d = Mathf.Clamp01(new Vector2(local.x / (r.width * 0.5f), local.y / (r.height * 0.5f)).magnitude);
                velocity = Mathf.RoundToInt(Mathf.Lerp(120f, 80f, d));
            }
            fingerPad[finger] = target;
            p.fingers++;
            p.lit = 1f;
            p.wobble = 1f;
            Services.Engine?.NoteOn(instrument, p.note, velocity, channel);
            PadDown?.Invoke(target, velocity);
            NoteDown?.Invoke(p.note, velocity);
        }

        public void Release(int target, int finger)
        {
            if (!fingerPad.TryGetValue(finger, out int idx)) return;
            fingerPad.Remove(finger);
            Pad p = pads[idx];
            p.fingers = Mathf.Max(0, p.fingers - 1);
            if (p.fingers == 0)
            {
                // Plucked/strummed strings and drums ring on; held notes (bow, wind) stop when the finger lifts.
                if (mode == Mode.Hold) Services.Engine?.NoteOff(instrument, p.note, channel);
                PadUp?.Invoke(idx);
            }
        }

        public void Drag(int target, int finger, Vector2 screen, Vector2 delta)
        {
            if (target >= 0 && target < pads.Count && mode == Mode.Hold) pads[target].wobble = Mathf.Max(pads[target].wobble, 0.4f);
        }

        public void ReleaseAll()
        {
            foreach (var kv in fingerPad)
                if (mode == Mode.Hold && instrument != null) Services.Engine?.NoteOff(instrument, pads[kv.Value].note, channel);
            fingerPad.Clear();
            foreach (Pad p in pads) p.fingers = 0;
        }

        /// <summary>Visual flash for a pad played by a sequence or demonstration.</summary>
        public void Flash(int index)
        {
            if (index < 0 || index >= pads.Count) return;
            pads[index].lit = 1f;
            pads[index].wobble = 1f;
        }

        public int IndexOfNote(int note) => pads.FindIndex(p => p.note == note);

        public void SetGuide(int index, bool on)
        {
            if (index >= 0 && index < pads.Count) pads[index].guide = on;
        }

        public void ClearGuides() { foreach (Pad p in pads) p.guide = false; }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
            float t = Time.unscaledTime;
            foreach (Pad p in pads)
            {
                if (p.rect == null) continue;
                bool held = p.fingers > 0;
                p.lit = held && mode == Mode.Hold ? 1f : Mathf.MoveTowards(p.lit, 0f, dt * 3.5f);
                p.wobble = Mathf.MoveTowards(p.wobble, 0f, dt * (mode == Mode.Strum ? 1.6f : 6f));
                if (mode == Mode.Strum)
                {
                    // vibrating string: sideways wobble that decays
                    float w = Mathf.Sin(t * 70f) * 7f * p.wobble;
                    p.rect.anchoredPosition = p.rest + (vertical ? new Vector2(w, 0) : new Vector2(0, w));
                }
                else
                {
                    float sq = (held ? 0.08f : 0f) + 0.1f * p.wobble * Mathf.Sin(p.wobble * Mathf.PI);
                    p.rect.localScale = new Vector3(1f + sq * 0.6f, 1f - sq, 1f);
                }
                if (p.image != null) p.image.color = Color.Lerp(p.colour, Color.white, p.lit * 0.45f);
                if (p.glow != null)
                {
                    Color g = p.colour;
                    g.a = p.guide ? pulse : p.lit * 0.85f;
                    p.glow.color = g;
                }
            }
        }
    }
}
