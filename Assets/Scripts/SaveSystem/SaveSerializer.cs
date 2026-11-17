using System;
using UnityEngine;

namespace LittleMusicWorld.SaveSystem
{
    public enum LoadOutcome { Loaded, Migrated, Missing, Corrupt, NewerVersion }

    /// <summary>Pure JSON (de)serialisation and migration (no file IO, unit tested).</summary>
    public static class SaveSerializer
    {
        [Serializable] private sealed class VersionProbe { public int version; }

        public static string Serialize(SaveData data)
        {
            data.version = SaveData.CurrentVersion;
            return JsonUtility.ToJson(data, true);
        }

        public static string Serialize(CompositionData data)
        {
            data.version = CompositionData.CurrentVersion;
            return JsonUtility.ToJson(data, false);
        }

        public static SaveData Deserialize(string json, out LoadOutcome outcome) =>
            Read(json, SaveData.CurrentVersion, MigrateSave, d => d.Sanitise(), out outcome) ?? new SaveData();

        public static CompositionData DeserializeComposition(string json, out LoadOutcome outcome) =>
            Read(json, CompositionData.CurrentVersion, MigrateComposition, d => d.Sanitise(), out outcome);

        private static T Read<T>(string json, int current, Func<string, int, T> migrate, Action<T> sanitise, out LoadOutcome outcome)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) { outcome = LoadOutcome.Missing; return null; }
            if (!json.TrimStart().StartsWith("{")) { outcome = LoadOutcome.Corrupt; return null; }
            VersionProbe probe;
            try { probe = JsonUtility.FromJson<VersionProbe>(json); }
            catch (Exception) { outcome = LoadOutcome.Corrupt; return null; }
            if (probe == null || probe.version < 0) { outcome = LoadOutcome.Corrupt; return null; }

            T data;
            try { data = migrate(json, probe.version); }
            catch (Exception) { outcome = LoadOutcome.Corrupt; return null; }
            if (data == null) { outcome = LoadOutcome.Corrupt; return null; }
            sanitise(data);
            outcome = probe.version > current ? LoadOutcome.NewerVersion
                : probe.version < current ? LoadOutcome.Migrated : LoadOutcome.Loaded;
            return data;
        }

        /// <summary>
        /// Upgrades older save JSON one step at a time. Version 1 is the first released shape (0 = no version field,
        /// same shape). Add steps as: if (version == 1) { json = V1ToV2(json); version = 2; }
        /// </summary>
        private static SaveData MigrateSave(string json, int version) => JsonUtility.FromJson<SaveData>(json);

        private static CompositionData MigrateComposition(string json, int version) => JsonUtility.FromJson<CompositionData>(json);
    }
}
