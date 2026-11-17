using System;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>
    /// Grown-ups only: the gear must be held for 2 seconds (a ring fills) before the parent area opens, so a child
    /// tapping around never lands in settings.
    /// </summary>
    public sealed class ParentGate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image ring;
        [SerializeField] private float holdSeconds = 2f;
        [SerializeField] private GameObject panel;

        private float held = -1f;

        public void OnPointerDown(PointerEventData e) => held = 0f;
        public void OnPointerUp(PointerEventData e) => Cancel();
        public void OnPointerExit(PointerEventData e) => Cancel();

        private void Cancel()
        {
            held = -1f;
            if (ring != null) ring.fillAmount = 0f;
        }

        private void Update()
        {
            if (held < 0f) return;
            held += Time.unscaledDeltaTime;
            if (ring != null) ring.fillAmount = held / holdSeconds;
            if (held >= holdSeconds)
            {
                Cancel();
                if (panel != null) panel.SetActive(true);
            }
        }
    }
}
