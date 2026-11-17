using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// One entry point that (re)generates everything derived from sources: settings, UI sprites, instrument
    /// definitions, imported Blender models, prefabs and scenes. Batch: Tools/setup.sh. Menu: Little Music World ▸ Setup.
    /// </summary>
    public static class ProjectSetup
    {
        public const string MenuRoot = "Little Music World/";

        [MenuItem(MenuRoot + "Setup Project", priority = 1)]
        public static void Run() => RunInternal();

        public static void RunBatch()
        {
            bool ok = false;
            try { ok = RunInternal(); }
            catch (Exception e) { Debug.LogException(e); }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool RunInternal()
        {
            DateTime t0 = DateTime.Now;
            Step("TextMeshPro", () => ProjectConfigurator.EnsureTextMeshPro());
            Step("Player settings", ProjectConfigurator.ConfigurePlayerSettings);
            Step("Render pipeline", ProjectConfigurator.ConfigureRenderPipeline);
            Step("UI sprites", UISprites.BuildAll);
            Step("Instruments", () => InstrumentImporter.BuildAll());
            Step("Scenes", SceneBuilder.BuildAll);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            bool valid = ProjectValidator.Validate();
            Debug.Log($"[LMW] Setup finished in {(DateTime.Now - t0).TotalSeconds:0}s, validation {(valid ? "passed" : "FAILED")}.");
            return valid;
        }

        private static void Step(string name, Action a)
        {
            DateTime t = DateTime.Now;
            a();
            Debug.Log($"[LMW] step '{name}' done in {(DateTime.Now - t).TotalSeconds:0.0}s");
        }
    }
}
