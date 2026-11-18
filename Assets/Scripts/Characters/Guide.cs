using System;
using System.Collections.Generic;
using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;

namespace LittleMusicWorld.Characters
{
    /// <summary>
    /// Melo's voice: every instruction is spoken (offline TTS clip) AND shown in a speech bubble, from one source
    /// (Tools/voice/voice_lines.json → Resources/Voice). Children who cannot read still hear it; parents see it.
    /// Lines never overlap: a new line replaces the current one.
    /// </summary>
    public static class Guide
    {
        [Serializable] private sealed class Line { public string key, en, th; }
        [Serializable] private sealed class LineFile { public Line[] lines; }

        private static Dictionary<string, Line> s_Lines;
        private static AudioSource s_Voice;
        private static readonly Dictionary<string, AudioClip> s_Clips = new Dictionary<string, AudioClip>();

        public static event Action<string, string, float> Said; // key, text, seconds
        public static string Language => Services.Save?.Data.settings.language ?? "en";

        public static string Text(string key)
        {
            Load();
            if (!s_Lines.TryGetValue(key, out Line l)) return key;
            return Language == "th" && !string.IsNullOrEmpty(l.th) ? l.th : l.en;
        }

        /// <summary>True while the last line is a tutorial instruction that stays on screen until the next line.</summary>
        public static bool Holding { get; private set; }

        /// <summary>Says a line; with hold, its text stays in the dialog box until another line replaces it.</summary>
        public static float Say(string key, bool hold)
        {
            float t = Say(key);
            Holding = hold;
            return t;
        }

        public static float Say(string key)
        {
            Load();
            Holding = false;
            string text = Text(key);
            AudioClip clip = Clip(key);
            float seconds = clip != null ? clip.length : Mathf.Clamp(text.Length * 0.06f, 1.2f, 4f);
            if (clip != null)
            {
                EnsureSource();
                s_Voice.Stop();
                s_Voice.clip = clip;
                s_Voice.volume = Services.Save?.Data.settings.voiceVolume ?? 1f;
                s_Voice.Play();
            }
            Said?.Invoke(key, text, seconds);
            return seconds;
        }

        public static void Silence()
        {
            if (s_Voice != null) s_Voice.Stop();
        }

        public static bool Speaking => s_Voice != null && s_Voice.isPlaying;

        private static AudioClip Clip(string key)
        {
            string path = $"Voice/{Language}/{key}";
            if (!s_Clips.TryGetValue(path, out AudioClip c))
            {
                c = Resources.Load<AudioClip>(path);
                if (c == null && Language != "en") c = Resources.Load<AudioClip>($"Voice/en/{key}");
                s_Clips[path] = c;
            }
            return c;
        }

        private static void EnsureSource()
        {
            if (s_Voice != null) return;
            var go = new GameObject("Melo Voice", typeof(AudioSource));
            UnityEngine.Object.DontDestroyOnLoad(go);
            s_Voice = go.GetComponent<AudioSource>();
            s_Voice.playOnAwake = false;
            s_Voice.spatialBlend = 0f;
            s_Voice.priority = 10;
        }

        private static void Load()
        {
            if (s_Lines != null) return;
            s_Lines = new Dictionary<string, Line>();
            var ta = Resources.Load<TextAsset>("Voice/voice_lines");
            if (ta == null) return;
            LineFile f = JsonUtility.FromJson<LineFile>(ta.text);
            if (f?.lines == null) return;
            foreach (Line l in f.lines) s_Lines[l.key] = l;
        }
    }

}
