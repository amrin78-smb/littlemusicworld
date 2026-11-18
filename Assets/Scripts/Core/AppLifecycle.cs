using System;
using LittleMusicWorld.Audio;
using LittleMusicWorld.SaveSystem;
using UnityEngine;

namespace LittleMusicWorld.Core
{
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

        /// <summary>Android back: area → hub → main menu; on the menu it does nothing (no accidental exits).</summary>
        private const float AwakeAfterTouchSeconds = 600f;
        private float lastTouch;
        private bool keepAwake;

        /// <summary>Screen stays on while the child plays or listens; the phone's own timeout returns after 10 idle minutes.</summary>
        private void KeepScreenAwake()
        {
            var p = UnityEngine.InputSystem.Pointer.current;
            if (p != null && p.press.isPressed) lastTouch = Time.unscaledTime;
            bool want = Time.unscaledTime - lastTouch < AwakeAfterTouchSeconds;
            if (want == keepAwake) return;
            keepAwake = want;
            Screen.sleepTimeout = want ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }

        private void Update()
        {
            KeepScreenAwake();
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame || Services.Scenes == null || Services.Scenes.Busy) return;
            string cur = Services.Scenes.Current;
            if (cur == SceneFlow.MainMenu || cur == SceneFlow.Boot) return;
            Characters.Guide.Silence();
            Services.Scenes.Load(cur == SceneFlow.Hub ? SceneFlow.MainMenu : SceneFlow.Hub);
        }
    }
}
