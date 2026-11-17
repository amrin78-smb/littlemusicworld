using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleMusicWorld.World
{
    /// <summary>An instrument placed in an area. Tap it to play it; the camera moves to <see cref="view"/>.</summary>
    public sealed class InstrumentStation : MonoBehaviour
    {
        public string instrumentId;
        public Transform view;          // camera pose while playing this instrument
        public InstrumentModel3D model;
        public Transform bounce;        // bobs gently to invite a tap until discovered

        public InstrumentDefinition Definition => Services.Instruments?.Get(instrumentId);
    }
}
