using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;

namespace LittleMusicWorld.Characters
{
    /// <summary>
    /// Melo's dialog box: every spoken line also appears as text (portrait + words, typed in as Melo speaks) and stays
    /// long enough to read at a relaxed pace — for grown-ups and children who like to read. A new line replaces it.
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float extraSeconds = 3f;
        [SerializeField] private float charsPerSecond = 38f;

        private float hideAt, revealStart;
        private int total;
        private bool shown;

        private void OnEnable()
        {
            Guide.Said += Show;
            if (group != null) group.alpha = 0f;
            shown = false;
        }

        private void OnDisable() => Guide.Said -= Show;

        private void Show(string key, string text, float seconds)
        {
            if (label == null) return;
            label.text = text;
            label.maxVisibleCharacters = 0;
            label.ForceMeshUpdate();
            total = label.textInfo.characterCount;
            revealStart = Time.unscaledTime;
            float typing = total / charsPerSecond;
            float reading = 1.5f + total / 15f; // a slow reader's pace
            hideAt = revealStart + Mathf.Max(seconds, typing + reading) + extraSeconds;
            if (group != null) Tween.Fade(group, 1f, 0.15f);
            if (!shown) Tween.Run(this, 1, 0.35f, k => transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, k), Ease.OutBack);
            shown = true;
        }

        private void Update()
        {
            if (!shown || label == null) return;
            int visible = Mathf.Min(total, Mathf.CeilToInt((Time.unscaledTime - revealStart) * charsPerSecond));
            if (label.maxVisibleCharacters != visible) label.maxVisibleCharacters = visible;
            if (Time.unscaledTime > hideAt && !Guide.Holding)
            {
                shown = false;
                if (group != null) Tween.Fade(group, 0f, 0.3f);
            }
        }
    }
}
