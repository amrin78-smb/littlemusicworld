using System;
using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.UI
{
    /// <summary>
    /// Song cards for Follow the Melody: one picture card per lesson, grouped by 1–4 difficulty dots, a gold star on
    /// finished ones. No reading needed. Opened by any keyboard instrument.
    /// </summary>
    public sealed class SongPicker : MonoBehaviour
    {
        [SerializeField] private RectTransform cardGrid;
        [SerializeField] private Button[] tierButtons;
        [SerializeField] private Button closeButton;
        [SerializeField] private Sprite[] songIcons; // named "Icon<Picture>"

        private Action<SongLibrary.Lesson> onPick;
        private Func<SongLibrary.Lesson, bool> filter;
        private List<SongLibrary.Lesson> lessons;
        private int tier;
        private readonly List<GameObject> cards = new List<GameObject>();

        private void Awake()
        {
            lessons = SongLibrary.Lessons();
            for (int i = 0; i < tierButtons.Length; i++)
            {
                int t = i;
                tierButtons[i].onClick.AddListener(() => Show(t));
            }
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Open(Action<SongLibrary.Lesson> pick, Func<SongLibrary.Lesson, bool> canPlay = null)
        {
            onPick = pick;
            filter = canPlay;
            gameObject.SetActive(true);
            Show(tier);
            Guide.Say("piano_pick_song");
        }

        private void Show(int t)
        {
            tier = t;
            foreach (GameObject c in cards) Destroy(c);
            cards.Clear();
            for (int i = 0; i < tierButtons.Length; i++) tierButtons[i].transform.localScale = Vector3.one * (i == t ? 1.12f : 0.92f);
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            foreach (SongLibrary.Lesson l in lessons)
                if (l.tier == t && (filter == null || filter(l))) cards.Add(Card(l, p != null && p.completedSongs.Contains(l.Key)));
        }

        private GameObject Card(SongLibrary.Lesson l, bool done)
        {
            UISkin skin = UISkin.Instance;
            var go = new GameObject("Card_" + l.Key, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback));
            go.transform.SetParent(cardGrid, false);
            var img = go.GetComponent<Image>();
            img.sprite = skin.pebble;
            img.type = Image.Type.Sliced;
            img.color = NoteUtil.ColorOf(l.notes[0].note + l.song.id.Length * 2);
            Child(go.transform, "Icon", Icon(l.song.picture), Color.white, new Vector2(0, 14), new Vector2(124, 124));
            for (int d = 0; d <= l.tier; d++)
                Child(go.transform, "Dot", skin.circle, Color.white, new Vector2((d - l.tier * 0.5f) * 26f, -70), new Vector2(20, 20));
            if (done) Child(go.transform, "Star", skin.iconStar, new Color(1f, 0.82f, 0.25f), new Vector2(72, 72), new Vector2(60, 60));
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                onPick?.Invoke(l);
            });
            return go;
        }

        private static void Child(Transform parent, string name, Sprite s, Color c, Vector2 pos, Vector2 size)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            var i = g.GetComponent<Image>();
            i.sprite = s;
            i.color = c;
            i.raycastTarget = false;
        }

        private Sprite Icon(string picture)
        {
            string want = "Icon" + char.ToUpperInvariant(picture[0]) + picture.Substring(1);
            foreach (Sprite s in songIcons) if (s != null && s.name == want) return s;
            return songIcons[Mathf.Abs(picture.GetHashCode()) % songIcons.Length];
        }
    }
}
