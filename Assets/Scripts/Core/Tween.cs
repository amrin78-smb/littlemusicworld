using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleMusicWorld.Core
{
    public static class Ease
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
        /// <summary>0→1→0 bump (squash/stretch pulses).</summary>
        public static float Bump(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
    }

    /// <summary>
    /// Tiny allocation-light tween runner (one shared updater, unscaled time). Tweens are keyed by owner+channel so a
    /// new tween on the same target replaces the old one (no fighting animations when a child taps fast).
    /// </summary>
    public sealed class Tween : MonoBehaviour
    {
        private sealed class Job
        {
            public object owner; public int channel; public float duration, time, delay;
            public Action<float> step; public Action done; public Func<float, float> ease;
        }

        private static Tween s_Runner;
        private readonly List<Job> jobs = new List<Job>(64);
        private readonly Stack<Job> pool = new Stack<Job>();

        private static Tween Runner
        {
            get
            {
                if (s_Runner != null) return s_Runner;
                var go = new GameObject("LMW Tween");
                DontDestroyOnLoad(go);
                return s_Runner = go.AddComponent<Tween>();
            }
        }

        public static void Run(object owner, int channel, float duration, Action<float> step, Func<float, float> ease = null,
            Action done = null, float delay = 0f)
        {
            Tween r = Runner;
            r.Cancel(owner, channel, false);
            Job j = r.pool.Count > 0 ? r.pool.Pop() : new Job();
            j.owner = owner; j.channel = channel; j.duration = Mathf.Max(0.0001f, duration); j.time = 0f; j.delay = delay;
            j.step = step; j.done = done; j.ease = ease;
            r.jobs.Add(j);
        }

        public static void Stop(object owner, int channel = -1)
        {
            if (s_Runner != null) s_Runner.Cancel(owner, channel, true);
        }

        private void Cancel(object owner, int channel, bool all)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(jobs[i].owner, owner) || (!all && jobs[i].channel != channel) || (all && channel >= 0 && jobs[i].channel != channel)) continue;
                pool.Push(jobs[i]);
                jobs.RemoveAt(i);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (i >= jobs.Count) continue;
                Job j = jobs[i];
                if (j.owner is UnityEngine.Object uo && uo == null) { jobs.RemoveAt(i); pool.Push(j); continue; }
                if (j.delay > 0f) { j.delay -= dt; continue; }
                j.time += dt;
                float t = Mathf.Clamp01(j.time / j.duration);
                try { j.step?.Invoke(j.ease != null ? j.ease(t) : t); }
                catch (Exception e) { Debug.LogException(e); t = 1f; }
                if (t >= 1f)
                {
                    jobs.Remove(j);
                    Action done = j.done;
                    pool.Push(j);
                    done?.Invoke();
                }
            }
        }

        // ---- convenience
        public static void Punch(Transform t, float amount = 0.15f, float duration = 0.25f, int channel = 0)
        {
            if (t == null) return;
            Vector3 baseScale = t.localScale == Vector3.zero ? Vector3.one : t.localScale;
            if (t.TryGetComponent(out PunchBase pb)) baseScale = pb.scale;
            else t.gameObject.AddComponent<PunchBase>().scale = baseScale;
            Run(t, 100 + channel, duration, k =>
            {
                if (t == null) return;
                float s = 1f + amount * Ease.Bump(k) * (1f - k * 0.3f);
                t.localScale = new Vector3(baseScale.x * s, baseScale.y * (2f - s), baseScale.z * s);
            }, null, () => { if (t != null) t.localScale = baseScale; });
        }

        public static void Fade(CanvasGroup g, float to, float duration, Action done = null)
        {
            if (g == null) return;
            float from = g.alpha;
            Run(g, 1, duration, k => { if (g != null) g.alpha = Mathf.Lerp(from, to, k); }, Ease.InOutSine, done);
        }
    }

    /// <summary>Remembers the rest scale of something that gets squashed, so repeated punches never drift.</summary>
    public sealed class PunchBase : MonoBehaviour { public Vector3 scale = Vector3.one; }
}
