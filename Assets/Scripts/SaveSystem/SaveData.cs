using System;
using System.Collections.Generic;
using LittleMusicWorld.Audio;

namespace LittleMusicWorld.SaveSystem
{
    /// <summary>Everything the game remembers on the device. Versioned; see <see cref="SaveSerializer"/>.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public SettingsData settings = new SettingsData();
        public ProgressData progress = new ProgressData();
        public List<string> compositionIds = new List<string>(); // files under compositions/

        public void Sanitise()
        {
            settings ??= new SettingsData();
            progress ??= new ProgressData();
            compositionIds ??= new List<string>();
            settings.Sanitise();
            progress.Sanitise();
        }
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float masterVolume = 0.9f;
        public float musicVolume = 0.5f;
        public float voiceVolume = 1f;
        public string language = "en";   // "en" | "th"
        public int qualityTier = -1;     // -1 = automatic
        public bool leftHanded;

        public void Sanitise()
        {
            masterVolume = Clamp01(masterVolume);
            musicVolume = Clamp01(musicVolume);
            voiceVolume = Clamp01(voiceVolume);
            if (language != "en" && language != "th") language = "en";
            if (qualityTier < -1 || qualityTier > 2) qualityTier = -1;
        }

        private static float Clamp01(float v) => float.IsNaN(v) ? 1f : Math.Max(0f, Math.Min(1f, v));
    }

    [Serializable]
    public sealed class ProgressData
    {
        public List<string> discoveredInstruments = new List<string>();
        public List<string> visitedAreas = new List<string>();
        public List<string> completedSongs = new List<string>();      // song id + tier, e.g. "twinkle:0"
        public List<string> completedActivities = new List<string>();
        public List<string> unlockedMusicians = new List<string>();
        public List<string> unlockedDecorations = new List<string>();
        public List<string> collectibles = new List<string>();
        public int stars;
        public bool introSeen;
        public int tutorialStep;          // 0 hub part, 1 area part, 2 done (see Gameplay.Tutorial)

        public void Sanitise()
        {
            discoveredInstruments ??= new List<string>();
            visitedAreas ??= new List<string>();
            completedSongs ??= new List<string>();
            completedActivities ??= new List<string>();
            unlockedMusicians ??= new List<string>();
            unlockedDecorations ??= new List<string>();
            collectibles ??= new List<string>();
            if (stars < 0) stars = 0;
            Dedupe(discoveredInstruments); Dedupe(visitedAreas); Dedupe(completedSongs);
            Dedupe(completedActivities); Dedupe(unlockedMusicians); Dedupe(unlockedDecorations); Dedupe(collectibles);
        }

        private static void Dedupe(List<string> l)
        {
            var seen = new HashSet<string>();
            l.RemoveAll(s => string.IsNullOrEmpty(s) || !seen.Add(s));
        }

        public bool Add(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id)) return false;
            list.Add(id);
            return true;
        }
    }

    /// <summary>A Music Studio song: tracks of note events. Saved one file per composition.</summary>
    [Serializable]
    public sealed class CompositionData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public string id;
        public string title;           // generated (e.g. "Song 3"); children do not type
        public long createdUtcTicks;
        public long modifiedUtcTicks;
        public float tempo = 100f;
        public float lengthBeats = 16f;
        public int colorIndex;
        public List<TrackData> tracks = new List<TrackData>();

        public void Sanitise()
        {
            tracks ??= new List<TrackData>();
            if (float.IsNaN(tempo) || tempo < 40f || tempo > 200f) tempo = 100f;
            if (float.IsNaN(lengthBeats) || lengthBeats < 4f) lengthBeats = 16f;
            if (lengthBeats > 256f) lengthBeats = 256f;
            tracks.RemoveAll(t => t == null);
            foreach (TrackData t in tracks) t.Sanitise();
        }
    }

    [Serializable]
    public sealed class TrackData
    {
        public string instrumentId;
        public float volume = 0.9f;
        public bool muted;
        public List<NoteEvent> notes = new List<NoteEvent>();

        public void Sanitise()
        {
            notes ??= new List<NoteEvent>();
            if (float.IsNaN(volume)) volume = 0.9f;
            volume = Math.Max(0f, Math.Min(1f, volume));
            notes.RemoveAll(n => n.note < 0 || n.note > 127 || float.IsNaN(n.beat) || n.beat < 0f);
            for (int i = 0; i < notes.Count; i++)
            {
                NoteEvent n = notes[i];
                n.velocity = Math.Max(1, Math.Min(127, n.velocity == 0 ? 100 : n.velocity));
                if (float.IsNaN(n.length) || n.length < 0f) n.length = 0f;
                notes[i] = n;
            }
        }
    }
}
