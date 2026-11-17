using System;
using LittleMusicWorld.Audio;
using LittleMusicWorld.SaveSystem;
using UnityEngine;

namespace LittleMusicWorld.Core
{
    /// <summary>
    /// Game-wide services, created once before the first scene loads (so any scene can be played directly in the
    /// Editor). Kept deliberately small: audio engine + music sequencer, save store, scene flow.
    /// </summary>
    public static class Services
    {
        public static MusicalAudioEngine Engine { get; private set; }
        public static Sequencer Music { get; private set; }     // background music
        public static SaveStore Save { get; private set; }
        public static SceneFlow Scenes { get; private set; }
        public static InstrumentLibrary Instruments => InstrumentLibrary.Instance;
        public static GameObject Root { get; private set; }

        public static event Action<string> Discovery; // e.g. instrument id discovered for the first time

        public const int MusicChannel = 15;
        public const int UiChannel = 14;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Root != null) return;
            Root = new GameObject("LMW Services");
            UnityEngine.Object.DontDestroyOnLoad(Root);

            Save = new SaveStore(System.IO.Path.Combine(Application.persistentDataPath, "lmw"));
            SettingsData s = Save.Data.settings;
            QualityTier.Apply(s.qualityTier >= 0 ? (Tier)s.qualityTier : QualityTier.Detect());

            ConfigureAudioOutput();
            var audioGo = new GameObject("Musical Audio Engine", typeof(AudioSource));
            audioGo.transform.SetParent(Root.transform);
            Engine = audioGo.AddComponent<MusicalAudioEngine>();
            Engine.SetMaxVoices(QualityTier.MaxVoices);
            Music = audioGo.AddComponent<Sequencer>();
            ApplyVolumes();

            Scenes = Root.AddComponent<SceneFlow>();
            Root.AddComponent<AppLifecycle>();
            Debug.Log($"[LMW] Services ready. Save: {Save.LastOutcome} at {Save.Root}. Audio {AudioSettings.outputSampleRate} Hz, " +
                      $"dsp buffer {DspBuffer()}.");
        }

        /// <summary>
        /// 48 kHz output with a small DSP buffer for instant response. Budget phones get a slightly larger buffer
        /// (still ~20 ms) so a busy frame can never starve the mixer. Must run before any AudioSource plays.
        /// </summary>
        private static void ConfigureAudioOutput()
        {
            AudioConfiguration cfg = AudioSettings.GetConfiguration();
            int wantBuffer = QualityTier.Current == Tier.Low ? 512 : 256;
            if (cfg.sampleRate != 48000 || cfg.dspBufferSize != wantBuffer)
            {
                cfg.sampleRate = 48000;
                cfg.dspBufferSize = wantBuffer;
                if (!AudioSettings.Reset(cfg)) Debug.LogWarning("[LMW] Audio reset refused; keeping device defaults.");
            }
        }

        public static string DspBuffer()
        {
            AudioSettings.GetDSPBufferSize(out int len, out int num);
            return $"{len}x{num}";
        }

        public static void ApplyVolumes()
        {
            if (Engine == null || Save == null) return;
            SettingsData s = Save.Data.settings;
            Engine.MasterVolume = s.masterVolume;
            Engine.SetChannelVolume(MusicChannel, s.musicVolume * 0.55f);
        }

        /// <summary>Records a discovery (instrument, area, musician…) once and announces it.</summary>
        public static bool Discover(System.Collections.Generic.List<string> list, string id)
        {
            if (Save == null || !Save.Data.progress.Add(list, id)) return false;
            Save.Save();
            Discovery?.Invoke(id);
            return true;
        }
    }

    /// <summary>Saves when the app is backgrounded and silences hanging notes.</summary>
    public sealed class AppLifecycle : MonoBehaviour
    {
        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Services.Engine?.StopAll();
            Services.Save?.Save();
        }

        private void OnApplicationQuit() => Services.Save?.Save();
    }
}
