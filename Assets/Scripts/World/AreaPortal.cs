using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LittleMusicWorld.World
{
    /// <summary>An entrance to a musical area. Tapping it (or walking into it) sends Melo there and loads the area.</summary>
    public sealed class AreaPortal : MonoBehaviour
    {
        public string sceneName;
        public string voiceKey;
        public Transform approach;   // where Melo stops before entering
        public bool open = true;
        public Color tint = Color.white;

        public Vector3 ApproachPoint => approach != null ? approach.position : transform.position;
    }
}
