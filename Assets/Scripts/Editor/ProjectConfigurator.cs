using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Player, Android, graphics, audio and quality configuration. Re-runnable; every build calls
    /// <see cref="ConfigurePlayerSettings"/> so the budget-phone requirements can never be lost:
    ///   IL2CPP ARMv7 + ARM64 (the Redmi A3 runs 32-bit Android), Vulkan with a PowerVR deny filter (the Redmi A3's
    ///   PowerVR GE8320 crashed on relaunch under Vulkan in an earlier game) → OpenGL ES 3 there, landscape, offline.
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string AppVersion = "0.1.0";
        public const int AppVersionCode = 1;
        public const string ApplicationId = "com.nocvault.littlemusicworld";
        public const string VulkanFilterPath = "Assets/Settings/VulkanDeviceFilters.asset";
        public const string PowerVrPattern = ".*PowerVR.*";

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Nocvault";
            PlayerSettings.productName = "Little Music World";
            PlayerSettings.bundleVersion = AppVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationId);
            PlayerSettings.Android.bundleVersionCode = Math.Max(AppVersionCode, PlayerSettings.Android.bundleVersionCode);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 25) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.gpuSkinning = true;
            EditorUserBuildSettings.buildAppBundle = false;

            ConfigureSplash();
            ConfigureIcons();
            ConfigureAndroidGraphics();
            ConfigureAudio();

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
        }

        /// <summary>
        /// Unity Personal: the "Made with Unity" splash cannot be disabled. We configure what the licence permits —
        /// background colour, logo style, a short duration and our own logo shown alongside — and our branded Boot
        /// scene takes over immediately afterwards (see Documentation/TESTING.md, branding).
        /// </summary>
        public static void ConfigureSplash()
        {
            PlayerSettings.SplashScreen.backgroundColor = E.Hex("#8FD3FF");
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Branding/Splash_Logo.png");
            if (logo != null)
            {
                PlayerSettings.SplashScreen.logos = new[]
                {
                    PlayerSettings.SplashScreenLogo.Create(2f, logo),
                    PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(2f)
                };
            }
            try { PlayerSettings.SplashScreen.show = true; } catch (Exception) { /* Personal: always shown */ }
            Debug.Log($"[LMW] Splash: show={PlayerSettings.SplashScreen.show}, advanced license={PlayerSettings.advancedLicense}, " +
                      $"logos={PlayerSettings.SplashScreen.logos?.Length ?? 0}");
        }

        /// <summary>
        /// Launcher icons from Tools/branding output: adaptive (background + foreground layers, API 26+), round and
        /// legacy (square) for older launchers. Also the default (all-platform) icon.
        /// </summary>
        public static void ConfigureIcons()
        {
            Texture2D master = Tex("Assets/Art/Branding/Icon_Master.png");
            Texture2D bg = Tex("Assets/Art/Branding/Icon_Background.png");
            Texture2D fg = Tex("Assets/Art/Branding/Icon_Foreground.png");
            if (master == null || bg == null || fg == null) { Debug.LogWarning("[LMW] Branding icons missing; run Tools/branding/build_branding.sh"); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { master }, IconKind.Any);
            var android = NamedBuildTarget.Android;
            foreach (PlatformIconKind kind in new[] { UnityEditor.Android.AndroidPlatformIconKind.Adaptive,
                         UnityEditor.Android.AndroidPlatformIconKind.Round, UnityEditor.Android.AndroidPlatformIconKind.Legacy })
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (PlatformIcon icon in icons)
                {
                    if (kind == UnityEditor.Android.AndroidPlatformIconKind.Adaptive) icon.SetTextures(bg, fg);
                    else icon.SetTexture(master);
                }
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            Debug.Log("[LMW] Android icons: adaptive (bg+fg), round, legacy configured.");
        }

        private static Texture2D Tex(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
            if (ti.textureType != TextureImporterType.Default || ti.mipmapEnabled || !ti.alphaIsTransparency || ti.textureCompression != TextureImporterCompression.Uncompressed)
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void ConfigureAndroidGraphics()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            E.EnsureFolder(Path.GetDirectoryName(VulkanFilterPath));
            var filters = AssetDatabase.LoadAssetAtPath<VulkanDeviceFilterLists>(VulkanFilterPath);
            if (filters == null)
            {
                filters = new VulkanDeviceFilterLists();
                AssetDatabase.CreateAsset(filters, VulkanFilterPath);
            }
            filters.vulkanDeviceDenyFilters = new[] { new VulkanDeviceFilterData { deviceName = PowerVrPattern } };
            EditorUtility.SetDirty(filters);
            AssetDatabase.SaveAssets();
            PlayerSettings.Android.androidVulkanDeviceFilterListAsset = filters;
        }

        /// <summary>Low-latency audio: small DSP buffer ("Best latency") at the device's native rate.</summary>
        public static void ConfigureAudio()
        {
            var am = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0]);
            void SetI(string n, int v) { SerializedProperty p = am.FindProperty(n); if (p != null) p.intValue = v; }
            SetI("m_DSPBufferSize", 256);           // legacy field
            SetI("m_RequestedDSPBufferSize", 256);  // "Best latency"
            SetI("m_SampleRate", 48000);            // "system default" resolved to 24 kHz on Android: force 48 kHz
            SetI("m_OutputSamplingRate", 48000);
            SetI("m_RealVoiceCount", 24);
            SetI("m_VirtualVoiceCount", 32);
            am.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureRenderPipeline()
        {
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            foreach (RenderPipelineAsset a in new[] { mobile, pc })
            {
                if (a == null) continue;
                var so = new SerializedObject(a);
                void SetI(string n, int v) { SerializedProperty p = so.FindProperty(n); if (p != null) p.intValue = v; }
                void SetF(string n, float v) { SerializedProperty p = so.FindProperty(n); if (p != null) p.floatValue = v; }
                void SetB(string n, bool v) { SerializedProperty p = so.FindProperty(n); if (p != null) p.boolValue = v; }
                SetB("m_MainLightShadowsSupported", true);
                SetB("m_SoftShadowsSupported", a != mobile);
                SetF("m_ShadowDistance", a == mobile ? 30f : 45f);
                SetI("m_ShadowCascadeCount", a == mobile ? 1 : 2);
                SetI("m_MainLightShadowmapResolution", a == mobile ? 1024 : 2048);
                SetI("m_AdditionalLightsRenderingMode", a == mobile ? 0 : 1);
                SetI("m_MSAA", a == mobile ? 2 : 4);
                SetB("m_SupportsHDR", a != mobile);
                SetB("m_RequireDepthTexture", false);
                SetB("m_RequireOpaqueTexture", false);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }
            if (mobile != null) GraphicsSettings.defaultRenderPipeline = mobile;

            string[] names = QualitySettings.names;
            int mobileLevel = Array.IndexOf(names, "Mobile");
            if (mobileLevel >= 0)
            {
                var qso = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
                SerializedProperty perPlatform = qso.FindProperty("m_PerPlatformDefaultQuality");
                if (perPlatform != null)
                {
                    for (int i = 0; i < perPlatform.arraySize; i++)
                    {
                        SerializedProperty e = perPlatform.GetArrayElementAtIndex(i);
                        if (e.FindPropertyRelative("first")?.stringValue == "Android")
                            e.FindPropertyRelative("second").intValue = mobileLevel;
                    }
                    qso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        public const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        public static bool EnsureTextMeshPro()
        {
            if (AssetDatabase.LoadMainAssetAtPath("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) return true;
            string full = Path.GetFullPath(TmpPackage);
            if (!File.Exists(full)) { Debug.LogError("[LMW] TMP essentials not found at " + full); return false; }
            AssetDatabase.ImportPackage(full, false);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadMainAssetAtPath("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;
        }

        public static void ConfigureBuildScenes(params string[] scenePaths)
        {
            var list = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++) list[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            EditorBuildSettings.scenes = list;
        }
    }
}
