using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>
    /// The discovery sticker book: every instrument (a dark silhouette until played for the first time — tap a found
    /// one to hear it), the six animal musicians and the areas visited, plus the star count. Pure celebration: there is
    /// nothing to lose and no list of chores.
    /// </summary>
    public sealed class StickerBook : MonoBehaviour
    {
        [SerializeField] private RectTransform instrumentGrid, musicianGrid, areaGrid;
        [SerializeField] private TMP_Text starCount;
        [SerializeField] private Sprite[] portraits;
        [SerializeField] private Sprite[] areaIcons; // by SceneFlow name order below
        [SerializeField] private Button close;

        private static readonly string[] Areas =
            { SceneFlow.PianoParadise, SceneFlow.DrumValley, SceneFlow.StringTown, SceneFlow.WindVillage,
              SceneFlow.OrchestraPark, SceneFlow.MusicStudio, SceneFlow.RhythmCarnival };
        private static readonly string[] Musicians = { "Bear", "Rabbit", "Fox", "Elephant", "Cat", "Penguin" };

        private readonly List<GameObject> built = new List<GameObject>();

        private void Awake() => close.onClick.AddListener(() => gameObject.SetActive(false));

        private void OnEnable()
        {
            foreach (GameObject g in built) Destroy(g);
            built.Clear();
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            if (p == null) return;
            foreach (InstrumentDefinition d in Services.Instruments.instruments)
            {
                bool found = p.discoveredInstruments.Contains(d.id);
                InstrumentDefinition def = d;
                Sticker(instrumentGrid, Portrait(d.id), found, found ? Color.Lerp(d.color, Color.white, 0.6f) : new Color(0.86f, 0.84f, 0.9f),
                    found ? () => Taste(def) : (System.Action)null);
            }
            foreach (string m in Musicians)
                Sticker(musicianGrid, Portrait(m), p.unlockedMusicians.Contains(m) || m == "Bear" || m == "Rabbit", new Color(1f, 0.95f, 0.85f), null);
            for (int i = 0; i < Areas.Length; i++)
                Sticker(areaGrid, i < areaIcons.Length ? areaIcons[i] : UISkin.Instance.star(), p.visitedAreas.Contains(Areas[i]),
                    NoteUtil.PitchColors[(i * 2) % 12], null);
            if (starCount != null) starCount.text = p.stars.ToString();
            Guide.Say("stickers");
        }

        private void Sticker(RectTransform grid, Sprite icon, bool found, Color bg, System.Action tap)
        {
            var go = new GameObject("Sticker", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(grid, false);
            built.Add(go);
            var img = go.GetComponent<Image>();
            img.sprite = UISkin.Instance.circle;
            img.color = bg;
            var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            ic.transform.SetParent(go.transform, false);
            ((RectTransform)ic.transform).sizeDelta = new Vector2(110, 110);
            var ii = ic.GetComponent<Image>();
            ii.sprite = icon;
            ii.preserveAspect = true;
            ii.color = found ? Color.white : new Color(0.16f, 0.14f, 0.26f, 0.55f); // silhouette until found
            ii.raycastTarget = false;
            if (tap != null)
            {
                go.AddComponent<PressFeedback>();
                go.GetComponent<Button>().onClick.AddListener(() => tap());
            }
        }

        private void Taste(InstrumentDefinition d)
        {
            Services.Engine?.Load(d);
            double t = AudioSettings.dspTime + 0.05;
            int n = d.playableNotes.Length > 0 ? d.playableNotes[d.playableNotes.Length / 2] : d.zones[0].rootNote;
            Services.Engine?.NoteOnAt(d, n, 100, Services.UiChannel, t);
            Services.Engine?.NoteOffAt(d, n, Services.UiChannel, t + 0.6);
        }

        private Sprite Portrait(string id)
        {
            foreach (Sprite s in portraits) if (s != null && s.name == "Portrait_" + id) return s;
            return UISkin.Instance.note;
        }
    }

    public static class SkinExtensions
    {
        public static Sprite star(this UISkin s) => s.iconStar;
    }
}
