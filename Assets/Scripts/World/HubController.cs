using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LittleMusicWorld.World
{
    /// <summary>
    /// Hub navigation for small children: tap anywhere on the ground and Melo walks there; tap an area gate and Melo
    /// walks to it, announces the area and the area loads. Drags never trigger movement (so a child exploring the
    /// screen does not send Melo away), and UI touches are ignored.
    /// </summary>
    public sealed class HubController : MonoBehaviour
    {
        [SerializeField] private MascotController melo;
        [SerializeField] private Camera cam;
        [SerializeField] private float walkRadius = 15f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 9.5f, -10.5f);
        [SerializeField] private float cameraLag = 3.5f;
        [SerializeField] private Transform tapMarker;

        private Vector2 downPos;
        private float downTime;
        private bool pressing;
        private AreaPortal pending;
        private readonly System.Collections.Generic.Queue<Vector3> route = new System.Collections.Generic.Queue<Vector3>();
        private const float BandstandRadius = 3.3f;
        private Vector3 camVel;

        private void Start()
        {
            if (cam == null) cam = Camera.main;
            melo.Arrived += OnArrived;
            if (Services.Scenes != null && Services.Scenes.Previous != SceneFlow.MainMenu && !string.IsNullOrEmpty(Services.Scenes.Previous))
            {
                // Coming back from an area: start at that area's gate.
                foreach (AreaPortal p in FindObjectsByType<AreaPortal>(FindObjectsSortMode.None))
                    if (p.sceneName == Services.Scenes.Previous)
                    {
                        Vector3 a = p.ApproachPoint;
                        melo.transform.position = a + (Vector3.zero - a).normalized * 1.5f;
                        melo.FaceTowards(Vector3.zero, true);
                    }
            }
            cam.transform.position = melo.transform.position + cameraOffset;
            cam.transform.LookAt(melo.transform.position + Vector3.up * 0.8f);
            Services.Discover(Services.Save?.Data.progress.visitedAreas, SceneFlow.Hub);
            Invoke(nameof(Welcome), 0.6f);
        }

        private void Welcome()
        {
            bool first = Services.Save != null && !Services.Save.Data.progress.introSeen;
            melo.Wave();
            Guide.Say(first ? "hub_welcome" : "hub_tap_path");
            if (first)
            {
                Services.Save.Data.progress.introSeen = true;
                Services.Save.Save();
            }
        }

        private void Update()
        {
            Pointer p = Pointer.current;
            if (p != null && Services.Scenes != null && !Services.Scenes.Busy)
            {
                if (p.press.wasPressedThisFrame)
                {
                    pressing = !OverUI();
                    downPos = p.position.ReadValue();
                    downTime = Time.unscaledTime;
                }
                else if (p.press.wasReleasedThisFrame && pressing)
                {
                    pressing = false;
                    Vector2 up = p.position.ReadValue();
                    if ((up - downPos).magnitude < Screen.dpi * 0.15f + 12f && Time.unscaledTime - downTime < 0.8f) Tap(up);
                }
            }
            FollowCamera();
        }

        private static bool OverUI()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return es.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
            return es.IsPointerOverGameObject();
        }

        private void Tap(Vector2 screen)
        {
            Ray ray = cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                AreaPortal portal = hit.collider.GetComponentInParent<AreaPortal>();
                if (portal != null)
                {
                    GoTo(portal);
                    return;
                }
                Vector3 target = hit.point;
                Vector3 flat = new Vector3(target.x, 0f, target.z);
                if (flat.magnitude > walkRadius) flat = flat.normalized * walkRadius;
                pending = null;
                if (flat.magnitude < BandstandRadius) flat = flat.normalized * BandstandRadius;
                Route(flat);
                ShowMarker(flat);
                UI.UISound.Plink(79, 50);
            }
        }

        public void GoTo(AreaPortal portal)
        {
            if (!portal.open)
            {
                melo.Do("React");
                Guide.Say(portal.voiceKey);
                return;
            }
            pending = portal;
            Route(portal.ApproachPoint);
            ShowMarker(portal.ApproachPoint);
            UI.UISound.Plink(84, 70);
        }

        /// <summary>Walks around the bandstand: if the straight line crosses it, go via a point on its rim.</summary>
        private void Route(Vector3 to)
        {
            route.Clear();
            Vector3 from = melo.transform.position;
            Vector3 a = new Vector3(from.x, 0, from.z), b = new Vector3(to.x, 0, to.z);
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector3.Dot(-a, ab) / ab.sqrMagnitude) : 0f;
            Vector3 closest = a + ab * t;
            if (closest.magnitude < BandstandRadius && t > 0f && t < 1f)
            {
                Vector3 side = closest.sqrMagnitude > 0.01f ? closest.normalized : Vector3.Cross(ab, Vector3.up).normalized;
                Vector3 via = side * (BandstandRadius + 0.6f);
                route.Enqueue(new Vector3(via.x, to.y, via.z));
            }
            route.Enqueue(to);
            melo.WalkTo(route.Dequeue());
        }

        private void OnArrived()
        {
            if (route.Count > 0) { melo.WalkTo(route.Dequeue()); return; }
            if (pending == null) return;
            AreaPortal p = pending;
            pending = null;
            melo.FaceTowards(p.transform.position, true);
            float t = Guide.Say(p.voiceKey);
            melo.Celebrate();
            StartCoroutine(EnterAfter(p.sceneName, Mathf.Min(1.4f, t * 0.6f)));
        }

        private System.Collections.IEnumerator EnterAfter(string scene, float delay)
        {
            yield return new WaitForSeconds(delay);
            Services.Scenes.Load(scene);
        }

        private void ShowMarker(Vector3 at)
        {
            if (tapMarker == null) return;
            tapMarker.position = at + Vector3.up * 0.05f;
            tapMarker.gameObject.SetActive(true);
            Tween.Run(tapMarker, 0, 0.6f, k =>
            {
                tapMarker.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.3f, k);
                foreach (Renderer r in tapMarker.GetComponentsInChildren<Renderer>())
                    r.material.color = new Color(1f, 1f, 1f, 1f - k);
            }, Ease.OutCubic, () => tapMarker.gameObject.SetActive(false));
        }

        private void FollowCamera()
        {
            Vector3 focus = melo.transform.position;
            focus.x = Mathf.Clamp(focus.x, -10f, 10f);
            focus.z = Mathf.Clamp(focus.z, -8f, 12f);
            Vector3 want = focus + cameraOffset;
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, want, ref camVel, 1f / cameraLag);
            Quaternion look = Quaternion.LookRotation(focus + Vector3.up * 0.8f - cam.transform.position);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look, Time.deltaTime * cameraLag);
        }
    }
}
