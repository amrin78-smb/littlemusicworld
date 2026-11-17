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
    /// Orchestra Park (GDD 5.6 / 6.5). Animal cards along the bottom: tap one to bring that musician onto the stage
    /// (tap again to send them back). Tap a musician on stage to give them another instrument. Pick a song card and
    /// press the big Play button: the arranger writes a part for every musician, the sequencer plays them together and
    /// each musician's playing animation fires on its own notes. Tempo goes from turtle (slow) to rabbit (fast) within
    /// safe musical bounds. Musicians unlock when their instrument has been discovered in the areas (Bear and Rabbit
    /// are free from the start); locked cards show a gentle hint instead.
    /// </summary>
    public sealed class OrchestraManager : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Seat
        {
            public MusicianController musician;
            public Button card;
            public Image cardLock;
            public Transform wing;   // where they wait off-stage
        }

        [SerializeField] private Seat[] seats;
        [SerializeField] private Transform[] stageSpots;
        [SerializeField] private Button playButton, stopButton, songButton;
        [SerializeField] private Slider tempo;
        [SerializeField] private RectTransform songGrid;
        [SerializeField] private GameObject songPanel;
        [SerializeField] private RectTransform instrumentGrid;
        [SerializeField] private GameObject instrumentPanel;
        [SerializeField] private Sprite[] instrumentIcons;   // named by instrument id
        [SerializeField] private Sprite[] songIcons;
        [SerializeField] private GameObject[] instrumentPrefabs; // named by instrument id
        [SerializeField] private MascotController melo;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private Image songCardIcon;

        private Sequencer seq;
        private SongLibrary.Song song;
        private MusicianController picking;
        private readonly List<MusicianController> onStage = new List<MusicianController>();
        private readonly List<MusicianController> trackOwner = new List<MusicianController>();

        public bool Playing => seq != null && seq.IsPlaying;

        private void Start()
        {
            seq = gameObject.AddComponent<Sequencer>();
            seq.NoteVisual += (track, ev) => { if (track < trackOwner.Count) trackOwner[track].Hit(ev.note); };
            seq.Finished += OnFinished;
            song = SongLibrary.Get("twinkle");
            for (int i = 0; i < seats.Length; i++)
            {
                Seat s = seats[i];
                InstrumentDefinition def = Services.Instruments.Get(s.musician.defaultInstrument);
                s.musician.SetInstrument(def, Prefab(def?.id));
                s.musician.transform.position = s.wing.position;
                int k = i;
                s.card.onClick.AddListener(() => ToggleSeat(k));
                Services.Engine?.Load(def);
            }
            playButton.onClick.AddListener(Perform);
            stopButton.onClick.AddListener(StopShow);
            songButton.onClick.AddListener(() => { songPanel.SetActive(true); BuildSongCards(); });
            tempo.minValue = 0.7f;
            tempo.maxValue = 1.3f;
            tempo.value = 1f;
            tempo.onValueChanged.AddListener(v => { if (seq.IsPlaying) seq.Tempo = song.tempo * v; });
            songPanel.SetActive(false);
            instrumentPanel.SetActive(false);
            stopButton.gameObject.SetActive(false);
            RefreshCards();
            UpdateSongCard();
            Services.Discover(Services.Save?.Data.progress.visitedAreas, SceneFlow.OrchestraPark);
            // Start with two friends on stage so pressing Play immediately makes music.
            ToggleSeat(0);
            ToggleSeat(1);
            Invoke(nameof(Intro), 0.4f);
        }

        private void Intro()
        {
            melo?.Wave();
            Guide.Say("area_orchestra");
        }

        private GameObject Prefab(string id)
        {
            if (id == null) return null;
            foreach (GameObject g in instrumentPrefabs) if (g != null && g.name == id) return g;
            return null;
        }

        private Sprite InstrumentIcon(string id)
        {
            foreach (Sprite s in instrumentIcons) if (s != null && s.name == "Portrait_" + id) return s;
            return UISkin.Instance.note;
        }

        public bool Unlocked(MusicianController m)
        {
            if (m.animalId == "Bear" || m.animalId == "Rabbit") return true;
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            return p == null || p.discoveredInstruments.Contains(m.defaultInstrument) || p.unlockedMusicians.Contains(m.animalId);
        }

        private void RefreshCards()
        {
            foreach (Seat s in seats)
            {
                bool open = Unlocked(s.musician);
                if (s.cardLock != null) s.cardLock.gameObject.SetActive(!open);
                s.card.transform.localScale = Vector3.one * (onStage.Contains(s.musician) ? 1.08f : 0.94f);
                if (open) Services.Save?.Data.progress.Add(Services.Save.Data.progress.unlockedMusicians, s.musician.animalId);
            }
        }

        private void ToggleSeat(int i)
        {
            if (Playing) return;
            Seat s = seats[i];
            MusicianController m = s.musician;
            if (!Unlocked(m))
            {
                melo?.Point();
                Guide.Say("orchestra_find_" + m.animalId.ToLowerInvariant());
                return;
            }
            if (onStage.Contains(m))
            {
                onStage.Remove(m);
                m.Spot = -1;
                m.WalkTo(s.wing.position);
            }
            else
            {
                int spot = FreeSpot();
                if (spot < 0) return;
                onStage.Add(m);
                m.Spot = spot;
                m.WalkTo(stageSpots[spot].position);
                m.React();
                // a little taste of their instrument as they arrive
                if (m.Instrument != null)
                {
                    int note = m.Instrument.playableNotes.Length > 0 ? m.Instrument.playableNotes[m.Instrument.playableNotes.Length / 2] :
                        m.Instrument.zones[0].rootNote;
                    double t = AudioSettings.dspTime + 0.05;
                    Services.Engine?.NoteOnAt(m.Instrument, note, 90, 9, t);
                    Services.Engine?.NoteOffAt(m.Instrument, note, 9, t + 0.5);
                }
            }
            UISound.Plink(80, 60);
            RefreshCards();
        }

        private int FreeSpot()
        {
            for (int s = 0; s < stageSpots.Length; s++)
            {
                bool used = false;
                foreach (MusicianController m in onStage) used |= m.Spot == s;
                if (!used) return s;
            }
            return -1;
        }

        /// <summary>Tap a musician on stage (called by a raycast in Update) to choose its instrument.</summary>
        private void Update()
        {
            if (Playing || instrumentPanel.activeSelf || songPanel.activeSelf) return;
            UnityEngine.InputSystem.Pointer p = UnityEngine.InputSystem.Pointer.current;
            if (p == null || !p.press.wasReleasedThisFrame) return;
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = Camera.main.ScreenPointToRay(p.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 60f)) return;
            MusicianController m = hit.collider.GetComponentInParent<MusicianController>();
            if (m != null && onStage.Contains(m)) OpenInstrumentPicker(m);
        }

        private static readonly string[] Choices =
        {
            "GrandPiano", "DigitalKeyboard", "DrumKit", "Xylophone", "Congas", "Maracas", "AcousticGuitar", "Ukulele",
            "Violin", "Cello", "Harp", "Flute", "Trumpet", "Saxophone", "Clarinet", "Trombone"
        };

        private void OpenInstrumentPicker(MusicianController m)
        {
            picking = m;
            instrumentPanel.SetActive(true);
            for (int i = instrumentGrid.childCount - 1; i >= 0; i--) Destroy(instrumentGrid.GetChild(i).gameObject);
            SaveSystem.ProgressData prog = Services.Save?.Data.progress;
            foreach (string id in Choices)
            {
                bool known = prog == null || prog.discoveredInstruments.Contains(id) || id == m.defaultInstrument;
                var go = new GameObject(id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback));
                go.transform.SetParent(instrumentGrid, false);
                var img = go.GetComponent<Image>();
                img.sprite = UISkin.Instance.pebble;
                img.type = Image.Type.Sliced;
                InstrumentDefinition d = Services.Instruments.Get(id);
                img.color = known ? Color.Lerp(d != null ? d.color : Color.white, Color.white, 0.55f) : new Color(0.85f, 0.85f, 0.85f);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(150, 150);
                var ii = icon.GetComponent<Image>();
                ii.sprite = known ? InstrumentIcon(id) : UISkin.Instance.iconLock;
                ii.color = known ? Color.white : new Color(0.4f, 0.4f, 0.45f);
                ii.preserveAspect = true;
                ii.raycastTarget = false;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (!known) return;
                    picking.SetInstrument(d, Prefab(id));
                    Services.Engine?.Load(d);
                    picking.React();
                    instrumentPanel.SetActive(false);
                });
            }
        }

        private void BuildSongCards()
        {
            for (int i = songGrid.childCount - 1; i >= 0; i--) Destroy(songGrid.GetChild(i).gameObject);
            foreach (SongLibrary.Song s in SongLibrary.Songs)
            {
                SongLibrary.Song pick = s;
                var go = new GameObject(s.id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback));
                go.transform.SetParent(songGrid, false);
                var img = go.GetComponent<Image>();
                img.sprite = UISkin.Instance.pebble;
                img.type = Image.Type.Sliced;
                img.color = NoteUtil.ColorOf(s.Melody[0].note + s.id.Length * 2);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(140, 140);
                var ii = icon.GetComponent<Image>();
                ii.sprite = SongIcon(s.picture);
                ii.raycastTarget = false;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    song = pick;
                    songPanel.SetActive(false);
                    UpdateSongCard();
                    Preview();
                });
            }
        }

        private Sprite SongIcon(string picture)
        {
            string want = "Icon" + char.ToUpperInvariant(picture[0]) + picture.Substring(1);
            foreach (Sprite s in songIcons) if (s != null && s.name == want) return s;
            return songIcons[Mathf.Abs(picture.GetHashCode()) % songIcons.Length];
        }

        private void UpdateSongCard()
        {
            if (songCardIcon != null) songCardIcon.sprite = SongIcon(song.picture);
        }

        private void Preview()
        {
            // the first phrase on the xylophone so the child hears which song was chosen
            var x = Services.Instruments.Get("Xylophone");
            double t = AudioSettings.dspTime + 0.05;
            List<NoteEvent> m = song.Melody;
            for (int i = 0; i < Mathf.Min(4, m.Count); i++)
                Services.Engine?.NoteOnAt(x, Arranger.Fit(new List<NoteEvent> { m[i] }, x, 0)[0].note, 80, Services.UiChannel, t + m[i].beat * 60.0 / song.tempo);
        }

        private void Perform()
        {
            if (onStage.Count == 0) { melo?.Point(); Guide.Say("orchestra_pick"); return; }
            seq.Clear();
            trackOwner.Clear();
            bool melodyTaken = false;
            // Melody-capable musicians first so the tune is always carried.
            var order = new List<MusicianController>(onStage);
            order.Sort((a, b) => Score(b).CompareTo(Score(a)));
            int ch = 1;
            foreach (MusicianController m in order)
            {
                Arranger.Role role = Arranger.RoleFor(m.Instrument, melodyTaken);
                melodyTaken |= role == Arranger.Role.Melody;
                float pan = m.Spot < 0 ? 0f : Mathf.Lerp(-0.6f, 0.6f, m.Spot / Mathf.Max(1f, stageSpots.Length - 1f));
                seq.AddTrack(m.Instrument, ch++, Arranger.Part(song, m.Instrument, role), pan);
                trackOwner.Add(m);
                m.SetPerforming(true);
            }
            seq.LengthBeats = Arranger.SongLength(song);
            seq.Tempo = song.tempo * tempo.value;
            seq.Play(0f, 0.3);
            playButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(true);
            melo?.SetBase(MascotController.Base.Dance);
        }

        private static int Score(MusicianController m)
        {
            if (m.Instrument == null) return 0;
            switch (m.Instrument.playStyle)
            {
                case PlayStyle.Blow: case PlayStyle.Bow: return 3;
                case PlayStyle.Keyboard: case PlayStyle.Mallets: case PlayStyle.Pluck: return 2;
                default: return 1;
            }
        }

        private void StopShow()
        {
            seq.Stop();
            EndShow(false);
        }

        private void OnFinished() => EndShow(true);

        private void EndShow(bool complete)
        {
            foreach (MusicianController m in onStage)
            {
                m.SetPerforming(false);
                if (complete) m.Celebrate();
            }
            playButton.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(false);
            melo?.SetBase(MascotController.Base.Idle);
            if (!complete) return;
            melo?.Celebrate();
            UISound.Sparkle();
            if (confetti != null) confetti.Play();
            Guide.Say("orchestra_bravo");
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            if (p != null && p.Add(p.completedActivities, "orchestra:" + song.id)) { p.stars++; Services.Save.Save(); }
        }
    }
}
