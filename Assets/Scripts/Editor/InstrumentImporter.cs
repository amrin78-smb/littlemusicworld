using System;
using System.Collections.Generic;
using System.IO;
using LittleMusicWorld.Audio;
using UnityEditor;
using UnityEngine;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Audio import rules + InstrumentDefinition generation from Tools/audio output
    /// (Assets/Audio/Instruments/&lt;Id&gt;/instrument.json + WAVs).
    ///
    /// Samples import as mono Vorbis (small APK), "Decompress On Load" but NOT preloaded: the sampler decodes an
    /// instrument's samples into its own 16-bit buffers when an area loads that instrument, then releases Unity's copy.
    /// Sample rate is preserved so the loop points written by the build script stay sample-exact.
    /// </summary>
    public sealed class InstrumentAudioPostprocessor : AssetPostprocessor
    {
        public const string Folder = "Assets/Audio/Instruments/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            imp.loadInBackground = false;
            var s = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.DecompressOnLoad,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.62f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = false
            };
            imp.defaultSampleSettings = s;
            imp.SetOverrideSampleSettings("Android", s);
        }
    }

    public static class InstrumentImporter
    {
        public const string DefinitionFolder = "Assets/ScriptableObjects/Instruments";
        public const string LibraryPath = "Assets/Resources/InstrumentLibrary.asset";

        [Serializable] private sealed class ZoneJson
        {
            public string file; public int root, lo, hi, loVel, hiVel, loopStart, loopEnd; public float tune, gain; public string label;
        }

        [Serializable] private sealed class InstrumentJson
        {
            public string id, name, family, style, color;
            public float attack, release, volume;
            public bool oneShot, sustainLoop;
            public int voices, lowest, highest, rate;
            public int[] playable, openStrings, chokeNotes, chokeIds;
            public ZoneJson[] zones;
        }

        /// <summary>Order instruments appear in menus (area by area).</summary>
        public static readonly string[] Order =
        {
            "GrandPiano", "UprightPiano", "DigitalKeyboard",
            "DrumKit", "Snare", "BassDrum", "Cymbals", "Bongos", "Congas", "Tambourine", "Maracas", "Xylophone",
            "AcousticGuitar", "ElectricGuitar", "Ukulele", "Violin", "Cello", "Harp", "Banjo",
            "Flute", "Trumpet", "Saxophone", "Clarinet", "Trombone", "Recorder"
        };

        public static InstrumentLibrary BuildAll()
        {
            E.EnsureFolder(DefinitionFolder);
            var defs = new List<InstrumentDefinition>();
            string root = InstrumentAudioPostprocessor.Folder.TrimEnd('/');
            if (!Directory.Exists(root)) { Debug.LogError("[LMW] No instrument audio; run Tools/audio/build_instruments.py"); return null; }

            var dirs = new List<string>(Directory.GetDirectories(root));
            dirs.Sort((a, b) => Rank(Path.GetFileName(a)).CompareTo(Rank(Path.GetFileName(b))));
            foreach (string dir in dirs)
            {
                string jsonPath = Path.Combine(dir, "instrument.json");
                if (!File.Exists(jsonPath)) continue;
                InstrumentDefinition def = Build(dir.Replace('\\', '/'), JsonUtility.FromJson<InstrumentJson>(File.ReadAllText(jsonPath)));
                if (def != null) defs.Add(def);
            }

            InstrumentLibrary lib = E.LoadOrCreateSO<InstrumentLibrary>(LibraryPath);
            lib.instruments = defs;
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LMW] Instrument library: {defs.Count} instruments, {CountZones(defs)} sample zones.");
            return lib;
        }

        private static int Rank(string id)
        {
            int i = Array.IndexOf(Order, id);
            return i < 0 ? 1000 : i;
        }

        private static int CountZones(List<InstrumentDefinition> defs)
        {
            int n = 0;
            foreach (InstrumentDefinition d in defs) n += d.zones.Count;
            return n;
        }

        private static InstrumentDefinition Build(string dir, InstrumentJson j)
        {
            if (j == null || string.IsNullOrEmpty(j.id)) return null;
            InstrumentDefinition def = E.LoadOrCreateSO<InstrumentDefinition>($"{DefinitionFolder}/{j.id}.asset");
            def.id = j.id;
            def.displayName = j.name;
            def.family = (InstrumentFamily)Enum.Parse(typeof(InstrumentFamily), j.family);
            def.playStyle = (PlayStyle)Enum.Parse(typeof(PlayStyle), j.style);
            def.color = E.Hex(j.color);
            def.attack = j.attack;
            def.release = j.release;
            def.oneShot = j.oneShot;
            def.sustainLoop = j.sustainLoop;
            def.volume = j.volume;
            def.maxVoices = j.voices;
            def.lowestNote = j.lowest;
            def.highestNote = j.highest;
            def.playableNotes = j.playable ?? Array.Empty<int>();
            def.openStrings = j.openStrings ?? Array.Empty<int>();
            def.chokeGroups = Array.Empty<int>();
            if (j.chokeNotes != null && j.chokeNotes.Length > 0)
            {
                def.chokeGroups = new int[j.highest - j.lowest + 1];
                for (int i = 0; i < j.chokeNotes.Length; i++)
                {
                    int idx = j.chokeNotes[i] - j.lowest;
                    if (idx >= 0 && idx < def.chokeGroups.Length) def.chokeGroups[idx] = j.chokeIds[i];
                }
            }
            def.zones = new List<SampleZone>();
            foreach (ZoneJson z in j.zones)
            {
                string clipPath = $"{dir}/{z.file}";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null) { Debug.LogError("[LMW] Missing sample " + clipPath); continue; }
                def.zones.Add(new SampleZone
                {
                    clip = clip, rootNote = z.root, lowNote = z.lo, highNote = z.hi, lowVelocity = z.loVel,
                    highVelocity = z.hiVel, loopStart = z.loopStart, loopEnd = z.loopEnd, tuneCents = z.tune,
                    gain = z.gain <= 0 ? 1f : z.gain, label = z.label
                });
            }
            EditorUtility.SetDirty(def);
            return def;
        }
    }
}
