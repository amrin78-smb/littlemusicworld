using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LittleMusicWorld.Audio;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LittleMusicWorld.Core
{
    /// <summary>
    /// On-device measurement run (not part of normal play). Start it with
    ///   adb shell am start -n com.nocvault.littlemusicworld/com.unity3d.player.UnityPlayerGameActivity --es lmw_selftest 1
    /// (Tools/device_selftest.sh). It measures what the audio pillar depends on and logs "[LMW SelfTest]" lines:
    /// instrument decode time and memory, mixer CPU load per audio buffer, 40-note polyphony, sample-accurate
    /// scheduling (late notes), a fast multi-track sequence, held loops, and frame pacing during all of it.
    /// </summary>
    public sealed class DeviceSelfTest : MonoBehaviour
    {
        private readonly StringBuilder report = new StringBuilder();
        private readonly List<float> frameMs = new List<float>(4096);
        private bool recordFrames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void MaybeStart()
        {
            if (!Requested()) return;
            var go = new GameObject("LMW Self Test");
            DontDestroyOnLoad(go);
            go.AddComponent<DeviceSelfTest>();
        }

        private static bool Requested()
        {
            foreach (string a in System.Environment.GetCommandLineArgs()) if (a == "-lmwSelfTest") return true;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent");
                return intent.Call<string>("getStringExtra", "lmw_selftest") == "1";
            }
            catch (System.Exception) { return false; }
#else
            return false;
#endif
        }

        private void Line(string s)
        {
            report.AppendLine(s);
            Debug.Log("[LMW SelfTest] " + s);
        }

        private void Update()
        {
            if (recordFrames) frameMs.Add(Time.unscaledDeltaTime * 1000f);
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(3f); // let the first scene settle
            MusicalAudioEngine e = Services.Engine;
            InstrumentLibrary lib = Services.Instruments;
            Line($"device={SystemInfo.deviceModel} os={SystemInfo.operatingSystem} gpu={SystemInfo.graphicsDeviceName} " +
                 $"api={SystemInfo.graphicsDeviceType} ram={SystemInfo.systemMemorySize}MB cpus={SystemInfo.processorCount} " +
                 $"64bit={System.IntPtr.Size == 8} tier={QualityTier.Current}");
            Line($"audio rate={AudioSettings.outputSampleRate} dsp={Services.DspBuffer()} voices={QualityTier.MaxVoices}");

            // 1. decode every instrument
            long mem0 = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            var sw = new Stopwatch();
            double total = 0;
            foreach (InstrumentDefinition d in lib.instruments)
            {
                sw.Restart();
                e.Load(d);
                sw.Stop();
                total += sw.Elapsed.TotalMilliseconds;
                Line($"load {d.id,-15} {sw.Elapsed.TotalMilliseconds,7:0.0} ms  zones={d.zones.Count}");
                yield return null;
            }
            long mem1 = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            Line($"load all: {total:0} ms, managed+native delta {(mem1 - mem0) / 1048576f:0.0} MB");

            recordFrames = true;
            // 2. polyphony burst: 40 piano notes in one frame
            InstrumentDefinition piano = lib.Get("GrandPiano");
            e.ResetStats();
            for (int i = 0; i < 40; i++) e.NoteOn(piano, 36 + i, 90 + (i % 3) * 10, 3);
            yield return new WaitForSecondsRealtime(2.5f);
            Report("polyphony 40 notes", e.ReadStats());
            e.StopAll();
            yield return new WaitForSecondsRealtime(1f);

            // 3. fast 3-track sequence (16th notes at 180 bpm) for 8 s
            Sequencer seq = gameObject.AddComponent<Sequencer>();
            var mel = new List<NoteEvent>();
            var drums = new List<NoteEvent>();
            var bass = new List<NoteEvent>();
            int[] scale = { 60, 62, 64, 65, 67, 69, 71, 72 };
            for (int i = 0; i < 96; i++)
            {
                mel.Add(new NoteEvent(i * 0.25f, 0.2f, scale[(i * 3) % 8], 100));
                drums.Add(new NoteEvent(i * 0.25f, 0f, i % 4 == 0 ? 36 : i % 4 == 2 ? 38 : 42, 100));
                if (i % 2 == 0) bass.Add(new NoteEvent(i * 0.25f, 0.45f, 36 + scale[i % 8] % 12, 90));
            }
            seq.AddTrack(lib.Get("DigitalKeyboard"), 4, mel);
            seq.AddTrack(lib.Get("DrumKit"), 5, drums);
            seq.AddTrack(lib.Get("Cello"), 6, bass);
            seq.Tempo = 180f;
            e.ResetStats();
            seq.Play();
            yield return new WaitForSecondsRealtime(8.5f);
            Report("sequence 3 tracks 16ths @180bpm", e.ReadStats());
            seq.Stop();

            // 4. held loops: violin + flute + cello chords for 4 s (loop seams under real playback)
            e.ResetStats();
            foreach (int n in new[] { 67, 71, 74 }) e.NoteOn(lib.Get("Violin"), n, 100, 7);
            foreach (int n in new[] { 72, 76 }) e.NoteOn(lib.Get("Flute"), n, 100, 8);
            e.NoteOn(lib.Get("Cello"), 48, 100, 9);
            yield return new WaitForSecondsRealtime(4f);
            Report("held loops (6 sustained notes)", e.ReadStats());
            e.StopAll();
            yield return new WaitForSecondsRealtime(0.5f);

            recordFrames = false;
            frameMs.Sort();
            if (frameMs.Count > 0)
            {
                float avg = 0f;
                foreach (float f in frameMs) avg += f;
                avg /= frameMs.Count;
                Line($"frames n={frameMs.Count} avg={avg:0.0}ms (={1000f / avg:0} fps) p95={frameMs[(int)(frameMs.Count * 0.95f)]:0.0}ms " +
                     $"max={frameMs[frameMs.Count - 1]:0.0}ms");
            }
            Line($"memory: allocated={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f:0}MB " +
                 $"reserved={UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576f:0}MB mono={UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576f:0}MB");
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "selftest.txt"), report.ToString());
            Line("DONE");
        }

        private void Report(string name, MusicalAudioEngine.Stats s)
        {
            Line($"{name}: buffers={s.buffers} buffer={s.bufferMs:0.00}ms mix avg={s.mixMsAverage:0.000}ms peak={s.mixMsPeak:0.000}ms " +
                 $"load={s.loadPercent:0.0}% peakVoices={s.peakVoices} limiterActiveBuffers={s.clippedBuffers} " +
                 $"scheduled={s.scheduledNotes} late={s.lateNotes} maxLate={s.maxLateSamples}smp");
        }
    }
}
