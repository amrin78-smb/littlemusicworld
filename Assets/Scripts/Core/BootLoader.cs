using UnityEngine;

namespace LittleMusicWorld.Core
{
    /// <summary>
    /// First scene after Unity's own splash: shows the branded launch composition (logo over the sky) while the
    /// first area loads, so the player never sees an empty or unbranded screen.
    /// </summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private string firstScene = SceneFlow.MainMenu;
        [SerializeField] private float holdSeconds = 1.2f;

        private float t;
        private bool started;

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            if (started || t < holdSeconds || Services.Scenes == null) return;
            started = true;
            Services.Scenes.Load(firstScene, 0.3f);
        }
    }
}
