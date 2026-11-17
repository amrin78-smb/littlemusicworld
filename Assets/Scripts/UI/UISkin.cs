using UnityEngine;

namespace LittleMusicWorld.UI
{
    /// <summary>Sprites for UI built at runtime (instrument panels, cards). Resources/UISkin, filled by setup.</summary>
    [CreateAssetMenu(menuName = "Little Music World/UI Skin")]
    public sealed class UISkin : ScriptableObject
    {
        public Sprite pebble, panel, circle, ring, glow, glowRect, keyWhite, keyBlack, note, sparkle;
        public Sprite iconPlay, iconStop, iconRecord, iconBack, iconHome, iconStar, iconListen, iconCheck, iconTrash, iconUp, iconDown, iconLock;
        public TMPro.TMP_FontAsset font;

        private static UISkin s_Instance;
        public static UISkin Instance => s_Instance != null ? s_Instance : s_Instance = Resources.Load<UISkin>("UISkin");
    }
}
