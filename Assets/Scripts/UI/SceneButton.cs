using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>A button that changes scene (home/back). Wired at runtime so generated scenes need no code.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class SceneButton : MonoBehaviour
    {
        public string scene;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                Characters.Guide.Silence();
                Services.Scenes?.Load(scene);
            });
        }
    }
}
