using System;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>Parent area: volumes, language, quality and credits. Changes save immediately.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider master, music, voice;
        [SerializeField] private Button english, thai, quality, close, credits, resetProgress;
        [SerializeField] private TMP_Text qualityLabel, info;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button checkDevice;
        [SerializeField] private TMP_Text checkReport;
        [SerializeField] private GameObject checkPanel;

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
            if (checkDevice != null) checkDevice.onClick.AddListener(RunCheck);
        }

        /// <summary>Grown-up diagnostic: the audio/performance self-test, results shown on screen for a screenshot.</summary>
        private void RunCheck()
        {
            if (checkPanel == null) return;
            checkPanel.SetActive(true);
            checkReport.text = "Checking… (about 25 seconds, sounds will play)";
            var lines = new System.Text.StringBuilder();
            DeviceSelfTest.Run(line =>
            {
                if (line.StartsWith("load ")) return; // keep the screen short
                lines.AppendLine(line);
                checkReport.text = lines.ToString();
            }, report => checkReport.text = lines + "\nDone — please take a screenshot.");
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
