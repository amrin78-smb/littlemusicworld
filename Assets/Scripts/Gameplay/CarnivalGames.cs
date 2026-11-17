using System.Collections;
using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// Base for Rhythm Carnival mini-games. Each game builds its own big, readable UI inside <see cref="Stage"/>, runs as
    /// a coroutine and reports <see cref="Succeeded"/>. No timers that end a game, no failure screens: a miss becomes a
    /// demonstration and another try. Difficulty rises one gentle step per success (saved per game).
    /// </summary>
    public abstract class MiniGame : MonoBehaviour
    {
        public RectTransform Stage { get; set; }
        public CarnivalManager Host { get; set; }
        public int Level { get; set; }
        public abstract string Id { get; }
        public abstract string IntroVoice { get; }
        public bool Succeeded { get; protected set; }
        public bool Finished { get; protected set; }

        protected MusicalAudioEngine E => Services.Engine;
        protected InstrumentDefinition I(string id) => Services.Instruments.Get(id);
        protected readonly List<GameObject> built = new List<GameObject>();
        protected const int Ch = 10;

        public abstract IEnumerator Run();

        public virtual void Cleanup()
        {
            foreach (GameObject g in built) if (g != null) Destroy(g);
            built.Clear();
            E?.StopChannel(Ch);
        }

        protected void Win()
        {
            Succeeded = true;
            Finished = true;
        }

        protected Button Btn(Vector2 pos, Vector2 size, Color c, Sprite icon, System.Action onTap, Sprite body = null)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback));
            go.transform.SetParent(Stage, false);
            built.Add(go);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = body ?? UISkin.Instance.pebble;
            img.type = img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = c;
            if (icon != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                ic.transform.SetParent(go.transform, false);
                ((RectTransform)ic.transform).sizeDelta = size * 0.62f;
                var ii = ic.GetComponent<Image>();
                ii.sprite = icon;
                ii.preserveAspect = true;
                ii.raycastTarget = false;
            }
            var b = go.GetComponent<Button>();
            b.transition = Selectable.Transition.None;
            if (onTap != null) b.onClick.AddListener(() => onTap());
            return b;
        }

        protected Image Img(Vector2 pos, Vector2 size, Color c, Sprite s)
        {
            var go = new GameObject("Img", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Stage, false);
            built.Add(go);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var i = go.GetComponent<Image>();
            i.sprite = s;
            i.color = c;
            i.raycastTarget = false;
            return i;
        }

        protected void Pulse(Component c, float amount = 0.18f) => Tween.Punch(c.transform, amount, 0.25f);

        protected void Note(InstrumentDefinition d, int note, double at, float len = 0.4f, int vel = 100)
        {
            E?.NoteOnAt(d, note, vel, Ch, at);
            if (len > 0) E?.NoteOffAt(d, note, Ch, at + len);
        }

        protected static IEnumerator Until(double dspTime)
        {
            while (AudioSettings.dspTime < dspTime) yield return null;
        }
    }

    // ===================================================================================== 1. Rhythm Match
    public sealed class RhythmMatchGame : MiniGame
    {
        public override string Id => "rhythm_match";
        public override string IntroVoice => "game_rhythm";
        private readonly List<double> taps = new List<double>();
        private Button drum;

        private float[] Pattern()
        {
            var r = new System.Random(Level * 31 + 7);
            int hits = Mathf.Min(3 + Level / 2, 6);
            var beats = new List<float> { 0f };
            float step = Level < 2 ? 1f : 0.5f;
            while (beats.Count < hits)
            {
                float b = beats[beats.Count - 1] + step * (r.Next(Level < 2 ? 1 : 1, 3));
                if (b > 3.5f) break;
                beats.Add(b);
            }
            return beats.ToArray();
        }

        public override IEnumerator Run()
        {
            InstrumentDefinition bongos = I("Congas");
            drum = Btn(new Vector2(0, -40), new Vector2(420, 420), new Color(0.71f, 0.4f, 0.11f), null, Tap, UISkin.Instance.circle);
            Img(new Vector2(0, -40), new Vector2(330, 330), new Color(1f, 0.96f, 0.88f), UISkin.Instance.circle);
            float[] p = Pattern();
            float tempo = 90f;
            while (!Finished)
            {
                taps.Clear();
                Guide.Say("game_listen");
                yield return new WaitForSeconds(1.0f);
                double start = AudioSettings.dspTime + 0.2;
                double beat = 60.0 / tempo;
                foreach (float b in p) Note(bongos, 64, start + b * beat, 0);
                foreach (float b in p) { yield return Until(start + b * beat); Pulse(drum); Host.MeloReact(); }
                yield return new WaitForSeconds(0.6f);
                Guide.Say("game_your_turn");
                taps.Clear();
                listening = true;
                float wait = 0f;
                while (taps.Count < p.Length && wait < 8f) { wait += Time.deltaTime; yield return null; }
                listening = false;
                yield return new WaitForSeconds(0.5f);
                if (Matches(p, beat)) { Win(); yield break; }
                Guide.Say("game_again");
                yield return new WaitForSeconds(1.2f);
            }
        }

        private bool listening;

        private void Tap()
        {
            E?.NoteOn(I("Congas"), 64, 105, Ch);
            Pulse(drum);
            if (listening) taps.Add(AudioSettings.dspTime);
        }

        /// <summary>Same number of hits and each gap within ±35 % of the model (or ±0.18 s) — kind to small hands.</summary>
        private bool Matches(float[] pattern, double beat)
        {
            if (taps.Count != pattern.Length) return false;
            for (int i = 1; i < pattern.Length; i++)
            {
                double want = (pattern[i] - pattern[i - 1]) * beat;
                double got = taps[i] - taps[i - 1];
                if (System.Math.Abs(got - want) > System.Math.Max(0.18, want * 0.35)) return false;
            }
            return true;
        }
    }

    // ===================================================================================== 2. Sound Detective
    public sealed class SoundDetectiveGame : MiniGame
    {
        public override string Id => "sound_detective";
        public override string IntroVoice => "game_detective";
        private static readonly string[][] Groups =
        {
            new[] { "GrandPiano", "DrumKit", "Trumpet", "Violin", "AcousticGuitar", "Flute" },          // very different
            new[] { "Xylophone", "Harp", "Saxophone", "Cello", "Banjo", "Clarinet", "Congas" },
            new[] { "Violin", "Cello", "Flute", "Recorder", "Trumpet", "Trombone", "AcousticGuitar", "Ukulele" }, // close cousins
        };

        public override IEnumerator Run()
        {
            var rnd = new System.Random(System.Environment.TickCount);
            string[] pool = Groups[Mathf.Min(Level / 2, Groups.Length - 1)];
            var options = new List<string>(pool);
            for (int i = options.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (options[i], options[j]) = (options[j], options[i]); }
            options = options.GetRange(0, Mathf.Min(3, options.Count));
            string answer = options[rnd.Next(options.Count)];
            bool done = false;
            for (int i = 0; i < options.Count; i++)
            {
                string id = options[i];
                Button b = null;
                b = Btn(new Vector2((i - 1) * 420, -60), new Vector2(340, 340), Color.Lerp(I(id).color, Color.white, 0.6f), Host.Portrait(id), () =>
                {
                    if (done) return;
                    if (id == answer) { done = true; Pulse(b, 0.3f); Win(); }
                    else { Phrase(id); Pulse(b, 0.1f); Guide.Say("game_try_other"); }
                });
            }
            Btn(new Vector2(0, 250), new Vector2(170, 150), new Color(0.36f, 0.49f, 0.98f), UISkin.Instance.iconListen, () => Phrase(answer));
            yield return new WaitForSeconds(0.8f);
            Phrase(answer);
            while (!done) yield return null;
        }

        private void Phrase(string id)
        {
            InstrumentDefinition d = I(id);
            E?.Load(d);
            double t = AudioSettings.dspTime + 0.1;
            if (d.playStyle == PlayStyle.DrumPads)
            {
                for (int k = 0; k < 6; k++) Note(d, d.zones[k % d.zones.Count].rootNote, t + k * 0.25, 0);
                return;
            }
            int[] tune = { 0, 4, 7, 12, 7 };
            int baseNote = d.playableNotes.Length > 0 ? d.playableNotes[0] : 60;
            for (int k = 0; k < tune.Length; k++) Note(d, baseNote + tune[k], t + k * 0.32, 0.3f);
        }
    }

    // ===================================================================================== 3. Follow the Beat
    public sealed class FollowBeatGame : MiniGame
    {
        public override string Id => "follow_beat";
        public override string IntroVoice => "game_beat";
        private readonly List<double> taps = new List<double>();
        private Button drum;

        public override IEnumerator Run()
        {
            drum = Btn(new Vector2(0, -40), new Vector2(440, 440), new Color(0.89f, 0.33f, 0.25f), null, () =>
            {
                E?.NoteOn(I("BassDrum"), 36, 110, Ch);
                taps.Add(AudioSettings.dspTime);
                Pulse(drum, 0.12f);
            }, UISkin.Instance.circle);
            Image skin = Img(new Vector2(0, -40), new Vector2(350, 350), new Color(1f, 0.96f, 0.88f), UISkin.Instance.circle);
            float tempo = 70f + Mathf.Min(Level, 6) * 8f;
            double beat = 60.0 / tempo;
            while (!Finished)
            {
                taps.Clear();
                double start = AudioSettings.dspTime + 0.5;
                InstrumentDefinition kit = I("DrumKit");
                for (int b = 0; b < 12; b++) Note(kit, b < 4 ? 38 : 42, start + b * beat, 0, b < 4 ? 80 : 70); // 4 count-in + 8 beats
                for (int b = 0; b < 12; b++)
                {
                    yield return Until(start + b * beat);
                    Pulse(skin, b < 4 ? 0.08f : 0.16f);
                    if (b == 3) Guide.Say("game_tap_with");
                }
                yield return new WaitForSeconds((float)beat);
                int good = 0;
                for (int b = 4; b < 12; b++)
                {
                    double t = start + b * beat;
                    foreach (double tap in taps) if (System.Math.Abs(tap - t) < 0.18) { good++; break; }
                }
                Host.ShowScore(good, 8);
                if (good >= 5) { Win(); yield break; }
                Guide.Say("game_again");
                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    // ===================================================================================== 4. Musical Memory
    public sealed class MemoryGame : MiniGame
    {
        public override string Id => "memory";
        public override string IntroVoice => "game_memory";
        private static readonly int[] Bells = { 72, 76, 79, 84 };
        private readonly List<int> sequence = new List<int>();
        private readonly List<int> entered = new List<int>();
        private Button[] buttons;
        private bool input;

        public override IEnumerator Run()
        {
            InstrumentDefinition x = I("Xylophone");
            buttons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                buttons[i] = Btn(new Vector2((i - 1.5f) * 330, -60), new Vector2(280, 280), NoteUtil.ColorOf(Bells[i]), null, () => Press(k),
                    UISkin.Instance.circle);
            }
            var rnd = new System.Random(System.Environment.TickCount);
            int length = 2 + Mathf.Min(Level, 5);
            for (int i = 0; i < length; i++) sequence.Add(rnd.Next(4));
            while (!Finished)
            {
                input = false;
                Guide.Say("game_listen");
                yield return new WaitForSeconds(1.0f);
                double t = AudioSettings.dspTime + 0.1;
                for (int i = 0; i < sequence.Count; i++) Note(x, Bells[sequence[i]], t + i * 0.55, 0);
                for (int i = 0; i < sequence.Count; i++) { yield return Until(t + i * 0.55); Pulse(buttons[sequence[i]], 0.25f); }
                yield return new WaitForSeconds(0.5f);
                Guide.Say("game_your_turn");
                entered.Clear();
                input = true;
                while (entered.Count < sequence.Count && Matching()) yield return null;
                input = false;
                if (Matching()) { Win(); yield break; }
                Guide.Say("game_again");
                yield return new WaitForSeconds(1.2f);
            }
        }

        private bool Matching()
        {
            for (int i = 0; i < entered.Count; i++) if (entered[i] != sequence[i]) return false;
            return true;
        }

        private void Press(int k)
        {
            E?.NoteOn(I("Xylophone"), Bells[k], 105, Ch);
            Pulse(buttons[k], 0.2f);
            if (input) entered.Add(k);
        }
    }

    // ===================================================================================== 5. High or Low
    public sealed class HighLowGame : MiniGame
    {
        public override string Id => "high_low";
        public override string IntroVoice => "game_highlow";

        public override IEnumerator Run()
        {
            InstrumentDefinition piano = I("GrandPiano");
            var rnd = new System.Random(System.Environment.TickCount);
            int[] intervals = { 12, 9, 7, 5, 4, 3, 2 };
            int interval = intervals[Mathf.Min(Level, intervals.Length - 1)];
            bool higher = rnd.Next(2) == 0;
            int first = 60 + rnd.Next(-3, 4);
            int second = higher ? first + interval : first - interval;
            bool done = false;
            Button up = null, down = null;
            up = Btn(new Vector2(-260, -60), new Vector2(330, 330), new Color(0.36f, 0.49f, 0.98f), UISkin.Instance.iconUp, () => Answer(true));
            down = Btn(new Vector2(260, -60), new Vector2(330, 330), new Color(1f, 0.65f, 0.17f), UISkin.Instance.iconDown, () => Answer(false));
            Btn(new Vector2(0, 250), new Vector2(170, 150), new Color(0.55f, 0.83f, 0.27f), UISkin.Instance.iconListen, Play);
            void Play()
            {
                double t = AudioSettings.dspTime + 0.1;
                Note(piano, first, t, 0.6f);
                Note(piano, second, t + 0.75, 0.8f);
            }
            void Answer(bool saidHigher)
            {
                if (done) return;
                if (saidHigher == higher) { done = true; Pulse(saidHigher ? up : down, 0.3f); Win(); }
                else { Guide.Say("game_listen_again"); Play(); }
            }
            yield return new WaitForSeconds(0.8f);
            Play();
            while (!done) yield return null;
        }
    }

    // ===================================================================================== 6. Instrument Orchestra
    public sealed class WhoIsPlayingGame : MiniGame
    {
        public override string Id => "who_playing";
        public override string IntroVoice => "game_orchestra";
        private static readonly string[] Pool = { "GrandPiano", "DrumKit", "Violin", "Trumpet", "Flute", "AcousticGuitar", "Xylophone", "Cello" };

        public override IEnumerator Run()
        {
            var rnd = new System.Random(System.Environment.TickCount);
            var options = new List<string>(Pool);
            for (int i = options.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (options[i], options[j]) = (options[j], options[i]); }
            options = options.GetRange(0, 4);
            int count = Level < 2 ? 2 : 3;
            var answer = new HashSet<string>(options.GetRange(0, count));
            var chosen = new HashSet<string>();
            options.Sort((a, b) => rnd.Next(3) - 1);
            bool done = false;
            var cards = new Dictionary<string, Button>();
            for (int i = 0; i < options.Count; i++)
            {
                string id = options[i];
                Button b = null;
                b = Btn(new Vector2((i - 1.5f) * 330, -40), new Vector2(290, 290), Color.white, Host.Portrait(id), () =>
                {
                    if (!chosen.Add(id)) chosen.Remove(id);
                    b.GetComponent<Image>().color = chosen.Contains(id) ? new Color(1f, 0.85f, 0.3f) : Color.white;
                    Pulse(b, 0.12f);
                });
                cards[id] = b;
            }
            Btn(new Vector2(-120, 260), new Vector2(170, 150), new Color(0.36f, 0.49f, 0.98f), UISkin.Instance.iconListen, () => PlayTogether(answer));
            Btn(new Vector2(120, 260), new Vector2(170, 150), new Color(0.55f, 0.83f, 0.27f), UISkin.Instance.iconCheck, () =>
            {
                if (done) return;
                if (chosen.SetEquals(answer)) { done = true; Win(); }
                else StartCoroutine(Reveal(answer, cards));
            });
            yield return new WaitForSeconds(0.8f);
            PlayTogether(answer);
            while (!done) yield return null;
        }

        private void PlayTogether(IEnumerable<string> ids)
        {
            double t = AudioSettings.dspTime + 0.15;
            SongLibrary.Song s = SongLibrary.Get("odetojoy");
            bool melody = false;
            foreach (string id in ids)
            {
                InstrumentDefinition d = I(id);
                E?.Load(d);
                Arranger.Role role = Arranger.RoleFor(d, melody);
                melody |= role == Arranger.Role.Melody;
                foreach (NoteEvent e in Arranger.Part(s, d, role))
                {
                    if (e.beat >= 8f) break;
                    Note(d, e.note, t + e.beat * 0.5, e.length > 0 ? e.length * 0.5f : 0, e.velocity);
                }
            }
        }

        private IEnumerator Reveal(HashSet<string> answer, Dictionary<string, Button> cards)
        {
            Guide.Say("game_listen_again");
            foreach (string id in answer)
            {
                Pulse(cards[id], 0.3f);
                PlayTogether(new[] { id });
                yield return new WaitForSeconds(2.2f);
            }
        }
    }

    // ===================================================================================== 7. Melody Builder
    public sealed class MelodyBuilderGame : MiniGame
    {
        public override string Id => "melody_builder";
        public override string IntroVoice => "game_builder";
        private static readonly int[] Palette = { 60, 62, 64, 67, 69, 72 }; // C major pentatonic: every order sounds pleasant
        private readonly List<int> slots = new List<int>();
        private Image[] slotImages;
        private int slotCount;

        public override IEnumerator Run()
        {
            slotCount = Mathf.Min(4 + Level, 8);
            slotImages = new Image[slotCount];
            float w = Mathf.Min(170f, 1500f / slotCount);
            for (int i = 0; i < slotCount; i++)
                slotImages[i] = Img(new Vector2((i - (slotCount - 1) * 0.5f) * w, 170), new Vector2(w * 0.85f, w * 0.85f), new Color(1, 1, 1, 0.5f), UISkin.Instance.circle);
            for (int i = 0; i < Palette.Length; i++)
            {
                int n = Palette[i];
                Btn(new Vector2((i - 2.5f) * 230, -170), new Vector2(200, 200), NoteUtil.ColorOf(n), UISkin.Instance.note, () => Add(n), UISkin.Instance.circle);
            }
            Btn(new Vector2(-520, 20), new Vector2(160, 140), new Color(1f, 0.65f, 0.17f), UISkin.Instance.iconBack, () =>
            {
                if (slots.Count == 0) return;
                slots.RemoveAt(slots.Count - 1);
                Refresh();
            });
            Button play = Btn(new Vector2(520, 20), new Vector2(180, 160), new Color(0.55f, 0.83f, 0.27f), UISkin.Instance.iconPlay, () => StartCoroutine(PlayIt()));
            while (!Finished) yield return null;
        }

        private void Add(int note)
        {
            if (slots.Count >= slotCount) return;
            slots.Add(note);
            E?.NoteOn(I("Xylophone"), note + 12, 100, Ch);
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < slotImages.Length; i++)
                slotImages[i].color = i < slots.Count ? NoteUtil.ColorOf(slots[i]) : new Color(1, 1, 1, 0.5f);
        }

        private IEnumerator PlayIt()
        {
            if (slots.Count < slotCount) { Guide.Say("game_fill"); yield break; }
            double t = AudioSettings.dspTime + 0.1;
            InstrumentDefinition x = I("Xylophone"), p = I("UprightPiano");
            for (int i = 0; i < slots.Count; i++)
            {
                Note(x, slots[i] + 12, t + i * 0.45, 0);
                if (i % 2 == 0) Note(p, i % 4 == 0 ? 48 : 43, t + i * 0.45, 0.8f, 70);
            }
            for (int i = 0; i < slots.Count; i++) { yield return Until(t + i * 0.45); Pulse(slotImages[i], 0.3f); }
            yield return new WaitForSeconds(0.6f);
            Win();
        }
    }
}
