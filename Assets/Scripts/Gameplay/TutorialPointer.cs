using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// The tutorial's "tap here" marker: a pulsing golden ring with a bouncing arrow above it, following a 3D object
    /// (projected through the main camera) or a UI element. Never blocks touches.
    /// </summary>
    public sealed class TutorialPointer : MonoBehaviour
    {
        private RectTransform root, ring, arrow;
        private Image ringImg;
        private RectTransform canvasRect;
        private Transform worldTarget;
        private Vector3 worldPoint;
        private bool usePoint;
        private RectTransform uiTarget;

        public static TutorialPointer Create(Component anyUnderCanvas)
        {
            Canvas c = anyUnderCanvas != null ? anyUnderCanvas.GetComponentInParent<Canvas>(true) : Object.FindAnyObjectByType<Canvas>();
            if (c == null) return null;
            c = c.rootCanvas;
            var go = new GameObject("Tutorial Pointer", typeof(RectTransform));
            go.transform.SetParent(c.transform, false);
            var p = go.AddComponent<TutorialPointer>();
            p.Build((RectTransform)c.transform);
            go.SetActive(false);
            return p;
        }

        private void Build(RectTransform canvas)
        {
            canvasRect = canvas;
            root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            UISkin skin = UISkin.Instance;
            Color gold = new Color(1f, 0.82f, 0.25f);
            ringImg = Make("Ring", skin.ring, gold, new Vector2(190, 190));
            ring = ringImg.rectTransform;
            Image glow = Make("Glow", skin.glow, new Color(1f, 0.9f, 0.5f, 0.55f), new Vector2(240, 240));
            glow.transform.SetAsFirstSibling();
            arrow = Make("Arrow", skin.iconDown, gold, new Vector2(130, 130)).rectTransform;
            Image shade = Make("ArrowShade", skin.iconDown, new Color(0.18f, 0.16f, 0.29f, 0.35f), new Vector2(130, 130));
            shade.transform.SetParent(arrow, false);
            shade.rectTransform.anchoredPosition = new Vector2(4, -6);
            shade.transform.SetAsFirstSibling();
        }

        private Image Make(string name, Sprite s, Color c, Vector2 size)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(transform, false);
            var img = g.GetComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        public void PointAt(Transform world)
        {
            worldTarget = world;
            usePoint = false;
            uiTarget = null;
            Show();
        }

        public void PointAt(Vector3 world)
        {
            worldPoint = world;
            usePoint = true;
            worldTarget = null;
            uiTarget = null;
            Show();
        }

        public void PointAt(RectTransform ui)
        {
            uiTarget = ui;
            worldTarget = null;
            usePoint = false;
            Show();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Show()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            LateUpdate();
        }

        private void LateUpdate()
        {
            Vector2 screen;
            bool behind = false;
            if (uiTarget != null) screen = RectTransformUtility.WorldToScreenPoint(null, uiTarget.position);
            else
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                Vector3 w = usePoint ? worldPoint : worldTarget != null ? worldTarget.position : Vector3.zero;
                Vector3 sp = cam.WorldToScreenPoint(w);
                behind = sp.z < 0f;
                screen = behind ? new Vector2(Screen.width - sp.x, Screen.height - sp.y) : (Vector2)sp;
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local)) return;

            if (uiTarget != null)
            {
                // Buttons are always on screen: ring on the button, arrow above it, or below (pointing up) near the top.
                root.anchoredPosition = local;
                float tt = Time.unscaledTime;
                float pl = 0.5f + 0.5f * Mathf.Sin(tt * 5f);
                ring.localScale = Vector3.one * (0.85f + 0.25f * pl);
                ringImg.color = new Color(1f, 0.82f, 0.25f, 0.6f + 0.4f * (1f - pl));
                bool below = local.y > canvasRect.rect.height * 0.5f - 330f;
                float b = 150f + 26f * Mathf.Abs(Mathf.Sin(tt * 4f));
                arrow.anchoredPosition = new Vector2(0, below ? -b : b);
                arrow.localRotation = Quaternion.Euler(0, 0, below ? 180f : 0f);
                return;
            }

            // Off screen (e.g. a gate behind the camera's view): sit at the edge and point the arrow towards it.
            Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(170f, 170f);
            Vector2 clamped = new Vector2(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y));
            bool edge = behind || (clamped - local).sqrMagnitude > 1f;
            if (behind) clamped = local.sqrMagnitude > 1f ? Vector2.Scale(local.normalized, half * 1.5f) : new Vector2(0, -half.y);
            clamped = new Vector2(Mathf.Clamp(clamped.x, -half.x, half.x), Mathf.Clamp(clamped.y, -half.y, half.y));
            root.anchoredPosition = clamped;

            float t = Time.unscaledTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
            ring.localScale = Vector3.one * (edge ? 0.6f : 0.85f + 0.25f * pulse);
            ringImg.color = new Color(1f, 0.82f, 0.25f, 0.6f + 0.4f * (1f - pulse));
            float bounce = 26f * Mathf.Abs(Mathf.Sin(t * 4f));
            if (edge)
            {
                Vector2 dir = (local - clamped).sqrMagnitude > 1f ? (local - clamped).normalized : Vector2.down;
                if (behind) dir = clamped.normalized;
                arrow.anchoredPosition = dir * (60f + bounce);
                arrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
            }
            else
            {
                arrow.anchoredPosition = new Vector2(0, 150 + bounce);
                arrow.localRotation = Quaternion.identity;
            }
        }
    }
}
