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

    /// <summary>Parent area: volumes, language, quality and credits. Changes save immediately.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider master, music, voice;
        [SerializeField] private Button english, thai, quality, close, credits, resetProgress;
        [SerializeField] private TMP_Text qualityLabel, info;
        [SerializeField] private GameObject creditsPanel;

        private SaveSystem.SettingsData S => Services.Save.Data.settings;

        private void OnEnable()
        {
            if (Services.Save == null) return;
            master.SetValueWithoutNotify(S.masterVolume);
            music.SetValueWithoutNotify(S.musicVolume);
            voice.SetValueWithoutNotify(S.voiceVolume);
            RefreshQuality();
            if (info != null)
                info.text = $"v{Application.version}  ·  {SystemInfo.deviceModel}  ·  {(IntPtr.Size == 8 ? "64" : "32")}-bit  ·  tier {QualityTier.Current}";
        }

        private void Start()
        {
            master.onValueChanged.AddListener(v => { S.masterVolume = v; Services.ApplyVolumes(); });
            music.onValueChanged.AddListener(v => { S.musicVolume = v; Services.ApplyVolumes(); });
            voice.onValueChanged.AddListener(v => S.voiceVolume = v);
            english.onClick.AddListener(() => SetLanguage("en"));
            thai.onClick.AddListener(() => SetLanguage("th"));
            quality.onClick.AddListener(CycleQuality);
            close.onClick.AddListener(() => { Services.Save.Save(); gameObject.SetActive(false); });
            if (credits != null) credits.onClick.AddListener(() => creditsPanel.SetActive(!creditsPanel.activeSelf));
            if (resetProgress != null) resetProgress.onClick.AddListener(() => Services.Save.ResetProgress(false));
        }

        private void SetLanguage(string lang)
        {
            S.language = lang;
            Services.Save.Save();
            Guide.Say("hello");
        }

        private void CycleQuality()
        {
            S.qualityTier = S.qualityTier >= 2 ? -1 : S.qualityTier + 1;
            QualityTier.Apply(S.qualityTier >= 0 ? (Tier)S.qualityTier : QualityTier.Detect());
            Services.Save.Save();
            RefreshQuality();
        }

        private void RefreshQuality()
        {
            if (qualityLabel != null) qualityLabel.text = S.qualityTier < 0 ? $"Auto ({QualityTier.Current})" : ((Tier)S.qualityTier).ToString();
        }
    }
}
