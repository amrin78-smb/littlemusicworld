using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>Fires only after being held (default 1.2 s) — for destructive actions a child might tap by accident.</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float seconds = 1.2f;
        public event Action Held;

        private float t = -1f;
        private Image image;

        private void Awake() => image = GetComponent<Image>();
        public void OnPointerDown(PointerEventData e) => t = 0f;
        public void OnPointerUp(PointerEventData e) => Cancel();
        public void OnPointerExit(PointerEventData e) => Cancel();

        private void Cancel()
        {
            t = -1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (t < 0f) return;
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.one * (1f + 0.4f * Mathf.Clamp01(t / seconds));
            if (image != null) image.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), t / seconds);
            if (t < seconds) return;
            Cancel();
            Held?.Invoke();
        }
    }
}
