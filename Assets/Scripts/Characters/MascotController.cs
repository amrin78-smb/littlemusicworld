using System;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;

namespace LittleMusicWorld.Characters
{
    /// <summary>
    /// Melo in the world: plays animation states with short crossfades, walks to points (hub navigation), talks
    /// while a voice line plays, and reacts to music — a hop for single notes, dancing while lots of notes play.
    /// One-shot clips (Celebrate, Point, React) return to the base state automatically.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class MascotController : MonoBehaviour
    {
        public enum Base { Idle, Dance, Sleep, Sing }

        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private bool reactToMusic = true;
        [SerializeField] private bool talkWhenSpeaking = true;

        public bool Walking { get; private set; }
        public event Action Arrived;

        private Animator animator;
        private Base baseState = Base.Idle;
        private string current;
        private float oneShotUntil;
        private Vector3 target;
        private float talkUntil;
        private float danceEnergy;
        private float lastReact;

        private static readonly (string name, float seconds)[] OneShots =
            { ("Celebrate", 1.5f), ("Point", 1.4f), ("React", 0.6f), ("Wave", 2.4f) };

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private void OnEnable()
        {
            Guide.Said += OnSaid;
            if (Services.Engine != null) Services.Engine.NoteStarted += OnNote;
        }

        private void OnDisable()
        {
            Guide.Said -= OnSaid;
            if (Services.Engine != null) Services.Engine.NoteStarted -= OnNote;
        }

        private void Start() => Play("Idle", 0f);

        public void SetBase(Base b)
        {
            baseState = b;
            if (Time.time >= oneShotUntil && !Walking) Play(b.ToString());
        }

        public void Do(string action)
        {
            foreach (var (name, seconds) in OneShots)
            {
                if (name != action) continue;
                Play(name, 0.12f, true);
                oneShotUntil = Time.time + seconds;
                return;
            }
            Play(action);
        }

        public void Celebrate() => Do("Celebrate");
        public void Point() => Do("Point");
        public void Wave() => Do("Wave");

        public void WalkTo(Vector3 worldPoint)
        {
            target = worldPoint;
            target.y = transform.position.y;
            Walking = (target - transform.position).sqrMagnitude > 0.01f;
            if (Walking) Play("Walk", 0.1f);
        }

        public void StopWalking()
        {
            Walking = false;
            Play(baseState.ToString());
        }

        public void FaceTowards(Vector3 worldPoint, bool instant = false)
        {
            Vector3 d = worldPoint - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            Quaternion q = Quaternion.LookRotation(d);
            transform.rotation = instant ? q : Quaternion.RotateTowards(transform.rotation, q, turnSpeed * Time.deltaTime);
        }

        private void Play(string state, float fade = 0.18f, bool restart = false)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (!restart && current == state) return;
            current = state;
            animator.CrossFadeInFixedTime(state, fade);
        }

        private void OnSaid(string key, string text, float seconds)
        {
            if (!talkWhenSpeaking) return;
            talkUntil = Time.time + seconds;
            if (!Walking && Time.time >= oneShotUntil) Play("Talk", 0.1f);
        }

        private void OnNote(InstrumentDefinition def, int note, int velocity, int channel)
        {
            if (!reactToMusic) return;
            danceEnergy = Mathf.Min(6f, danceEnergy + 1f);
            if (danceEnergy < 3f && Time.time - lastReact > 1.2f && Time.time >= oneShotUntil && !Walking && Time.time >= talkUntil)
            {
                lastReact = Time.time;
                Do("React");
            }
        }

        private void Update()
        {
            danceEnergy = Mathf.Max(0f, danceEnergy - Time.deltaTime * 1.5f);
            if (Walking)
            {
                Vector3 to = target - transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.05f)
                {
                    Walking = false;
                    Play(baseState.ToString());
                    Arrived?.Invoke();
                }
                else
                {
                    FaceTowards(target);
                    float facing = Vector3.Dot(transform.forward, to / dist);
                    float step = walkSpeed * Time.deltaTime * Mathf.Clamp01(facing + 0.3f);
                    transform.position += to / dist * Mathf.Min(step, dist);
                }
                return;
            }
            if (Time.time < oneShotUntil) return;
            if (Time.time < talkUntil) { Play("Talk", 0.1f); return; }
            if (reactToMusic && danceEnergy > 3f) { Play("Dance", 0.2f); return; }
            Play(baseState.ToString());
        }
    }
}
