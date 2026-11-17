using LittleMusicWorld.Audio;
using NUnit.Framework;
using UnityEngine;

namespace LittleMusicWorld.Tests
{
    /// <summary>Runs the real sampler mixer offline on synthetic samples (no audio device needed).</summary>
    public class EngineTests
    {
        private const int Rate = 48000;
        private GameObject go;
        private MusicalAudioEngine engine;
        private InstrumentDefinition sine, drum;

        [SetUp] public void SetUp()
        {
            go = new GameObject("engine test");
            engine = go.AddComponent<MusicalAudioEngine>();
            engine.InitForTest(Rate, 32);
            sine = Make("Sine", 69, 2.0f, true, false, 440f);
            drum = Make("Drum", 36, 0.3f, false, true, 0f);
        }

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(sine);
            Object.DestroyImmediate(drum);
        }

        private static InstrumentDefinition Make(string id, int root, float seconds, bool loop, bool oneShot, float freq)
        {
            int n = (int)(seconds * Rate);
            var pcm = new float[n];
            var rnd = new System.Random(1);
            for (int i = 0; i < n; i++)
                pcm[i] = freq > 0 ? 0.8f * Mathf.Sin(2 * Mathf.PI * freq * i / Rate) : (float)(rnd.NextDouble() * 2 - 1) * Mathf.Exp(-i / (0.05f * Rate));
            AudioClip clip = AudioClip.Create(id, n, 1, Rate, false);
            clip.SetData(pcm, 0);
            var d = ScriptableObject.CreateInstance<InstrumentDefinition>();
            d.id = id;
            d.sustainLoop = loop;
            d.oneShot = oneShot;
            d.attack = 0.002f;
            d.release = 0.05f;
            d.maxVoices = 8;
            int period = Mathf.RoundToInt(Rate / 440f * 40); // 40 whole cycles of 440 Hz
            d.zones.Add(new SampleZone { clip = clip, rootNote = root, loopStart = loop ? Rate / 2 : -1, loopEnd = loop ? Rate / 2 + period : -1 });
            return d;
        }

        private float[] Run(ref long t, int buffers, int frames = 512)
        {
            var all = new float[buffers * frames * 2];
            var buf = new float[frames * 2];
            for (int b = 0; b < buffers; b++)
            {
                System.Array.Clear(buf, 0, buf.Length);
                engine.Render(buf, 2, t);
                t += frames;
                System.Array.Copy(buf, 0, all, b * buf.Length, buf.Length);
            }
            return all;
        }

        private static float Peak(float[] a, int from = 0, int to = -1)
        {
            float p = 0;
            for (int i = from; i < (to < 0 ? a.Length : to); i++) p = Mathf.Max(p, Mathf.Abs(a[i]));
            return p;
        }

        [Test] public void NoteOnProducesSoundAtThePitchRequested()
        {
            long t = 0;
            engine.NoteOn(sine, 81, 127); // an octave above the 440 Hz root → 880 Hz
            float[] o = Run(ref t, 20);
            Assert.Greater(Peak(o), 0.05f);
            // count zero crossings on the left channel over 0.1 s
            int crossings = 0;
            for (int i = 2 * 2000; i < 2 * 6800; i += 2) if (o[i] <= 0 && o[i + 2] > 0) crossings++;
            Assert.AreEqual(88f, crossings, 2f, "≈880 Hz");
            Assert.AreEqual(1, engine.ActiveVoices);
        }

        [Test] public void HeldLoopSustainsAndReleaseEndsCleanly()
        {
            long t = 0;
            engine.NoteOn(sine, 69, 100);
            Run(ref t, 300); // 3.2 s: longer than the 2 s sample, so the loop must be cycling
            Assert.AreEqual(1, engine.ActiveVoices, "loop keeps the note alive");
            engine.NoteOff(sine, 69);
            float[] tail = Run(ref t, 20);
            Assert.AreEqual(0, engine.ActiveVoices, "released voice finishes");
            Assert.Less(Peak(tail, tail.Length - 512), 1e-4f, "silence after release");
        }

        [Test] public void ManyVoicesNeverClipAndRespectTheVoiceCap()
        {
            long t = 0;
            for (int i = 0; i < 40; i++) engine.NoteOn(sine, 50 + i, 127, i % 4);
            float[] o = Run(ref t, 40);
            Assert.LessOrEqual(Peak(o), 1f);
            Assert.LessOrEqual(engine.ActiveVoices, 32);
            foreach (float v in o) Assert.IsFalse(float.IsNaN(v));
        }

        [Test] public void ScheduledNotesStartOnTheExactSample()
        {
            long t = 0;
            Run(ref t, 4);                      // t = 2048
            long when = 2048 + 300;             // inside the next buffer
            engine.NoteOnAt(drum, 36, 127, 1, when / (double)Rate);
            float[] o = Run(ref t, 1);
            for (int f = 0; f < 300; f++) Assert.AreEqual(0f, o[f * 2], "silent before the scheduled sample");
            Assert.Greater(Peak(o, 300 * 2 + 2), 0f);
            Assert.AreEqual(0, engine.ReadStats().lateNotes);
        }

        [Test] public void OneShotIgnoresNoteOffAndChannelStopSilences()
        {
            long t = 0;
            engine.NoteOn(drum, 36, 127, 2);
            engine.NoteOff(drum, 36, 2);
            Run(ref t, 2);
            Assert.AreEqual(1, engine.ActiveVoices, "drums ring out after note-off");
            engine.NoteOn(sine, 60, 100, 3);
            engine.StopChannel(3);
            Run(ref t, 40); // 0.43 s: the 0.3 s drum has ended and the stopped channel faded out
            Assert.AreEqual(0, engine.ActiveVoices);
        }

        [Test] public void MutedChannelIsSilent()
        {
            long t = 0;
            engine.SetChannelMute(5, true);
            engine.NoteOn(sine, 69, 127, 5);
            Assert.AreEqual(0f, Peak(Run(ref t, 10)));
        }
    }
}
