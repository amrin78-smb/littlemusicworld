using System.Linq;
using LittleMusicWorld.Audio;
using UnityEditor;
using UnityEngine;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>Checks the generated project for things that must never ship broken.</summary>
    public static class ProjectValidator
    {
        [MenuItem(ProjectSetup.MenuRoot + "Validate", priority = 2)]
        public static bool Validate()
        {
            int errors = 0;
            void Fail(string m) { errors++; Debug.LogError("[LMW Validate] " + m); }

            var lib = AssetDatabase.LoadAssetAtPath<InstrumentLibrary>(InstrumentImporter.LibraryPath);
            if (lib == null) Fail("InstrumentLibrary missing");
            else
            {
                if (lib.instruments.Count < 15) Fail($"only {lib.instruments.Count} instruments (need ≥ 15)");
                foreach (InstrumentDefinition d in lib.instruments)
                {
                    if (d == null) { Fail("null instrument in library"); continue; }
                    if (d.zones.Count == 0) Fail(d.id + " has no samples");
                    if (d.zones.Any(z => z.clip == null)) Fail(d.id + " has a zone without a clip");
                    if (d.playableNotes.Length == 0 && d.playStyle != PlayStyle.DrumPads) Fail(d.id + " has no playable notes");
                }
            }

            if (PlayerSettings.Android.targetArchitectures != (AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64))
                Fail("Android architectures must be ARMv7 + ARM64");
            if (PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                Fail("Android scripting backend must be IL2CPP");
            var filters = PlayerSettings.Android.androidVulkanDeviceFilterListAsset;
            if (filters == null || filters.vulkanDeviceDenyFilters == null ||
                !filters.vulkanDeviceDenyFilters.Any(f => f.deviceName == ProjectConfigurator.PowerVrPattern))
                Fail("PowerVR Vulkan deny filter missing");
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) == null) Fail("build scene missing: " + s.path);
            if (EditorBuildSettings.scenes.Length == 0) Fail("no scenes in build settings");

            // Every serialisable component must live in a file of the same name, or Unity cannot map scene data to it
            // in the player ("level1 is corrupted! Position out of bounds" crash, 2026-11-18). Runtime-only classes
            // (added with AddComponent and never saved) are listed explicitly.
            var runtimeOnly = new System.Collections.Generic.HashSet<string> { "MiniGame", "RhythmMatchGame", "SoundDetectiveGame",
                "FollowBeatGame", "MemoryGame", "HighLowGame", "WhoIsPlayingGame", "MelodyBuilderGame" };
            var scripts = new System.Collections.Generic.HashSet<System.Type>();
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
            {
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (ms != null && ms.GetClass() != null) scripts.Add(ms.GetClass());
            }
            foreach (System.Type t in typeof(LittleMusicWorld.Audio.MusicalAudioEngine).Assembly.GetTypes())
            {
                bool serialisable = typeof(MonoBehaviour).IsAssignableFrom(t) || typeof(ScriptableObject).IsAssignableFrom(t);
                if (!serialisable || t.IsNested || runtimeOnly.Contains(t.Name)) continue;
                if (!scripts.Contains(t)) Fail($"{t.FullName} is not in a file named {t.Name}.cs (scenes using it break in builds)");
            }

            Debug.Log($"[LMW Validate] {(errors == 0 ? "OK" : errors + " problem(s)")}");
            return errors == 0;
        }
    }
}
