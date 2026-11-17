using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;

namespace LittleMusicWorld.Instruments
{
    /// <summary>
    /// Makes any Blender instrument model react to its sound. Naming convention from the Blender scripts:
    ///   Key_&lt;midi&gt;  piano keys (tilt down while held) · Pad_&lt;note&gt;  drum heads/bars/cymbals (squash or wobble)
    ///   String_&lt;i&gt;  strings (vibrate; i = lowest first) · Key_&lt;i&gt;  wind keys/valves/holes (press) · Slide · Bow.
    /// Mapping from notes to parts is by name for keys/pads, and by pitch order for strings and wind keys.
    /// </summary>
    public sealed class InstrumentModel3D : MonoBehaviour
    {
        [SerializeField] private InstrumentDefinition instrument;

        private sealed class Part
        {
            public Transform t;
            public Vector3 restPos, restScale;
            public Quaternion restRot;
            public float amount, wobble;
            public int held;
            public bool cymbal;
        }

        private readonly Dictionary<int, Part> byNote = new Dictionary<int, Part>();
        private readonly List<Part> strings = new List<Part>();
        private readonly List<Part> windKeys = new List<Part>();
        private Part slide, bow;
        private bool pianoKeys;
        private int lastWindIndex;

        public InstrumentDefinition Instrument { get => instrument; set => instrument = value; }

