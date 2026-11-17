using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>
    /// On screens squarer than 16:9 (tablets) the 1920×1080 layout scales by width instead of height so nothing is
    /// cut off at the sides; on wide phones it scales by height.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasAspect : MonoBehaviour
    {
        private CanvasScaler scaler;
        private Vector2Int last;

        private void Awake() => scaler = GetComponent<CanvasScaler>();

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == last) return;
            last = size;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            scaler.matchWidthOrHeight = aspect < 16f / 9f - 0.01f ? 0f : 1f;
        }
    }
}
