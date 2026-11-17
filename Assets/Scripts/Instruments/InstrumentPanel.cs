using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Instruments
{
    /// <summary>
    /// Builds the child-sized play interface for any instrument at runtime, from its <see cref="PlayStyle"/>:
    ///   Keyboard → <see cref="KeyboardSurface"/> · DrumPads → big round pads (kit laid out like a real kit) ·
    ///   Mallets → coloured bars · Strum → strings + chord buttons (swipe to strum, tap to pluck) ·
    ///   Pluck → harp strings in rainbow colours · Bow / Blow → hold-to-play note buttons (sustained notes).
    /// Everything is coloured by pitch (do-re-mi rainbow) so the same colours mean the same notes everywhere.
    /// </summary>
    public sealed class InstrumentPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform area;
        public event System.Action<int> Played; // note (for discovery/recording hooks)

        public InstrumentDefinition Current { get; private set; }
        public KeyboardSurface Keyboard { get; private set; }
        public PadSurface Pads { get; private set; }

        private readonly List<GameObject> built = new List<GameObject>();
        private GameObject chordBar;

        private UISkin Skin => UISkin.Instance;

        public void Show(InstrumentDefinition def)
        {
            Clear();
            Current = def;
            if (def == null) return;
            Services.Engine?.Load(def);
            gameObject.SetActive(true);
            switch (def.playStyle)
            {
                case PlayStyle.Keyboard: BuildKeyboard(def); break;
                case PlayStyle.DrumPads: BuildDrums(def); break;
                case PlayStyle.Mallets: BuildBars(def); break;
                case PlayStyle.Strum: BuildStrum(def); break;
                case PlayStyle.Pluck: BuildHarp(def); break;
                case PlayStyle.Bow:
                case PlayStyle.Blow: BuildHold(def); break;
            }
        }

        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            foreach (GameObject g in built) if (g != null) Destroy(g);
            built.Clear();
            Keyboard = null;
            Pads = null;
            chordBar = null;
        }

        private RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (parent == area) built.Add(go);
            return (RectTransform)go.transform;
        }

        private RectTransform Fill(string name)
        {
            RectTransform r = Node(name, area);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        private Image Img(RectTransform parent, string name, Sprite s, Color c, Vector2 pos, Vector2 size, Vector2? anchor = null)
        {
            RectTransform r = Node(name, parent);
            r.anchorMin = r.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            var i = r.gameObject.AddComponent<Image>();
            i.sprite = s;
            if (s != null && s.border != Vector4.zero) i.type = Image.Type.Sliced;
            i.color = c;
            i.raycastTarget = false;
            return i;
        }

        // ------------------------------------------------------------------ keyboard
        private void BuildKeyboard(InstrumentDefinition def)
        {
            RectTransform r = Fill("Keyboard");
            Keyboard = r.gameObject.AddComponent<KeyboardSurface>();
            Keyboard.SetSprites(r, Skin.keyWhite, Skin.keyBlack, Skin.circle, Skin.glowRect);
            int low = def.playableNotes.Length > 0 ? def.playableNotes[0] : 60;
            low = Mathf.Max(low, 48);
            Keyboard.Configure(def, low + 12 > def.highestNote ? low : 60, 15);
            Keyboard.KeyDown += (n, v) => Played?.Invoke(n);
        }

        // ------------------------------------------------------------------ pads
        private PadSurface NewPadSurface(string name, PadSurface.Mode mode, out RectTransform root)
        {
            root = Fill(name);
            var ps = root.gameObject.AddComponent<PadSurface>();
            ps.Instrument = Current;
            ps.PlayMode = mode;
            ps.PadDown += (i, v) => Played?.Invoke(ps.pads[i].note);
            Pads = ps;
            return ps;
        }

        private PadSurface.Pad AddPad(PadSurface ps, RectTransform root, int note, Color colour, Vector2 pos, Vector2 size, Sprite sprite,
            bool ring = true, Vector2? anchor = null)
        {
            Image glow = Img(root, "Glow" + note, Skin.glow, new Color(colour.r, colour.g, colour.b, 0f), pos, size * 1.35f, anchor);
            Image body = Img(root, "Pad" + note, sprite, colour, pos, size, anchor);
            if (ring && sprite == Skin.circle)
            {
                Image skin = Img(body.rectTransform, "Skin", Skin.circle, new Color(1f, 0.97f, 0.9f), Vector2.zero, size * 0.78f);
                Img(skin.rectTransform, "Dot", Skin.circle, new Color(0, 0, 0, 0.08f), Vector2.zero, size * 0.2f);
            }
            var pad = new PadSurface.Pad { rect = body.rectTransform, image = body, glow = glow, note = note, colour = colour };
            ps.pads.Add(pad);
            return pad;
        }

        private void BuildDrums(InstrumentDefinition def)
        {
            PadSurface ps = NewPadSurface("Drums", PadSurface.Mode.Tap, out RectTransform root);
            var zones = new List<SampleZone>(def.zones);
            var notes = new List<int>();
            foreach (SampleZone z in zones) if (!notes.Contains(z.rootNote)) notes.Add(z.rootNote);
            Rect r = area.rect;
            Color baseC = def.color;
            if (def.id == "DrumKit")
            {
                // kit layout: cymbals high at the sides, toms across the top, snare + hi-hat left, kick big in the middle
                var layout = new Dictionary<int, (Vector2 pos, float size)>
                {
                    { 49, (new Vector2(-0.38f, 0.28f), 230) }, { 51, (new Vector2(0.38f, 0.28f), 250) },
                    { 50, (new Vector2(-0.12f, 0.3f), 200) }, { 48, (new Vector2(0.12f, 0.3f), 210) },
                    { 42, (new Vector2(-0.4f, -0.12f), 210) }, { 46, (new Vector2(-0.4f, -0.12f), 0) },
                    { 38, (new Vector2(-0.2f, -0.2f), 240) }, { 45, (new Vector2(0.24f, -0.2f), 250) },
                    { 36, (new Vector2(0.0f, -0.25f), 300) },
                };
                foreach (int n in notes)
                {
                    if (!layout.TryGetValue(n, out var l) || l.size <= 0) continue;
                    bool cym = n == 42 || n == 49 || n == 51;
                    Color c = cym ? new Color(0.95f, 0.76f, 0.3f) : n == 36 ? new Color(0.89f, 0.33f, 0.25f) : Color.Lerp(baseC, Color.white, 0.15f);
                    AddPad(ps, root, n, c, new Vector2(l.pos.x * r.width, l.pos.y * r.height), Vector2.one * l.size, Skin.circle, !cym);
                }
                return;
            }
            int count = notes.Count;
            float size = Mathf.Min(r.height * 0.8f, r.width / Mathf.Max(2, count) * 0.82f, 360f);
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * size * 1.15f;
                Color c = Color.Lerp(baseC, NoteUtil.PitchColors[(i * 4) % 12], 0.35f);
                AddPad(ps, root, notes[i], c, new Vector2(x, 0), Vector2.one * size, Skin.circle);
            }
        }

        private void BuildBars(InstrumentDefinition def)
        {
            PadSurface ps = NewPadSurface("Bars", PadSurface.Mode.Strum, out RectTransform root);
            int[] notes = def.playableNotes;
            Rect r = area.rect;
            float w = r.width / notes.Length;
            Img(root, "Frame", Skin.panel, new Color(0.66f, 0.41f, 0.24f), new Vector2(0, -r.height * 0.05f), new Vector2(r.width, r.height * 0.5f));
            for (int i = 0; i < notes.Length; i++)
            {
                float h = r.height * (0.95f - 0.4f * i / Mathf.Max(1, notes.Length - 1));
                AddPad(ps, root, notes[i], NoteUtil.ColorOf(notes[i]), new Vector2((i - (notes.Length - 1) * 0.5f) * w, 0),
                    new Vector2(w * 0.86f, h), Skin.pebble, false);
            }
        }

        // ------------------------------------------------------------------ strings
        private static readonly (string name, int[] pcs, Color colour)[] Chords =
        {
            ("C", new[] { 0, 4, 7 }, new Color(1f, 0.35f, 0.37f)), ("F", new[] { 5, 9, 0 }, new Color(0.55f, 0.83f, 0.27f)),
            ("G", new[] { 7, 11, 2 }, new Color(0.18f, 0.77f, 0.71f)), ("Am", new[] { 9, 0, 4 }, new Color(0.36f, 0.49f, 0.98f)),
        };

        private void BuildStrum(InstrumentDefinition def)
        {
            PadSurface ps = NewPadSurface("Strings", PadSurface.Mode.Strum, out RectTransform root);
            int[] open = def.openStrings.Length > 0 ? def.openStrings : new[] { 40, 45, 50, 55, 59, 64 };
            Rect r = area.rect;
            float left = r.width * 0.2f;
            Img(root, "Body", Skin.panel, Color.Lerp(def.color, new Color(0.4f, 0.25f, 0.15f), 0.35f), new Vector2(left * 0.5f, 0),
                new Vector2(r.width - left + 40, r.height * 0.95f));
            Img(root, "Hole", Skin.circle, new Color(0.18f, 0.12f, 0.1f, 0.9f), new Vector2(left * 0.5f + r.width * 0.12f, 0), Vector2.one * r.height * 0.6f);
            // strings: lowest string at the bottom (as a child holding a guitar sees it)
            float span = r.height * 0.8f;
            for (int i = 0; i < open.Length; i++)
            {
                float y = -span / 2 + span * i / Mathf.Max(1, open.Length - 1);
                float thick = Mathf.Lerp(26f, 14f, (float)i / Mathf.Max(1, open.Length - 1));
                // the touch target is much taller than the visible string so children can hit it easily
                PadSurface.Pad pad = AddPad(ps, root, open[i], new Color(1f, 0.93f, 0.75f), new Vector2(left * 0.5f, y),
                    new Vector2(r.width - left, span / open.Length * 0.85f), null, false);
                pad.image.color = new Color(1, 1, 1, 0f);
                pad.colour = new Color(1, 1, 1, 0f);
                Image line = Img(pad.rect, "Line", Skin.pebble, new Color(1f, 0.93f, 0.75f), Vector2.zero, new Vector2(r.width - left, thick));
                pad.image = line;
                pad.colour = line.color;
            }
            // chord buttons
            chordBar = Node("Chords", root).gameObject;
            var cr = (RectTransform)chordBar.transform;
            cr.anchorMin = cr.anchorMax = new Vector2(0f, 0.5f);
            cr.anchoredPosition = new Vector2(left * 0.5f, 0);
            for (int c = 0; c < Chords.Length; c++)
            {
                int ci = c;
                RectTransform b = Node(Chords[c].name, cr);
                b.sizeDelta = new Vector2(left * 0.8f, span / 4.6f);
                b.anchoredPosition = new Vector2(0, span * 0.38f - c * span / 4f);
                var img = b.gameObject.AddComponent<Image>();
                img.sprite = Skin.pebble;
                img.type = Image.Type.Sliced;
                img.color = Chords[c].colour;
                var btn = b.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                b.gameObject.AddComponent<PressFeedback>();
                for (int d = 0; d <= c % 4; d++)
                {
                    RectTransform dot = Node("Dot", b);
                    dot.sizeDelta = new Vector2(22, 22);
                    dot.anchoredPosition = new Vector2((d - (c % 4) * 0.5f) * 30f, 4f);
                    dot.gameObject.AddComponent<Image>().sprite = Skin.circle;
                }
                btn.onClick.AddListener(() => SetChord(ps, open, ci));
            }
            SetChord(ps, open, 0);
        }

        private void SetChord(PadSurface ps, int[] open, int chord)
        {
            var notes = new int[open.Length];
            int[] pcs = Chords[chord].pcs;
            for (int i = 0; i < open.Length; i++)
            {
                int best = open[i];
                for (int up = 0; up <= 7; up++)
                {
                    if (System.Array.IndexOf(pcs, (open[i] + up) % 12) >= 0) { best = open[i] + up; break; }
                }
                notes[i] = best;
            }
            ps.SetNotes(notes);
            if (chordBar != null)
                for (int c = 0; c < chordBar.transform.childCount; c++)
                    chordBar.transform.GetChild(c).localScale = Vector3.one * (c == chord ? 1.1f : 0.92f);
        }

        private void BuildHarp(InstrumentDefinition def)
        {
            PadSurface ps = NewPadSurface("Harp", PadSurface.Mode.Strum, out RectTransform root);
            int[] all = def.playableNotes;
            int start = Mathf.Max(0, System.Array.IndexOf(all, 60));
            int count = Mathf.Min(15, all.Length - start);
            Rect r = area.rect;
            float w = r.width / count;
            for (int i = 0; i < count; i++)
            {
                int n = all[start + i];
                PadSurface.Pad pad = AddPad(ps, root, n, new Color(1, 1, 1, 0f), new Vector2((i - (count - 1) * 0.5f) * w, 0),
                    new Vector2(w * 0.9f, r.height), null, false);
                Image line = Img(pad.rect, "String", Skin.pebble, NoteUtil.ColorOf(n), Vector2.zero,
                    new Vector2(16, r.height * (0.95f - 0.35f * i / count)));
                pad.image = line;
                pad.colour = line.color;
            }
            ps.SetNotes(ps.pads.ConvertAll(p => p.note));
            ps.Vertical = true;
        }

        private void BuildHold(InstrumentDefinition def)
        {
            PadSurface ps = NewPadSurface(def.playStyle == PlayStyle.Bow ? "Bow" : "Wind", PadSurface.Mode.Hold, out RectTransform root);
            int[] notes = def.playableNotes;
            Rect r = area.rect;
            float w = r.width / notes.Length;
            bool wind = def.playStyle == PlayStyle.Blow;
            if (wind)
                Img(root, "Tube", Skin.panel, Color.Lerp(def.color, Color.white, 0.25f), Vector2.zero, new Vector2(r.width, r.height * 0.42f));
            for (int i = 0; i < notes.Length; i++)
            {
                float size = Mathf.Min(w * 0.82f, wind ? r.height * 0.36f : r.height);
                Vector2 dims = wind ? Vector2.one * size : new Vector2(w * 0.8f, r.height * 0.9f);
                AddPad(ps, root, notes[i], NoteUtil.ColorOf(notes[i]), new Vector2((i - (notes.Length - 1) * 0.5f) * w, 0), dims,
                    wind ? Skin.circle : Skin.pebble, wind);
            }
        }
    }
}
