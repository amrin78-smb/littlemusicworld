using System.Collections;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// Rhythm Carnival (GDD 5.8): seven booth cards. A game runs at the child's current level for it; success gives a
    /// star, a celebration and the next level (saved). "Again" plays a fresh round; Back returns to the booths.
    /// </summary>
    public sealed class CarnivalManager : MonoBehaviour
    {
        [SerializeField] private RectTransform stage;
        [SerializeField] private GameObject boothPanel;
        [SerializeField] private Button[] boothButtons;     // order matches Games()
        [SerializeField] private Image[] boothStars;        // shows the level reached
        [SerializeField] private GameObject gameBar;        // back + again
        [SerializeField] private Button backButton, againButton;
        [SerializeField] private RectTransform scoreDots;
        [SerializeField] private Sprite[] portraits;
        [SerializeField] private MascotController melo;
        [SerializeField] private ParticleSystem confetti;

        private MiniGame current;
        private int currentIndex = -1;

        private static System.Type[] Games() => new[]
        {
            typeof(RhythmMatchGame), typeof(SoundDetectiveGame), typeof(FollowBeatGame), typeof(MemoryGame),
            typeof(HighLowGame), typeof(WhoIsPlayingGame), typeof(MelodyBuilderGame)
        };

        public static readonly string[] Ids = { "rhythm_match", "sound_detective", "follow_beat", "memory", "high_low", "who_playing", "melody_builder" };

        private void Start()
        {
            for (int i = 0; i < boothButtons.Length; i++)
            {
                int k = i;
                boothButtons[i].onClick.AddListener(() => Launch(k));
            }
            backButton.onClick.AddListener(Back);
            againButton.onClick.AddListener(() => Launch(currentIndex));
            gameBar.SetActive(false);
            againButton.gameObject.SetActive(false);
            RefreshStars();
            Services.Discover(Services.Save?.Data.progress.visitedAreas, SceneFlow.RhythmCarnival);
            Invoke(nameof(Intro), 0.4f);
        }

        private void Intro()
        {
            melo?.Wave();
            Guide.Say("area_carnival");
        }

        public static int LevelOf(string id)
        {
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            if (p == null) return 0;
            int level = 0;
            while (p.completedActivities.Contains($"carnival:{id}:{level}")) level++;
            return level;
        }

        private void RefreshStars()
        {
            for (int i = 0; i < boothStars.Length && i < Ids.Length; i++)
            {
                int lv = LevelOf(Ids[i]);
                boothStars[i].enabled = lv > 0;
                boothStars[i].transform.localScale = Vector3.one * Mathf.Min(1.3f, 0.8f + lv * 0.08f);
            }
        }

        public Sprite Portrait(string id)
        {
            foreach (Sprite s in portraits) if (s != null && s.name == "Portrait_" + id) return s;
            return UISkin.Instance.note;
        }

        public void MeloReact() => melo?.Do("React");

        public void ShowScore(int good, int total)
        {
            for (int i = 0; i < scoreDots.childCount; i++)
            {
                Transform d = scoreDots.GetChild(i);
                d.gameObject.SetActive(i < total);
                d.GetComponent<Image>().color = i < good ? new Color(1f, 0.82f, 0.25f) : new Color(1, 1, 1, 0.4f);
            }
        }

        private void Launch(int index)
        {
            Stop();
            currentIndex = index;
            boothPanel.SetActive(false);
            gameBar.SetActive(true);
            againButton.gameObject.SetActive(false);
            ShowScore(0, 0);
            current = (MiniGame)gameObject.AddComponent(Games()[index]);
            current.Stage = stage;
            current.Host = this;
            current.Level = LevelOf(Ids[index]);
            StartCoroutine(RunGame(current));
        }

        private IEnumerator RunGame(MiniGame g)
        {
            float t = Guide.Say(g.IntroVoice);
            melo?.Point();
            yield return new WaitForSeconds(Mathf.Min(t, 3f) + 0.3f);
            yield return g.Run();
            if (g != current || !g.Succeeded) yield break;
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            if (p != null && p.Add(p.completedActivities, $"carnival:{g.Id}:{g.Level}")) { p.stars++; Services.Save.Save(); }
            melo?.Celebrate();
            UISound.Sparkle();
            if (confetti != null) confetti.Play();
            Guide.Say("follow_done");
            againButton.gameObject.SetActive(true);
            RefreshStars();
        }

        private void Stop()
        {
            if (current == null) return;
            StopAllCoroutines();
            current.StopAllCoroutines();
            current.Cleanup();
            Destroy(current);
            current = null;
        }

        private void Back()
        {
            Stop();
            gameBar.SetActive(false);
            boothPanel.SetActive(true);
            ShowScore(0, 0);
            RefreshStars();
        }
    }
}
