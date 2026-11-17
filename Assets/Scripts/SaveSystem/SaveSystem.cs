using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleMusicWorld.SaveSystem
{
    /// <summary>
    /// Local persistence. Writes are atomic: data goes to a temp file, the previous good file becomes .bak, then the
    /// temp file is moved into place, so an interrupted write (battery dies, app killed) can always be recovered.
    /// Loading falls back to the backup when the main file is missing or corrupt. No network, no accounts.
    /// </summary>
    public sealed class SaveStore
    {
        public string Root { get; }
        public SaveData Data { get; private set; }
        public LoadOutcome LastOutcome { get; private set; }

        public event Action Changed;

        private string MainPath => Path.Combine(Root, "save.json");
        private string CompositionDir => Path.Combine(Root, "compositions");

        public SaveStore(string root)
        {
            Root = root;
            Directory.CreateDirectory(Root);
            Load();
        }

        public void Load()
        {
            Data = TryRead(MainPath, out LoadOutcome outcome);
            if (Data == null)
            {
                SaveData backup = TryRead(MainPath + ".bak", out LoadOutcome bo);
                if (backup != null)
                {
                    Debug.LogWarning($"[LMW Save] main save {outcome}; restored backup ({bo}).");
                    Data = backup;
                    outcome = LoadOutcome.Migrated;
                }
            }
            Data ??= new SaveData();
            LastOutcome = outcome;
        }

        private static SaveData TryRead(string path, out LoadOutcome outcome)
        {
            if (!File.Exists(path)) { outcome = LoadOutcome.Missing; return null; }
            try
            {
                SaveData d = SaveSerializer.Deserialize(File.ReadAllText(path), out outcome);
                return outcome == LoadOutcome.Corrupt || outcome == LoadOutcome.Missing ? null : d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LMW Save] read failed: " + e.Message);
                outcome = LoadOutcome.Corrupt;
                return null;
            }
        }

        public void Save()
        {
            WriteAtomic(MainPath, SaveSerializer.Serialize(Data));
            Changed?.Invoke();
        }

        public static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, text);
                if (File.Exists(path))
                {
                    string bak = path + ".bak";
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(path, bak);
                }
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError("[LMW Save] write failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------ compositions

        public CompositionData LoadComposition(string id)
        {
            string p = Path.Combine(CompositionDir, id + ".json");
            foreach (string candidate in new[] { p, p + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    CompositionData c = SaveSerializer.DeserializeComposition(File.ReadAllText(candidate), out LoadOutcome o);
                    if (c != null) { c.id = id; return c; }
                }
                catch (Exception e) { Debug.LogWarning("[LMW Save] composition read failed: " + e.Message); }
            }
            return null;
        }

        public void SaveComposition(CompositionData c)
        {
            if (string.IsNullOrEmpty(c.id)) c.id = "song_" + DateTime.UtcNow.Ticks.ToString("x");
            if (c.createdUtcTicks == 0) c.createdUtcTicks = DateTime.UtcNow.Ticks;
            c.modifiedUtcTicks = DateTime.UtcNow.Ticks;
            Directory.CreateDirectory(CompositionDir);
            WriteAtomic(Path.Combine(CompositionDir, c.id + ".json"), SaveSerializer.Serialize(c));
            if (!Data.compositionIds.Contains(c.id)) Data.compositionIds.Add(c.id);
            Save();
        }

        public void DeleteComposition(string id)
        {
            string p = Path.Combine(CompositionDir, id + ".json");
            foreach (string f in new[] { p, p + ".bak", p + ".tmp" }) if (File.Exists(f)) File.Delete(f);
            Data.compositionIds.Remove(id);
            Save();
        }

        public List<CompositionData> LoadAllCompositions()
        {
            var list = new List<CompositionData>();
            foreach (string id in Data.compositionIds.ToArray())
            {
                CompositionData c = LoadComposition(id);
                if (c != null) list.Add(c);
            }
            return list;
        }

        /// <summary>Parent area: erase all progress and songs (settings kept).</summary>
        public void ResetProgress(bool alsoCompositions)
        {
            Data.progress = new ProgressData();
            if (alsoCompositions)
                foreach (string id in Data.compositionIds.ToArray()) DeleteComposition(id);
            Save();
        }
    }
}
