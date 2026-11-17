using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace LittleMusicWorld.Instruments
{
    /// <summary>Something a child plays with fingers: keys, pads, strings, holes.</summary>
    public interface ITouchSurface
    {
        bool SurfaceActive { get; }
        int Priority { get; }
        /// <summary>Target index under the screen point, or -1.</summary>
        int HitTest(Vector2 screen);
        /// <summary>Whether a finger sliding onto another target should release/press (glissando, strumming).</summary>
        bool SlideBetweenTargets { get; }
        void Press(int target, int finger, Vector2 screen);
        void Release(int target, int finger);
        /// <summary>Finger moved while down (for bowing/strumming speed). target is the current target.</summary>
        void Drag(int target, int finger, Vector2 screen, Vector2 delta);
    }

    /// <summary>
    /// Multi-touch dispatcher. Each finger is tracked independently from touch-down to lift, so ten fingers can hold
    /// ten keys; sliding a finger across keys plays each one (when the surface allows it). Mouse acts as one finger
    /// in the Editor / desktop builds. Polls the Input System directly every frame (lowest latency, no UI raycasts).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class TouchRouter : MonoBehaviour
    {
        public static TouchRouter Instance { get; private set; }

        private readonly List<ITouchSurface> surfaces = new List<ITouchSurface>();
        private readonly Dictionary<int, (ITouchSurface surface, int target, Vector2 last)> fingers =
            new Dictionary<int, (ITouchSurface, int, Vector2)>();
        private const int MouseFinger = 1000;

        public int ActiveFingers => fingers.Count;

        public static TouchRouter Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Touch Router");
            return go.AddComponent<TouchRouter>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(ITouchSurface s)
        {
            if (!surfaces.Contains(s)) surfaces.Add(s);
            surfaces.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public void Unregister(ITouchSurface s)
        {
            surfaces.Remove(s);
            var drop = new List<int>();
            foreach (var kv in fingers) if (kv.Value.surface == s) drop.Add(kv.Key);
            foreach (int f in drop) { s.Release(fingers[f].target, f); fingers.Remove(f); }
        }

        public void ReleaseAll()
        {
            foreach (var kv in fingers) kv.Value.surface?.Release(kv.Value.target, kv.Key);
            fingers.Clear();
        }

        private void Update()
        {
            bool anyTouch = false;
            foreach (ETouch t in ETouch.activeTouches)
            {
                anyTouch = true;
                int id = t.finger.index;
                switch (t.phase)
                {
                    case TouchPhase.Began: Down(id, t.screenPosition); break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary: Move(id, t.screenPosition); break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled: Up(id); break;
                }
            }
            // Fingers that vanished without an Ended phase (app focus changes) are released.
            if (!anyTouch && fingers.Count > 0)
            {
                var stale = new List<int>();
                foreach (int k in fingers.Keys) if (k != MouseFinger) stale.Add(k);
                foreach (int k in stale) Up(k);
            }

            Mouse m = Mouse.current;
            if (m != null && !anyTouch)
            {
                Vector2 p = m.position.ReadValue();
                if (m.leftButton.wasPressedThisFrame) Down(MouseFinger, p);
                else if (m.leftButton.isPressed) Move(MouseFinger, p);
                else if (m.leftButton.wasReleasedThisFrame || fingers.ContainsKey(MouseFinger)) Up(MouseFinger);
            }
        }

        private void Down(int finger, Vector2 p)
        {
            if (fingers.ContainsKey(finger)) Up(finger);
            for (int i = 0; i < surfaces.Count; i++)
            {
                ITouchSurface s = surfaces[i];
                if (!s.SurfaceActive) continue;
                int target = s.HitTest(p);
                if (target < 0) continue;
                fingers[finger] = (s, target, p);
                s.Press(target, finger, p);
                return;
            }
        }

        private void Move(int finger, Vector2 p)
        {
            if (!fingers.TryGetValue(finger, out var f))
            {
                // A finger that started outside any surface may slide onto one (e.g. strumming in from the side).
                return;
            }
            Vector2 delta = p - f.last;
            if (f.surface.SlideBetweenTargets)
            {
                int target = f.surface.HitTest(p);
                if (target != f.target)
                {
                    // Keep tracking the finger through gaps (between strings/keys) so a strum or glissando
                    // continues onto the next target.
                    if (f.target >= 0) f.surface.Release(f.target, finger);
                    if (target >= 0) f.surface.Press(target, finger, p);
                    f.target = target;
                }
            }
            if (delta.sqrMagnitude > 0.01f) f.surface.Drag(f.target, finger, p, delta);
            f.last = p;
            fingers[finger] = f;
        }

        private void Up(int finger)
        {
            if (!fingers.TryGetValue(finger, out var f)) return;
            fingers.Remove(finger);
            f.surface.Release(f.target, finger);
        }
    }
}
