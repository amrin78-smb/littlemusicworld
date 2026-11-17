using LittleMusicWorld.Core;
using UnityEngine;

namespace LittleMusicWorld.World
{
    /// <summary>
    /// An optional golden note floating in the hub. Tap it: a sparkle arpeggio plays, it zooms into the sky and is
    /// remembered (ProgressData.collectibles). Never required for anything — just a happy surprise to find.
    /// </summary>
    public sealed class Collectible : MonoBehaviour
    {
        public string id;
        private Vector3 home;
        private float phase;
        private bool taken;

        private void Start()
        {
            home = transform.position;
            phase = (id?.GetHashCode() ?? 0) % 100 * 0.1f;
            if (Services.Save != null && Services.Save.Data.progress.collectibles.Contains(id)) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (taken) return;
            float t = Time.time + phase;
            transform.position = home + Vector3.up * (0.18f * Mathf.Sin(t * 1.8f));
            transform.rotation = Quaternion.Euler(0, t * 70f, 0);
        }

        public void Collect()
        {
            if (taken) return;
            taken = true;
            UI.UISound.Sparkle();
            Services.Discover(Services.Save?.Data.progress.collectibles, id);
            if (Services.Save != null) { Services.Save.Data.progress.stars++; Services.Save.Save(); }
            Vector3 from = transform.position;
            Tween.Run(this, 0, 0.8f, k =>
            {
                transform.position = from + Vector3.up * (6f * k * k);
                transform.localScale = Vector3.one * (1f + k);
            }, null, () => gameObject.SetActive(false));
        }
    }
}
