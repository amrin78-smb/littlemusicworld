using UnityEngine;

namespace LittleMusicWorld.Core
{
    /// <summary>
    /// Test hooks from the launch intent (adb shell am start … --es lmw_scene DrumValley --es lmw_station 2) or the
    /// desktop command line (-lmw_scene DrumValley). Used only by Tools/device_walkthrough.sh; normal launches have none.
    /// </summary>
    public static class DeviceArgs
    {
        public static string Get(string key)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-" + key) return args[i + 1];
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent");
                return intent.Call<string>("getStringExtra", key);
            }
            catch (System.Exception) { return null; }
#else
            return null;
#endif
        }
    }
}
