using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;

namespace LittleMusicWorld.Instruments
{
    /// <summary>
    /// Animates the 3D piano's keys (Blender pivots "Key_&lt;midi&gt;") in time with what is heard: keys go down on
    /// note-on and come back up on note-off, whether the note came from a finger or from a sequence (call
    /// <see cref="Pulse"/>). Notes outside the model's 49 keys fold into its range by octaves.
    /// </summary>
    public sealed class PianoModel3D : MonoBehaviour
    {
        [SerializeField] private float pressAngle = 4.5f;
        [SerializeField] private InstrumentDefinition[] listenTo;

        private sealed class KeyAnim
        {
            public Transform t;
            public Quaternion rest;
            public int held;
            public float amount, pulseUntil;
        }

        private readonly Dictionary<int, KeyAnim> keys = new Dictionary<int, KeyAnim>();
        private int lowest = int.MaxValue, highest = int.MinValue;

        private void Awake()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Key_") || !int.TryParse(t.name.Substring(4), out int note)) continue;
                keys[note] = new KeyAnim { t = t, rest = t.localRotation };
                lowest = Mathf.Min(lowest, note);
                highest = Mathf.Max(highest, note);
            }
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

        public void SetInstruments(params InstrumentDefinition[] defs) => listenTo = defs;

        private bool Listens(InstrumentDefinition d)
        {
            if (listenTo == null || listenTo.Length == 0) return d != null && d.family == InstrumentFamily.Keys;
            foreach (InstrumentDefinition x in listenTo) if (x == d) return true;
            return false;
        }

        private int Fold(int note)
        {
            if (keys.Count == 0) return note;
            while (note < lowest) note += 12;
            while (note > highest) note -= 12;
            return note;
        }

        private void OnStart(InstrumentDefinition d, int note, int vel, int ch)
        {
            if (!Listens(d) || !keys.TryGetValue(Fold(note), out KeyAnim k)) return;
            k.held++;
        }

        private void OnStop(InstrumentDefinition d, int note, int ch)
        {
            if (!Listens(d) || !keys.TryGetValue(Fold(note), out KeyAnim k)) return;
            k.held = Mathf.Max(0, k.held - 1);
        }

        /// <summary>Sequenced notes (no finger): press for `seconds`.</summary>
        public void Pulse(int note, float seconds = 0.25f)
        {
            if (keys.TryGetValue(Fold(note), out KeyAnim k)) k.pulseUntil = Time.time + seconds;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (KeyAnim k in keys.Values)
            {
                bool down = k.held > 0 || Time.time < k.pulseUntil;
                k.amount = Mathf.MoveTowards(k.amount, down ? 1f : 0f, dt * (down ? 28f : 10f));
                k.t.localRotation = k.rest * Quaternion.Euler(-pressAngle * k.amount, 0f, 0f);
            }
        }
    }
}