        private void Awake()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                Part p = null;
                if (n.StartsWith("Key_") || n.StartsWith("Pad_") || n.StartsWith("String_") || n == "Slide" || n == "Bow")
                    p = new Part { t = t, restPos = t.localPosition, restRot = t.localRotation, restScale = t.localScale };
                if (p == null) continue;
                if (n.StartsWith("String_")) strings.Add(p);
                else if (n == "Slide") slide = p;
                else if (n == "Bow") bow = p;
                else if (int.TryParse(n.Substring(4), out int v))
                {
                    if (n.StartsWith("Key_") && v < 20) windKeys.Add(p); // wind/valve keys are 0..n, piano keys are MIDI ≥ 21
                    else
                    {
                        byNote[v] = p;
                        pianoKeys |= n.StartsWith("Key_");
                        p.cymbal = n.StartsWith("Pad_") && (v == 42 || v == 46 || v == 49 || v == 51 || v == 53 || v == 57);
                    }
                }
            }
            strings.Sort((a, b) => string.CompareOrdinal(a.t.name.PadLeft(12), b.t.name.PadLeft(12)));
            windKeys.Sort((a, b) => string.CompareOrdinal(a.t.name.PadLeft(12), b.t.name.PadLeft(12)));
        }

        private void OnEnable()
        {
            if (Services.Engine == null) return;
            Services.Engine.NoteStarted += OnStart;
            Services.Engine.NoteStopped += OnStop;
        }

        private void OnDisable()
        {
            if (Services.Engine == null) return;
            Services.Engine.NoteStarted -= OnStart;
            Services.Engine.NoteStopped -= OnStop;
        }

        private void OnStart(InstrumentDefinition d, int note, int vel, int ch)
        {
            if (d != instrument) return;
            Hit(note, true);
        }

        private void OnStop(InstrumentDefinition d, int note, int ch)
        {
            if (d != instrument) return;
            Part p = Find(note);
            if (p != null) p.held = Mathf.Max(0, p.held - 1);
        }

        /// <summary>Animate as if the note were played (sequences call this from NoteVisual).</summary>
        public void Hit(int note, bool held = false)
        {
            Part p = Find(note);
            if (p == null) return;
            p.wobble = 1f;
            p.amount = 1f;
            if (held && !instrument.oneShot) p.held++;
            if (bow != null) bow.wobble = 1f;
        }

        private Part Find(int note)
        {
            if (byNote.TryGetValue(note, out Part p)) return p;
            if (pianoKeys && byNote.Count > 0)
            {
                int lo = int.MaxValue, hi = int.MinValue;
                foreach (int k in byNote.Keys) { lo = Mathf.Min(lo, k); hi = Mathf.Max(hi, k); }
                int n = note;
                while (n < lo) n += 12;
                while (n > hi) n -= 12;
                if (byNote.TryGetValue(n, out p)) return p;
            }
            if (strings.Count > 0 && instrument != null)
            {
                // nearest open string at or below the note (guitars) / step position (harp)
                int[] open = instrument.openStrings;
                if (open != null && open.Length == strings.Count)
                {
                    int best = 0;
                    for (int i = 0; i < open.Length; i++) if (open[i] <= note && open[i] >= open[best]) best = i;
                    return strings[best];
                }
                int[] play = instrument.playableNotes;
                int idx = play != null && play.Length > 0 ? System.Array.IndexOf(play, note) : -1;
                if (idx < 0) idx = Mathf.Abs(note) % strings.Count;
                return strings[Mathf.Clamp(idx * strings.Count / Mathf.Max(1, play?.Length ?? strings.Count), 0, strings.Count - 1)];
            }
            if (windKeys.Count > 0)
            {
                int[] play = instrument != null ? instrument.playableNotes : null;
                int idx = play != null ? System.Array.IndexOf(play, note) : -1;
                if (idx < 0) idx = (lastWindIndex + 1) % windKeys.Count;
                lastWindIndex = idx % windKeys.Count;
                if (slide != null) slide.amount = (float)idx / Mathf.Max(1, windKeys.Count - 1);
                return windKeys[lastWindIndex];
            }
            if (byNote.Count == 1) foreach (Part only in byNote.Values) return only;
            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;
            foreach (Part p in byNote.Values) Animate(p, dt, t, pianoKeys ? Kind.PianoKey : p.cymbal ? Kind.Cymbal : Kind.Drum);
            foreach (Part p in strings) Animate(p, dt, t, Kind.String);
            foreach (Part p in windKeys) Animate(p, dt, t, Kind.WindKey);
            if (slide != null)
                slide.t.localPosition = Vector3.Lerp(slide.t.localPosition, slide.restPos + new Vector3(0, 0, -0.28f * slide.amount), dt * 10f);
            if (bow != null)
            {
                bow.wobble = Mathf.MoveTowards(bow.wobble, 0f, dt * 1.2f);
                bow.t.localPosition = bow.restPos + new Vector3(Mathf.Sin(t * 5f) * 0.06f * bow.wobble, 0, 0);
            }
        }

        private enum Kind { PianoKey, Drum, Cymbal, String, WindKey }

        private void Animate(Part p, float dt, float t, Kind k)
        {
            bool down = p.held > 0;
            p.wobble = Mathf.MoveTowards(p.wobble, 0f, dt * (k == Kind.String ? 1.4f : k == Kind.Cymbal ? 1.1f : 6f));
            p.amount = Mathf.MoveTowards(p.amount, down ? 1f : 0f, dt * (down ? 30f : 9f));
            switch (k)
            {
                case Kind.PianoKey:
                    p.t.localRotation = p.restRot * Quaternion.Euler(-4.5f * Mathf.Max(p.amount, p.wobble * 0.8f), 0, 0);
                    break;
                case Kind.Drum:
                {
                    float s = p.wobble * Mathf.Sin(p.wobble * Mathf.PI * 3f) * 0.08f;
                    p.t.localScale = new Vector3(p.restScale.x * (1 + s * 0.5f), p.restScale.y * (1 - s), p.restScale.z * (1 + s * 0.5f));
                    break;
                }
                case Kind.Cymbal:
                    p.t.localRotation = p.restRot * Quaternion.Euler(Mathf.Sin(t * 22f) * 7f * p.wobble, 0, Mathf.Cos(t * 17f) * 5f * p.wobble);
                    break;
                case Kind.String:
                    p.t.localPosition = p.restPos + new Vector3(Mathf.Sin(t * 80f) * 0.004f * p.wobble, 0, 0);
                    break;
                case Kind.WindKey:
                    p.t.localPosition = p.restPos + new Vector3(0, -0.006f * Mathf.Max(p.amount, p.wobble), 0);
                    break;
            }
        }
    }
}
