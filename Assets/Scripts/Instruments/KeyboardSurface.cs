using System;
using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Instruments
{
    /// <summary>
    /// Child-sized multi-touch piano keyboard built inside a RectTransform. Every key is its own touch target; black
    /// keys are tested first because they sit on top. Pressing plays through the sampler immediately (no UI event
    /// system in the path) and the key dips, darkens and shows its pitch colour. Keys can also glow as guide prompts
    /// (Follow the Melody) or light up when played back by a sequence.
    /// </summary>
    public sealed class KeyboardSurface : MonoBehaviour, ITouchSurface, ISongSurface
    {
        [SerializeField] private InstrumentDefinition instrument;
        [SerializeField] private RectTransform area;
        [SerializeField] private int lowNote = 60;
        [SerializeField] private int whiteKeyCount = 15;
        [SerializeField] private Sprite whiteKeySprite;
        [SerializeField] private Sprite blackKeySprite;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private int channel = MusicalAudioEngine.FreePlayChannel;
        [SerializeField] private bool showColourDots = true;

        public event Action<int, int> KeyDown; // note, velocity
        public event Action<int> KeyUp;
        public event Action<int> Flashed; // a sequence (demo, reward) played this note

        public InstrumentDefinition Instrument
        {
            get => instrument;
            set
            {
                ReleaseEverything();
                instrument = value;
                if (instrument != null) MusicalAudioEngine.Instance?.Load(instrument);
            }
        }

        public event Action<int, int> NoteDown { add => KeyDown += value; remove => KeyDown -= value; }
        public bool Interactable { get => interactable; set => interactable = value; }
        public bool Has(int note) => byNote.ContainsKey(note);
        public int LowNote => lowNote;
        public int HighNote { get; private set; }
        public bool SurfaceActive => isActiveAndEnabled && instrument != null && interactable;
        public int Priority => 10;
        public bool SlideBetweenTargets => true;
        public bool interactable = true;

        private sealed class Key
        {
            public int note;
            public bool black;
            public RectTransform rt;
            public Image body, dot, glow;
            public int fingers;
            public float pressAmount;  // animated 0..1
            public float playbackLit;  // flash when a sequence plays this key
            public bool guide;
            public Vector2 restPos;
        }

        private readonly List<Key> keys = new List<Key>();
        private readonly Dictionary<int, Key> byNote = new Dictionary<int, Key>();
        private readonly Dictionary<int, int> fingerNote = new Dictionary<int, int>();
        private Camera uiCamera;

        /// <summary>For keyboards built at runtime (InstrumentPanel).</summary>
        public void SetSprites(RectTransform keyArea, Sprite white, Sprite black, Sprite dot, Sprite glow)
        {
            area = keyArea;
            whiteKeySprite = white;
            blackKeySprite = black;
            dotSprite = dot;
            glowSprite = glow;
        }

        public void Configure(InstrumentDefinition def, int low, int whiteKeys)
        {
            Instrument = def;
            lowNote = low;
            whiteKeyCount = whiteKeys;
            Build();
        }

        private void Start()
        {
            if (keys.Count == 0) Build();
            if (instrument != null) MusicalAudioEngine.Instance?.Load(instrument);
            TouchRouter.Ensure().Register(this);
            Canvas c = GetComponentInParent<Canvas>();
            uiCamera = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
        }

        private void OnDisable() => ReleaseEverything();

        private void OnDestroy()
        {
            if (TouchRouter.Instance != null) TouchRouter.Instance.Unregister(this);
        }

        public void Build()
        {
            if (area == null) area = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            foreach (Key k in keys) if (k.rt != null) Destroy(k.rt.gameObject);
            keys.Clear();
            byNote.Clear();

            Rect r = area.rect;
            float whiteW = r.width / whiteKeyCount;
            float gap = Mathf.Max(4f, whiteW * 0.05f);
            int note = lowNote;
            if (NoteUtil.IsBlackKey(note)) note--;
            lowNote = note;
            var whites = new List<Key>();
            int w = 0;
            for (; w < whiteKeyCount; note++)
            {
                if (NoteUtil.IsBlackKey(note)) continue;
                Key k = MakeKey(note, false, new Vector2(r.xMin + whiteW * (w + 0.5f), r.center.y), new Vector2(whiteW - gap, r.height));
                whites.Add(k);
                w++;
            }
            HighNote = note - 1;
            // black keys on top, between their white neighbours
            for (int i = 0; i < whites.Count - 1; i++)
            {
                int bn = whites[i].note + 1;
                if (!NoteUtil.IsBlackKey(bn)) continue;
                float x = (whites[i].restPos.x + whites[i + 1].restPos.x) * 0.5f;
                float h = r.height * 0.6f;
                MakeKey(bn, true, new Vector2(x, r.yMax - h * 0.5f), new Vector2(whiteW * 0.62f, h));
            }
        }

        private Key MakeKey(int note, bool black, Vector2 pos, Vector2 size)
        {
            var go = new GameObject((black ? "Black_" : "White_") + NoteUtil.Name(note), typeof(RectTransform));
            go.transform.SetParent(area, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var k = new Key { note = note, black = black, rt = rt, restPos = pos };

            k.glow = Child("Glow", rt, glowSprite, new Color(1, 1, 1, 0), size * new Vector2(1.25f, 1.08f), Vector2.zero);
            k.body = go.AddComponent<Image>();
            k.body.sprite = black ? blackKeySprite : whiteKeySprite;
            k.body.type = k.body.sprite != null && k.body.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            k.body.color = black ? new Color(0.18f, 0.16f, 0.29f) : Color.white;
            k.body.raycastTarget = false;
            k.glow.transform.SetAsFirstSibling();
            if (showColourDots && dotSprite != null)
            {
                float d = Mathf.Min(size.x * 0.5f, 70f);
                k.dot = Child("Dot", rt, dotSprite, NoteUtil.ColorOf(note), new Vector2(d, d),
                    new Vector2(0, -size.y * 0.5f + d * (black ? 0.9f : 1.0f)));
            }
            keys.Add(k);
            byNote[note] = k;
            return k;
        }

        private static Image Child(string name, RectTransform parent, Sprite s, Color c, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------------ ITouchSurface

        public int HitTest(Vector2 screen)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool black = pass == 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    Key k = keys[i];
                    if (k.black != black) continue;
                    if (RectTransformUtility.RectangleContainsScreenPoint(k.rt, screen, uiCamera)) return i;
                }
            }
            return -1;
        }

        public void Press(int target, int finger, Vector2 screen)
        {
            if (target < 0 || target >= keys.Count) return;
            Key k = keys[target];
            // Velocity from where the key is touched: nearer the front edge plays a little louder (like a real key).
            RectTransformUtility.ScreenPointToLocalPointInRectangle(k.rt, screen, uiCamera, out Vector2 local);
            float front = Mathf.InverseLerp(k.rt.rect.yMax, k.rt.rect.yMin, local.y);
            int velocity = Mathf.RoundToInt(Mathf.Lerp(78f, 118f, front));
            fingerNote[finger] = k.note;
            k.fingers++;
            MusicalAudioEngine.Instance?.NoteOn(instrument, k.note, velocity, channel);
            KeyDown?.Invoke(k.note, velocity);
        }

        public void Release(int target, int finger)
        {
            if (!fingerNote.TryGetValue(finger, out int note)) return;
            fingerNote.Remove(finger);
            if (!byNote.TryGetValue(note, out Key k)) return;
            k.fingers = Mathf.Max(0, k.fingers - 1);
            if (k.fingers == 0)
            {
                MusicalAudioEngine.Instance?.NoteOff(instrument, note, channel);
                KeyUp?.Invoke(note);
            }
        }

        public void Drag(int target, int finger, Vector2 screen, Vector2 delta) { }

        private void ReleaseEverything()
        {
            foreach (var kv in fingerNote)
                if (instrument != null) MusicalAudioEngine.Instance?.NoteOff(instrument, kv.Value, channel);
            fingerNote.Clear();
            foreach (Key k in keys) k.fingers = 0;
        }

        // ------------------------------------------------------------------ prompts / playback visuals

        public void SetGuide(int note, bool on)
        {
            if (byNote.TryGetValue(note, out Key k)) k.guide = on;
        }

        public void ClearGuides()
        {
            foreach (Key k in keys) k.guide = false;
        }

        public void Flash(int note)
        {
            if (byNote.TryGetValue(note, out Key k)) k.playbackLit = 1f;
            Flashed?.Invoke(note);
        }

        public bool HasNote(int note) => byNote.ContainsKey(note);

        public Vector3 KeyWorldPosition(int note) => byNote.TryGetValue(note, out Key k) ? k.rt.position : transform.position;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
            foreach (Key k in keys)
            {
                float target = k.fingers > 0 ? 1f : 0f;
                k.pressAmount = Mathf.MoveTowards(k.pressAmount, target, dt * (target > k.pressAmount ? 30f : 9f));
                k.playbackLit = Mathf.MoveTowards(k.playbackLit, 0f, dt * 3f);
                float lit = Mathf.Max(k.pressAmount, k.playbackLit);
                Color pc = NoteUtil.ColorOf(k.note);
                Color baseCol = k.black ? new Color(0.18f, 0.16f, 0.29f) : Color.white;
                k.body.color = Color.Lerp(baseCol, Color.Lerp(baseCol, pc, k.black ? 0.65f : 0.45f), lit);
                k.rt.anchoredPosition = k.restPos + new Vector2(0, -k.pressAmount * 10f);
                k.rt.localScale = new Vector3(1f - 0.03f * k.pressAmount, 1f - 0.02f * k.pressAmount, 1f);
                float glowA = k.guide ? pulse : lit * 0.8f;
                Color gc = pc;
                gc.a = glowA;
                k.glow.color = gc;
                if (k.dot != null) k.dot.rectTransform.localScale = Vector3.one * (1f + 0.25f * lit + (k.guide ? 0.15f * pulse : 0f));
            }
        }
    }
}
