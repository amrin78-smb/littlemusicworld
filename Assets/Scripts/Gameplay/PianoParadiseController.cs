using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Instruments;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// Piano Paradise scene flow: free play by default; the song button opens picture cards (one per lesson, grouped
    /// by 1–4 difficulty dots); picking one starts Follow the Melody with Melo demonstrating. Home returns to the hub.
    /// </summary>
    public sealed class PianoParadiseController : MonoBehaviour
    {
        [SerializeField] private PianoActivity activity;
        [SerializeField] private MelodyGuide guide;
        [SerializeField] private PianoModel3D piano3D;
        [SerializeField] private MascotController melo;
        [SerializeField] private Button homeButton, songsButton, freeButton, listenButton;
        [SerializeField] private GameObject songPanel;
        [SerializeField] private RectTransform cardGrid;
        [SerializeField] private Button[] tierButtons;
        [SerializeField] private Sprite cardSprite, starSprite, dotSprite;
        [SerializeField] private Sprite[] songIcons;          // matched by name "Icon<Picture>"
        [SerializeField] private RectTransform progressBar;   // fill grows with notes played
        [SerializeField] private Image progressFill;

        private int tier;
        private readonly List<GameObject> cards = new List<GameObject>();
        private List<SongLibrary.Lesson> lessons;

        private void Start()
        {
            lessons = SongLibrary.Lessons();
            homeButton.onClick.AddListener(() => { guide.Stop(); Services.Scenes.Load(SceneFlow.Hub); });
            songsButton.onClick.AddListener(OpenSongs);
            freeButton.onClick.AddListener(FreePlay);
            listenButton.onClick.AddListener(() => guide.Replay());
            for (int i = 0; i < tierButtons.Length; i++)
            {
                int t = i;
                tierButtons[i].onClick.AddListener(() => ShowTier(t));
            }
            guide.PhaseChanged += OnPhase;
            guide.Progress += (i, n) => SetProgress(n == 0 ? 0f : (float)i / n);
            activity.Keyboard.Flashed += n => piano3D.Pulse(n, 0.22f);
            guide.HintShown += n => { melo.Point(); Guide.Say("follow_hint"); };
            guide.Completed += l => { melo.Celebrate(); Guide.Say("follow_done"); };
            activity.InstrumentChanged += def => piano3D.SetInstruments(def, Services.Instruments.Get("UprightPiano"));
            songPanel.SetActive(false);
            SetProgress(-1f);
            listenButton.gameObject.SetActive(false);
            Invoke(nameof(Intro), 0.5f);
        }

        private void Intro()
        {
            melo.Wave();
            float t = Guide.Say("area_piano");
            Invoke(nameof(IntroFree), t + 0.6f);
        }

        private void IntroFree() => Guide.Say("piano_free");

        private void FreePlay()
        {
            guide.Stop();
            songPanel.SetActive(false);
            SetProgress(-1f);
            listenButton.gameObject.SetActive(false);
            melo.SetBase(MascotController.Base.Idle);
            Guide.Say("piano_free");
        }

        private void OpenSongs()
        {
            guide.Stop();
            songPanel.SetActive(true);
            ShowTier(tier);
            Guide.Say("piano_pick_song");
        }

        private void ShowTier(int t)
        {
            tier = t;
            foreach (GameObject c in cards) Destroy(c);
            cards.Clear();
            for (int i = 0; i < tierButtons.Length; i++) tierButtons[i].transform.localScale = Vector3.one * (i == t ? 1.12f : 0.92f);
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            foreach (SongLibrary.Lesson l in lessons)
            {
                if (l.tier != t) continue;
                cards.Add(MakeCard(l, p != null && p.completedSongs.Contains(l.Key)));
            }
        }

        private GameObject MakeCard(SongLibrary.Lesson l, bool done)
        {
            var go = new GameObject("Card_" + l.Key, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UI.PressFeedback));
            go.transform.SetParent(cardGrid, false);
            var img = go.GetComponent<Image>();
            img.sprite = cardSprite;
            img.type = Image.Type.Sliced;
            img.color = NoteUtil.ColorOf(l.notes[0].note);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            var ir = (RectTransform)icon.transform;
            ir.sizeDelta = new Vector2(150, 150);
            ir.anchoredPosition = new Vector2(0, 18);
            var ii = icon.GetComponent<Image>();
            ii.sprite = IconFor(l.song.picture);
            ii.raycastTarget = false;
            for (int d = 0; d <= l.tier; d++)
            {
                var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(go.transform, false);
                var dr = (RectTransform)dot.transform;
                dr.sizeDelta = new Vector2(22, 22);
                dr.anchoredPosition = new Vector2((d - l.tier * 0.5f) * 30f, -82);
                var di = dot.GetComponent<Image>();
                di.sprite = dotSprite;
                di.raycastTarget = false;
            }
            if (done)
            {
                var star = new GameObject("Star", typeof(RectTransform), typeof(Image));
                star.transform.SetParent(go.transform, false);
                var sr = (RectTransform)star.transform;
                sr.sizeDelta = new Vector2(70, 70);
                sr.anchoredPosition = new Vector2(88, 88);
                var si = star.GetComponent<Image>();
                si.sprite = starSprite;
                si.color = new Color(1f, 0.82f, 0.25f);
                si.raycastTarget = false;
            }
            go.GetComponent<Button>().onClick.AddListener(() => StartLesson(l));
            return go;
        }

        private Sprite IconFor(string picture)
        {
            string want = "Icon" + char.ToUpperInvariant(picture[0]) + picture.Substring(1);
            foreach (Sprite s in songIcons) if (s != null && s.name == want) return s;
            // songs without their own picture share a few friendly icons by first letter
            Sprite fallback = songIcons[Mathf.Abs(picture.GetHashCode()) % songIcons.Length];
            return fallback;
        }

        private void StartLesson(SongLibrary.Lesson l)
        {
            songPanel.SetActive(false);
            listenButton.gameObject.SetActive(true);
            guide.Begin(activity.Keyboard, l);
        }

        private void OnPhase(MelodyGuide.Phase p)
        {
            switch (p)
            {
                case MelodyGuide.Phase.Demonstrating:
                    melo.SetBase(MascotController.Base.Sing);
                    Guide.Say("follow_watch");
                    break;
                case MelodyGuide.Phase.Following:
                    melo.SetBase(MascotController.Base.Idle);
                    melo.Point();
                    Guide.Say("follow_turn");
                    break;
                case MelodyGuide.Phase.Celebrating:
                    melo.SetBase(MascotController.Base.Dance);
                    break;
                case MelodyGuide.Phase.Idle:
                    melo.SetBase(MascotController.Base.Idle);
                    break;
            }
        }

        private void SetProgress(float f)
        {
            if (progressBar == null) return;
            progressBar.gameObject.SetActive(f >= 0f);
            if (f >= 0f && progressFill != null) progressFill.fillAmount = f;
        }
    }
}
