using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LittleMusicWorld.Audio
{
    /// <summary>
    /// The game's sampler. Every instrument note in the game (free play, guided melodies, Studio playback, Orchestra,
    /// mini-games) goes through this one mixer so that timing, voice limits and loudness are consistent.
    ///
    /// Design:
    ///  - Samples are decoded once per instrument into 16-bit arrays (<see cref="Load"/>), outside any play path.
    ///  - The main thread pushes fixed-size <see cref="Command"/> structs into a single-producer/single-consumer ring;
    ///    the audio thread drains it in <see cref="OnAudioFilterRead"/>. No locks, no allocations while playing.
    ///  - Notes may be scheduled at an exact DSP sample (<see cref="NoteOnAt"/>), so sequenced music is sample-accurate
    ///    regardless of frame rate.
    ///  - Voices: linear-interpolated resampling, attack/release envelopes, optional sustain loops, choke groups,
    ///    per-instrument voice caps and quietest/oldest voice stealing. A soft limiter prevents clipping.
    ///  - 16 mix channels (Studio tracks, Orchestra players, free play) each have volume and mute.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicalAudioEngine : MonoBehaviour
    {
        public const int ChannelCount = 16;
        public const int FreePlayChannel = 0;

        public static MusicalAudioEngine Instance { get; private set; }

        /// <summary>Raised on the main thread when an immediate note starts (visual feedback, recording).</summary>
        public event Action<InstrumentDefinition, int, int, int> NoteStarted; // instrument, note, velocity, channel
        public event Action<InstrumentDefinition, int, int> NoteStopped;      // instrument, note, channel

        [SerializeField] private int maxVoices = 40;
        [Range(0f, 1.5f)] [SerializeField] private float masterVolume = 0.9f;

        // ---------- loaded instrument data (shared with the audio thread by reference) ----------
        private sealed class ZoneData
        {
            public short[] pcm;
            public int length;
            public int rate;
            public int root;
            public float tune; // semitones
            public int loopStart, loopEnd;
            public float gain;
            public int lowNote, highNote, lowVel, highVel;
        }

        private sealed class Bank
        {
            public InstrumentDefinition def;
            public ZoneData[] zones;
            public float attack, release, volume;
            public bool oneShot, loop;
            public int maxVoices;
        }

        private readonly List<Bank> banks = new List<Bank>();
        private readonly Dictionary<InstrumentDefinition, int> bankIndex = new Dictionary<InstrumentDefinition, int>();
        private Bank[] bankArray = new Bank[0]; // snapshot read by the audio thread

        // ---------- command ring ----------
        private enum CmdType : byte { NoteOn, NoteOff, AllOff, ChannelOff }

        private struct Command
        {
            public CmdType type;
            public int bank, note, velocity, channel;
            public long atSample; // absolute DSP sample; 0 = now
            public float pan;
        }

        private const int RingSize = 1024;
        private readonly Command[] ring = new Command[RingSize];
        private int ringWrite, ringRead;

        // pending scheduled note-ons/offs (audio thread only)
        private readonly Command[] pending = new Command[512];
        private int pendingCount;

        // ---------- voices (audio thread only) ----------
        private struct Voice
        {
            public bool active;
            public ZoneData zone;
            public int bank, note, channel, choke;
            public double pos, step;
            public float gain, panL, panR;
            public float env, envTarget, envRate; // linear envelope
            public bool released, loop;
            public long startOrder;
            public int startDelay; // samples into the current buffer
        }

        private Voice[] voices;
        private long voiceOrder;
        private long dspSample; // absolute sample position at the start of the next buffer
        private int outputRate = 48000;
        private float limiterGain = 1f;

        private readonly float[] channelVolume = new float[ChannelCount];
        private readonly bool[] channelMute = new bool[ChannelCount];
        private volatile int activeVoiceCount;

        public int ActiveVoices => activeVoiceCount;
        public int OutputRate => outputRate;
        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp(value, 0f, 1.5f); }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            outputRate = AudioSettings.outputSampleRate;
            voices = new Voice[Mathf.Clamp(maxVoices, 8, 96)];
            for (int i = 0; i < ChannelCount; i++) channelVolume[i] = 1f;

            // The mixer writes into this source's buffer. A silent looping clip keeps the filter callback running.
            AudioSource src = GetComponent<AudioSource>();
            src.clip = AudioClip.Create("LMW_SamplerBus", 1024, 2, outputRate, false);
            src.loop = true;
            src.spatialBlend = 0f;
            src.priority = 0;
            src.bypassReverbZones = true;
            src.playOnAwake = true;
            src.Play();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMaxVoices(int count)
        {
            // Only safe before notes play (quality presets at boot).
            if (activeVoiceCount == 0) voices = new Voice[Mathf.Clamp(count, 8, 96)];
        }

        // =====================================================================================
        // Loading
        // =====================================================================================

        public bool IsLoaded(InstrumentDefinition def) => def != null && bankIndex.ContainsKey(def);

        /// <summary>Decodes an instrument's samples (main thread, may take a few ms per sample). Idempotent.</summary>
        public int Load(InstrumentDefinition def)
        {
            if (def == null) return -1;
            if (bankIndex.TryGetValue(def, out int existing)) return existing;
            var zones = new List<ZoneData>(def.zones.Count);
            foreach (SampleZone z in def.zones)
            {
                if (z.clip == null) continue;
                AudioClip c = z.clip;
                if (c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData();
                int len = c.samples;
                int ch = c.channels;
                var tmp = new float[len * ch];
                if (!c.GetData(tmp, 0))
                {
                    Debug.LogWarning($"[LMW Audio] could not read {c.name}");
                    continue;
                }
                var pcm = new short[len + 2]; // +2 guard samples for interpolation
                for (int i = 0; i < len; i++)
                {
                    float v = ch == 1 ? tmp[i] : (tmp[i * ch] + tmp[i * ch + 1]) * 0.5f;
                    pcm[i] = (short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767);
                }
                c.UnloadAudioData();
                zones.Add(new ZoneData
                {
                    pcm = pcm, length = len, rate = c.frequency, root = z.rootNote, tune = z.tuneCents / 100f,
                    loopStart = z.loopStart, loopEnd = Math.Min(z.loopEnd, len - 1), gain = z.gain,
                    lowNote = z.lowNote, highNote = z.highNote, lowVel = z.lowVelocity, highVel = z.highVelocity
                });
            }

            var bank = new Bank
            {
                def = def, zones = zones.ToArray(), attack = Mathf.Max(0.001f, def.attack),
                release = Mathf.Max(0.005f, def.release), volume = def.volume, oneShot = def.oneShot,
                loop = def.sustainLoop, maxVoices = Mathf.Max(1, def.maxVoices)
            };
            banks.Add(bank);
            int index = banks.Count - 1;
            bankIndex[def] = index;
            bankArray = banks.ToArray(); // atomic reference swap
            return index;
        }

        /// <summary>Plays a short silent note so the first real touch has no first-use cost.</summary>
        public void Warm(InstrumentDefinition def) => Load(def);

        // =====================================================================================
        // Main-thread API
        // =====================================================================================

        public double DspTime => AudioSettings.dspTime;

        /// <summary>Converts an absolute DSP time (seconds) to the engine's sample clock.</summary>
        public long ToSample(double dspTime) => (long)Math.Round(dspTime * outputRate);

        public void NoteOn(InstrumentDefinition def, int note, int velocity = 100, int channel = FreePlayChannel, float pan = 0f)
        {
            int b = Load(def);
            if (b < 0) return;
            Push(new Command { type = CmdType.NoteOn, bank = b, note = note, velocity = velocity, channel = channel, pan = pan });
            NoteStarted?.Invoke(def, note, velocity, channel);
        }

        public void NoteOff(InstrumentDefinition def, int note, int channel = FreePlayChannel)
        {
            if (def == null || !bankIndex.TryGetValue(def, out int b)) return;
            Push(new Command { type = CmdType.NoteOff, bank = b, note = note, channel = channel });
            NoteStopped?.Invoke(def, note, channel);
        }

        /// <summary>Schedules a note at an absolute DSP time (from a sequencer). No main-thread event is raised.</summary>
        public void NoteOnAt(InstrumentDefinition def, int note, int velocity, int channel, double dspTime, float pan = 0f)
        {
            int b = Load(def);
            if (b < 0) return;
            Push(new Command { type = CmdType.NoteOn, bank = b, note = note, velocity = velocity, channel = channel, pan = pan, atSample = ToSample(dspTime) });
        }

        public void NoteOffAt(InstrumentDefinition def, int note, int channel, double dspTime)
        {
            if (def == null || !bankIndex.TryGetValue(def, out int b)) return;
            Push(new Command { type = CmdType.NoteOff, bank = b, note = note, channel = channel, atSample = ToSample(dspTime) });
        }

        public void StopChannel(int channel) => Push(new Command { type = CmdType.ChannelOff, channel = channel });
        public void StopAll() => Push(new Command { type = CmdType.AllOff });

        public void SetChannelVolume(int channel, float volume)
        {
            if (channel >= 0 && channel < ChannelCount) channelVolume[channel] = Mathf.Clamp01(volume);
        }

        public void SetChannelMute(int channel, bool mute)
        {
            if (channel >= 0 && channel < ChannelCount) channelMute[channel] = mute;
        }

        public float GetChannelVolume(int channel) => channel >= 0 && channel < ChannelCount ? channelVolume[channel] : 0f;
        public bool GetChannelMute(int channel) => channel >= 0 && channel < ChannelCount && channelMute[channel];

        private void Push(Command c)
        {
            int w = ringWrite;
            int next = (w + 1) & (RingSize - 1);
            if (next == Volatile.Read(ref ringRead)) return; // full: drop (never blocks the game)
            ring[w] = c;
            Volatile.Write(ref ringWrite, next);
        }

        // =====================================================================================
        // Audio thread
        // =====================================================================================

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (voices == null) return;
            int frames = data.Length / channels;
            long bufferStart = dspSample;
            long bufferEnd = bufferStart + frames;
            Bank[] bk = bankArray;

            // 1. drain the ring into immediate actions or the pending list
            int r = ringRead;
            int w = Volatile.Read(ref ringWrite);
            while (r != w)
            {
                Command c = ring[r];
                r = (r + 1) & (RingSize - 1);
                if (c.atSample <= 0 || c.atSample < bufferEnd) Execute(ref c, bk, (int)Math.Max(0, c.atSample - bufferStart));
                else if (pendingCount < pending.Length) pending[pendingCount++] = c;
            }
            Volatile.Write(ref ringRead, r);

            // 2. due scheduled commands
            for (int i = 0; i < pendingCount;)
            {
                if (pending[i].atSample < bufferEnd)
                {
                    Execute(ref pending[i], bk, (int)Math.Max(0, pending[i].atSample - bufferStart));
                    pending[i] = pending[--pendingCount];
                }
                else i++;
            }

            // 3. mix
            float master = masterVolume;
            int active = 0;
            for (int v = 0; v < voices.Length; v++)
            {
                if (!voices[v].active) continue;
                active++;
                MixVoice(ref voices[v], data, channels, frames, master);
            }
            activeVoiceCount = active;

            // 4. soft limiter (fast attack, slow release) keeps chords from clipping
            for (int f = 0; f < frames; f++)
            {
                int i0 = f * channels;
                float peak = 0f;
                for (int c = 0; c < channels; c++) peak = Math.Max(peak, Math.Abs(data[i0 + c]));
                float wanted = peak * limiterGain > 0.92f ? 0.92f / peak : 1f;
                limiterGain = wanted < limiterGain ? wanted : limiterGain + (1f - limiterGain) * 0.0002f;
                for (int c = 0; c < channels; c++)
                {
                    float s = data[i0 + c] * limiterGain;
                    data[i0 + c] = s > 1f ? 1f : s < -1f ? -1f : s;
                }
            }

            dspSample = bufferEnd;
        }

        private void Execute(ref Command c, Bank[] bk, int offset)
        {
            switch (c.type)
            {
                case CmdType.NoteOn: StartVoice(ref c, bk, offset); break;
                case CmdType.NoteOff:
                    for (int i = 0; i < voices.Length; i++)
                        if (voices[i].active && !voices[i].released && voices[i].bank == c.bank && voices[i].note == c.note && voices[i].channel == c.channel)
                            if (!bk[c.bank].oneShot) Release(ref voices[i], bk[c.bank].release);
                    break;
                case CmdType.ChannelOff:
                    for (int i = 0; i < voices.Length; i++)
                        if (voices[i].active && voices[i].channel == c.channel) Release(ref voices[i], 0.08f);
                    RemovePending(c.channel);
                    break;
                case CmdType.AllOff:
                    for (int i = 0; i < voices.Length; i++) if (voices[i].active) Release(ref voices[i], 0.08f);
                    pendingCount = 0;
                    break;
            }
        }

        private void RemovePending(int channel)
        {
            for (int i = 0; i < pendingCount;)
            {
                if (pending[i].channel == channel) pending[i] = pending[--pendingCount];
                else i++;
            }
        }

        private void Release(ref Voice v, float seconds)
        {
            v.released = true;
            v.envTarget = 0f;
            v.envRate = -Math.Max(v.env, 0.001f) / Math.Max(1f, seconds * outputRate);
        }

        private void StartVoice(ref Command c, Bank[] bk, int offset)
        {
            if (c.bank < 0 || c.bank >= bk.Length) return;
            Bank bank = bk[c.bank];
            ZoneData z = PickZone(bank, c.note, c.velocity);
            if (z == null) return;

            int choke = bank.def.ChokeGroupOf(c.note);
            int sameInstrument = 0, oldestSame = -1;
            long oldestSameOrder = long.MaxValue;
            for (int i = 0; i < voices.Length; i++)
            {
                ref Voice o = ref voices[i];
                if (!o.active || o.bank != c.bank || o.channel != c.channel) continue;
                // Re-striking a ringing key: fade the old voice quickly (like a real piano damper re-strike).
                if (o.note == c.note && !o.released) Release(ref o, 0.05f);
                else if (choke != 0 && o.choke == choke) Release(ref o, 0.03f);
                sameInstrument++;
                if (o.startOrder < oldestSameOrder) { oldestSameOrder = o.startOrder; oldestSame = i; }
            }
            if (sameInstrument >= bank.maxVoices && oldestSame >= 0) Release(ref voices[oldestSame], 0.03f);

            int slot = FindSlot();
            ref Voice v = ref voices[slot];
            float vel = Mathf.Clamp01(c.velocity / 127f);
            float velGain = 0.25f + 0.75f * vel * vel;
            float pan = Mathf.Clamp(c.pan, -1f, 1f);
            v.active = true;
            v.zone = z;
            v.bank = c.bank;
            v.note = c.note;
            v.channel = c.channel;
            v.choke = choke;
            v.pos = 0;
            v.step = Math.Pow(2.0, (c.note - z.root - z.tune) / 12.0) * z.rate / outputRate;
            v.gain = velGain * z.gain * bank.volume;
            v.panL = Mathf.Sqrt(0.5f * (1f - pan));
            v.panR = Mathf.Sqrt(0.5f * (1f + pan));
            v.env = 0f;
            v.envTarget = 1f;
            v.envRate = 1f / Math.Max(1f, bank.attack * outputRate);
            v.released = false;
            v.loop = bank.loop && z.loopEnd > z.loopStart + 64;
            v.startOrder = ++voiceOrder;
            v.startDelay = offset;
        }

        private static ZoneData PickZone(Bank bank, int note, int velocity)
        {
            ZoneData best = null;
            int bestD = int.MaxValue;
            ZoneData[] zs = bank.zones;
            for (int i = 0; i < zs.Length; i++)
            {
                ZoneData z = zs[i];
                if (velocity < z.lowVel || velocity > z.highVel) continue;
                if (note >= z.lowNote && note <= z.highNote) return z;
                int d = Math.Min(Math.Abs(note - z.lowNote), Math.Abs(note - z.highNote));
                if (d < bestD) { bestD = d; best = z; }
            }
            if (best == null && zs.Length > 0) best = zs[0];
            return best;
        }

        private int FindSlot()
        {
            int quietest = -1, oldest = 0;
            float quietestEnv = float.MaxValue;
            long oldestOrder = long.MaxValue;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].active) return i;
                if (voices[i].released && voices[i].env < quietestEnv) { quietestEnv = voices[i].env; quietest = i; }
                if (voices[i].startOrder < oldestOrder) { oldestOrder = voices[i].startOrder; oldest = i; }
            }
            return quietest >= 0 ? quietest : oldest;
        }

        private void MixVoice(ref Voice v, float[] data, int channels, int frames, float master)
        {
            ZoneData z = v.zone;
            short[] pcm = z.pcm;
            int ch = v.channel;
            float chGain = channelMute[ch] ? 0f : channelVolume[ch];
            float g = v.gain * master * chGain * (1f / 32768f);
            float gl = g * v.panL, gr = g * v.panR;
            int f = v.startDelay;
            v.startDelay = 0;
            double pos = v.pos, step = v.step;
            float env = v.env, rate = v.envRate, target = v.envTarget;
            int len = z.length;
            int loopStart = z.loopStart, loopEnd = z.loopEnd;
            bool loop = v.loop; // sustain loops keep cycling through the release fade (samples end at the loop)

            for (; f < frames; f++)
            {
                int i = (int)pos;
                if (loop && i >= loopEnd)
                {
                    pos -= loopEnd - loopStart;
                    i = (int)pos;
                }
                if (i >= len - 1) { v.active = false; break; }
                float frac = (float)(pos - i);
                float s = pcm[i] + (pcm[i + 1] - pcm[i]) * frac;

                env += rate;
                if (rate > 0f && env >= target) { env = target; rate = 0f; }
                else if (rate < 0f && env <= 0f) { v.active = false; break; }

                float o = s * env;
                int k = f * channels;
                if (channels >= 2)
                {
                    data[k] += o * gl;
                    data[k + 1] += o * gr;
                }
                else data[k] += o * (gl + gr) * 0.7071f;
                pos += step;
            }

            // Natural end of a sample without a loop: short fade avoids a click on truncated tails.
            v.pos = pos;
            v.env = env;
            v.envRate = rate;
            if (v.active && !loop && (int)pos >= len - 1 - (int)(0.01f * outputRate * step)) Release(ref v, 0.01f);
        }

        // Editor/testing helpers ---------------------------------------------------------------
        public int LoadedInstrumentCount => banks.Count;
    }
}
