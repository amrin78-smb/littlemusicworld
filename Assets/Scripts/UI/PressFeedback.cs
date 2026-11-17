using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>
    /// Every button squashes when touched and plays a soft musical "plink" (a glockenspiel-like xylophone note), so
    /// the whole interface sounds like part of the musical world. Disabled buttons do nothing.
    /// </summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private int note = 84;
        [SerializeField] private bool sound = true;

        private Selectable selectable;
        private Vector3 rest = Vector3.one;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            rest = transform.localScale;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            Tween.Run(this, 0, 0.08f, k => transform.localScale = rest * Mathf.Lerp(1f, 0.9f, k), Ease.OutCubic);
            if (sound) UISound.Plink(note);
        }

        public void OnPointerUp(PointerEventData e)
        {
            Tween.Run(this, 0, 0.3f, k => transform.localScale = rest * Mathf.LerpUnclamped(0.9f, 1f, k), Ease.OutElastic);
        }
    }

    /// <summary>UI sounds played through the sampler so they share the game's instruments and loudness.</summary>
    public static class UISound
    {
        private static InstrumentDefinition s_Bell;

        public static void Plink(int note = 84, int velocity = 70)
        {
            if (Services.Engine == null) return;
            if (s_Bell == null) s_Bell = Services.Instruments?.Get("Xylophone");
            if (s_Bell == null) return;
            Services.Engine.NoteOn(s_Bell, note, velocity, Services.UiChannel);
        }

        public static void Sparkle()
        {
            if (Services.Engine == null) return;
            if (s_Bell == null) s_Bell = Services.Instruments?.Get("Xylophone");
            if (s_Bell == null) return;
            double t = AudioSettings.dspTime + 0.03;
            int[] arp = { 72, 76, 79, 84 };
            for (int i = 0; i < arp.Length; i++) Services.Engine.NoteOnAt(s_Bell, arp[i], 80, Services.UiChannel, t + i * 0.07);
        }
    }

}
