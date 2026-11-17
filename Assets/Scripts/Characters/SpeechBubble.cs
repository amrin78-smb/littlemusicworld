using System;
using System.Collections.Generic;
using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;

namespace LittleMusicWorld.Characters
{
    /// <summary>The speech bubble on a canvas: pops in when Melo speaks, hides when the line ends.</summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float extraSeconds = 1.2f;

        private float hideAt;

        private void OnEnable()
        {
            Guide.Said += Show;
            if (group != null) group.alpha = 0f;
        }

        private void OnDisable() => Guide.Said -= Show;

        private void Show(string key, string text, float seconds)
        {
            if (label != null) label.text = text;
            hideAt = Time.unscaledTime + seconds + extraSeconds;
            if (group != null) Tween.Fade(group, 1f, 0.15f);
            Tween.Run(this, 1, 0.35f, k => transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, k), Ease.OutBack);
        }

        private void Update()
        {
            if (group != null && group.alpha > 0.99f && Time.unscaledTime > hideAt) Tween.Fade(group, 0f, 0.25f);
        }
    }
}
