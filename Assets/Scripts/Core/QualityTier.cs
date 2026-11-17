using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleMusicWorld.Core
{
    public enum Tier { Low = 0, Medium = 1, High = 2 }

    /// <summary>
    /// Picks a quality tier for the device and applies it. Low is for budget phones like the Redmi A3
    /// (PowerVR GE8320, 32-bit, 3–4 GB): 30 FPS, render scale 0.8, hard single-cascade shadows, no MSAA,
    /// fewer voices and particles. High targets 60 FPS with soft shadows and 4x MSAA.
    /// </summary>
    public static class QualityTier
    {
        public static Tier Current { get; private set; } = Tier.Medium;

        public static Tier Detect()
        {
            if (!Application.isMobilePlatform) return Tier.High;
            string gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
            int ram = SystemInfo.systemMemorySize;
            bool is64 = System.IntPtr.Size == 8;
            if (gpu.Contains("powervr") || gpu.Contains("mali-4") || gpu.Contains("mali-t") || gpu.Contains("adreno (tm) 3") ||
                ram < 3500 || !is64 || SystemInfo.processorCount <= 4)
                return Tier.Low;
            if (ram >= 7000 && SystemInfo.processorCount >= 8) return Tier.High;
            return Tier.Medium;
        }

        public static void Apply(Tier tier)
        {
            Current = tier;
            Application.targetFrameRate = tier == Tier.Low ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            Shader.globalMaximumLOD = tier == Tier.Low ? 300 : 600;
            QualitySettings.lodBias = tier == Tier.Low ? 0.7f : tier == Tier.Medium ? 1f : 1.5f;

            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null && !Application.isEditor) // in the Editor this would rewrite the asset on disk
            {
                urp.renderScale = tier == Tier.Low ? 0.8f : tier == Tier.Medium ? 0.9f : 1f;
                urp.msaaSampleCount = tier == Tier.Low ? 1 : tier == Tier.Medium ? 2 : 4;
                urp.shadowDistance = tier == Tier.Low ? 22f : tier == Tier.Medium ? 32f : 45f;
                urp.shadowCascadeCount = tier == Tier.High ? 2 : 1;
            }
            Debug.Log($"[LMW] Quality tier {tier} (gpu={SystemInfo.graphicsDeviceName}, ram={SystemInfo.systemMemorySize}MB, " +
                      $"cpus={SystemInfo.processorCount}, api={SystemInfo.graphicsDeviceType}, 64bit={System.IntPtr.Size == 8})");
        }

        public static int MaxVoices => Current == Tier.Low ? 28 : Current == Tier.Medium ? 40 : 56;
        public static float ParticleScale => Current == Tier.Low ? 0.4f : Current == Tier.Medium ? 0.7f : 1f;
    }
}
