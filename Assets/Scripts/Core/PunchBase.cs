using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleMusicWorld.Core
{
    /// <summary>Remembers the rest scale of something that gets squashed, so repeated punches never drift.</summary>
    public sealed class PunchBase : MonoBehaviour { public Vector3 scale = Vector3.one; }
}
