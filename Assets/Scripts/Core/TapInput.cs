using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace LittleMusicWorld.Core
{
    /// <summary>
    /// World taps for the hub and areas: a short touch that barely moved and did not start on UI. Uses EnhancedTouch
    /// on touchscreens, which keeps a touch that starts and ends inside one frame (a very quick tap) — polling the
    /// pointer's press state loses those. Mouse in the editor.
    /// </summary>
    public static class TapInput
    {
        private struct Down { public Vector2 pos; public float time; public bool overUI; }

        private static readonly Dictionary<int, Down> s_Downs = new Dictionary<int, Down>();
        private static readonly List<Vector2> s_Taps = new List<Vector2>();
        private static readonly List<RaycastResult> s_Hits = new List<RaycastResult>();
        private static int s_Frame = -1;
        private static Down s_Mouse;

        public static float MaxMove => (Screen.dpi > 0 ? Screen.dpi : 400f) * 0.15f + 12f;
        public const float MaxSeconds = 0.8f;

        /// <summary>True when a tap finished this frame; position in screen pixels.</summary>
        public static bool Poll(out Vector2 at)
        {
            Collect();
            at = s_Taps.Count > 0 ? s_Taps[0] : default;
            return s_Taps.Count > 0;
        }

        private static void Collect()
        {
            if (s_Frame == Time.frameCount) return;
            s_Frame = Time.frameCount;
            s_Taps.Clear();
            if (Touchscreen.current != null)
            {
                if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
                foreach (ETouch t in ETouch.activeTouches)
                {
                    int id = t.touchId;
                    switch (t.phase)
                    {
                        case UnityEngine.InputSystem.TouchPhase.Began:
                            s_Downs[id] = new Down { pos = t.screenPosition, time = Time.unscaledTime, overUI = OverUI(t.screenPosition) };
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Ended:
                            if (s_Downs.TryGetValue(id, out Down d)) Finish(d, t.screenPosition);
                            s_Downs.Remove(id);
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Canceled:
                            s_Downs.Remove(id);
                            break;
                    }
                }
                return;
            }
            Mouse m = Mouse.current;
            if (m == null) return;
            Vector2 p = m.position.ReadValue();
            if (m.leftButton.wasPressedThisFrame) s_Mouse = new Down { pos = p, time = Time.unscaledTime, overUI = OverUI(p) };
            if (m.leftButton.wasReleasedThisFrame) Finish(s_Mouse, p);
        }

        private static void Finish(Down d, Vector2 up)
        {
            if (!d.overUI && (up - d.pos).magnitude < MaxMove && Time.unscaledTime - d.time < MaxSeconds) s_Taps.Add(up);
        }

        private static bool OverUI(Vector2 screen)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            s_Hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen }, s_Hits);
            return s_Hits.Count > 0;
        }
    }
}
