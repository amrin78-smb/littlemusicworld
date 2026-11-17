using System;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>Main menu: Melo waves and invites the child to press the big Play button.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button play;
        [SerializeField] private MascotController melo;
        [SerializeField] private RectTransform logo;

        private void Start()
        {
            play.onClick.AddListener(() => { Guide.Silence(); Services.Scenes.Load(SceneFlow.Hub); });
            Invoke(nameof(Greet), 0.8f);
            if (logo != null) Tween.Run(logo, 0, 0.9f, k => logo.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k), Ease.OutElastic);
        }

        private void Greet()
        {
            if (melo != null) melo.Wave();
            float t = Guide.Say("hello");
            Invoke(nameof(Prompt), t + 2.5f);
        }

        private void Prompt() => Guide.Say("menu_play");

        private void Update()
        {
            if (play == null) return;
            float s = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3f);
            play.transform.localScale = new Vector3(s, s, 1f);
        }
    }

}
