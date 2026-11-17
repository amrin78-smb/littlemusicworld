using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.SaveSystem;
using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LittleMusicWorld.MusicStudio
{
    /// <summary>
    /// Music Studio (GDD 5.7): up to four coloured tracks on a 4-bar loop. Pick a track (its instrument appears below),
    /// press Record: four count-in ticks, then the loop plays while the child plays — every note and its timing is
    /// captured (gently quantised to eighth notes) and layered pass after pass. Tracks can be muted, turned down,
    /// cleared or removed; notes are removed by tapping them on the timeline. Songs save locally (versioned) and load
    /// from colour cards. Records note events, never the microphone.
    /// </summary>
    public sealed class StudioManager : MonoBehaviour
    {
        public const int MaxTracks = 4;
        public const float Quantise = 0.5f;
        public const int FirstChannel = 2;

        [System.Serializable]
        public sealed class Lane
        {
            public RectTransform root;
            public Button select, instrument, mute, clear;
            public Image instrumentIcon, muteIcon, highlight;
            public Slider volume;
            public RectTransform notes;
        }

        [SerializeField] private Lane[] lanes;
        [SerializeField] private InstrumentPanel panel;
        [SerializeField] private Button playButton, stopButton, recordButton, saveButton, songsButton, newButton;
        [SerializeField] private Image recordGlow;
        [SerializeField] private Slider tempoSlider;
        [SerializeField] private RectTransform playhead;
        [SerializeField] private RectTransform countIn;      // 4 dots
        [SerializeField] private GameObject pickerPanel;     // instrument picker
        [SerializeField] private RectTransform pickerGrid;
        [SerializeField] private GameObject songsPanel;
        [SerializeField] private RectTransform songsGrid;
        [SerializeField] private Sprite[] portraits;          // "Portrait_<id>"
        [SerializeField] private Sprite blockSprite;
        [SerializeField] private MascotController melo;

        private CompositionData song;
        private int selected;
        private Sequencer seq;
        private bool recording;
        private readonly List<NoteEvent> pending = new List<NoteEvent>();
        private readonly Dictionary<int, (float beat, double dsp)> open = new Dictionary<int, (float, double)>();
        private readonly List<GameObject>[] blocks = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
        private double countEnd;
        private int countTicks;
        private Sequencer clickSeq;

        public CompositionData Song => song;
        public bool Recording => recording;

        private void Start()
        {
            seq = gameObject.AddComponent<Sequencer>();
            seq.Loop = true;
            seq.Looped += MergePending;
            clickSeq = gameObject.AddComponent<Sequencer>();
            for (int i = 0; i < lanes.Length; i++)
            {
                int k = i;
                lanes[i].select.onClick.AddListener(() => Select(k));
                lanes[i].instrument.onClick.AddListener(() => OpenPicker(k));
                lanes[i].mute.onClick.AddListener(() => ToggleMute(k));
                lanes[i].clear.onClick.AddListener(() => ClearTrack(k));
                lanes[i].volume.onValueChanged.AddListener(v => SetVolume(k, v));
            }
            playButton.onClick.AddListener(Play);
            stopButton.onClick.AddListener(StopAll);
            recordButton.onClick.AddListener(Record);
            saveButton.onClick.AddListener(SaveSong);
            songsButton.onClick.AddListener(OpenSongs);
            newButton.onClick.AddListener(() => { StopAll(); NewSong(); });
            tempoSlider.minValue = 70;
            tempoSlider.maxValue = 140;
            tempoSlider.onValueChanged.AddListener(v => { song.tempo = v; seq.Tempo = v; });
            Services.Engine.NoteStarted += OnNoteOn;
            Services.Engine.NoteStopped += OnNoteOff;
            pickerPanel.SetActive(false);
            songsPanel.SetActive(false);
            countIn.gameObject.SetActive(false);
            NewSong();
            Services.Discover(Services.Save?.Data.progress.visitedAreas, SceneFlow.MusicStudio);
            Invoke(nameof(Intro), 0.4f);
        }

        private void OnDestroy()
        {
            if (Services.Engine == null) return;
            Services.Engine.NoteStarted -= OnNoteOn;
            Services.Engine.NoteStopped -= OnNoteOff;
        }

        private void Intro()
        {
            melo?.Wave();
            Guide.Say("area_studio");
        }

        // ------------------------------------------------------------------ song / tracks

        public void NewSong()
        {
            song = new CompositionData { tempo = 100f, lengthBeats = 16f, title = "Song " + ((Services.Save?.Data.compositionIds.Count ?? 0) + 1) };
            song.tracks.Add(new TrackData { instrumentId = "GrandPiano" });
            song.tracks.Add(new TrackData { instrumentId = "DrumKit" });
            tempoSlider.SetValueWithoutNotify(song.tempo);
            Select(0);
            RefreshAll();
        }

        public void Load(CompositionData c)
        {
            StopAll();
            song = c;
            foreach (TrackData t in song.tracks)
            {
                // Graceful: an instrument that no longer exists (older save) falls back to the piano.
                if (Services.Instruments.Get(t.instrumentId) == null) t.instrumentId = "GrandPiano";
                Services.Engine?.Load(Services.Instruments.Get(t.instrumentId));
            }
            while (song.tracks.Count > MaxTracks) song.tracks.RemoveAt(song.tracks.Count - 1);
            tempoSlider.SetValueWithoutNotify(song.tempo);
            Select(0);
            RefreshAll();
        }

        private TrackData Track(int i) => i < song.tracks.Count ? song.tracks[i] : null;

        private void Select(int i)
        {
            if (i >= song.tracks.Count)
            {
                if (song.tracks.Count >= MaxTracks) return;
                song.tracks.Add(new TrackData { instrumentId = DefaultFor(song.tracks.Count) });
                i = song.tracks.Count - 1;
                RefreshAll();
            }
            selected = i;
            InstrumentDefinition def = Services.Instruments.Get(Track(i).instrumentId);
            panel.Show(def);
            for (int k = 0; k < lanes.Length; k++) lanes[k].highlight.enabled = k == selected;
        }

        private static string DefaultFor(int index) => index switch { 0 => "GrandPiano", 1 => "DrumKit", 2 => "AcousticGuitar", _ => "Flute" };

        private void OpenPicker(int lane)
        {
            if (recording) return;
            if (lane >= song.tracks.Count) { Select(lane); return; }
            pickerPanel.SetActive(true);
            for (int i = pickerGrid.childCount - 1; i >= 0; i--) Destroy(pickerGrid.GetChild(i).gameObject);
            ProgressData p = Services.Save?.Data.progress;
            foreach (InstrumentDefinition d in Services.Instruments.instruments)
            {
                bool known = p == null || p.discoveredInstruments.Contains(d.id) || d.id == "GrandPiano" || d.id == "DrumKit";
                if (!known) continue;
                InstrumentDefinition def = d;
                Button b = Card(pickerGrid, Portrait(d.id), Color.Lerp(d.color, Color.white, 0.55f));
                b.onClick.AddListener(() =>
                {
                    Track(lane).instrumentId = def.id;
                    Services.Engine?.Load(def);
                    pickerPanel.SetActive(false);
                    Select(lane);
                    RefreshAll();
                    RebuildSequence();
                });
            }
        }

        private void ToggleMute(int i)
        {
            TrackData t = Track(i);
            if (t == null) return;
            t.muted = !t.muted;
            Services.Engine?.SetChannelMute(FirstChannel + i, t.muted);
            RefreshLane(i);
        }

        private void SetVolume(int i, float v)
        {
            TrackData t = Track(i);
            if (t == null) return;
            t.volume = v;
            Services.Engine?.SetChannelVolume(FirstChannel + i, v);
        }

        /// <summary>Trash: first clears the notes; pressing again on an empty track removes the track.</summary>
        private void ClearTrack(int i)
        {
            TrackData t = Track(i);
            if (t == null || recording) return;
            if (t.notes.Count > 0) t.notes.Clear();
            else if (song.tracks.Count > 1)
            {
                song.tracks.RemoveAt(i);
                if (selected >= song.tracks.Count) selected = song.tracks.Count - 1;
                Select(selected);
            }
            UISound.Plink(60, 70);
            RefreshAll();
            RebuildSequence();
        }

        // ------------------------------------------------------------------ transport

        private void RebuildSequence()
        {
            bool was = seq.IsPlaying;
            double beat = seq.CurrentBeat;
            seq.Clear();
            for (int i = 0; i < song.tracks.Count; i++)
            {
                TrackData t = song.tracks[i];
                Services.Engine?.SetChannelVolume(FirstChannel + i, t.volume);
                Services.Engine?.SetChannelMute(FirstChannel + i, t.muted);
                seq.AddTrack(Services.Instruments.Get(t.instrumentId), FirstChannel + i, t.notes);
            }
            seq.LengthBeats = song.lengthBeats;
            seq.Loop = true;
            seq.Tempo = song.tempo;
            if (was) seq.Play((float)(beat % song.lengthBeats));
        }

        public void Play()
        {
            RebuildSequence();
            seq.Play();
            melo?.SetBase(MascotController.Base.Dance);
            playButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(true);
        }

        public void StopAll()
        {
            seq.Stop();
            clickSeq.Stop();
            if (recording) EndRecording();
            countIn.gameObject.SetActive(false);
            melo?.SetBase(MascotController.Base.Idle);
            playButton.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(false);
        }

        private void Record()
        {
            if (recording) { EndRecording(); return; }
            // count-in: four ticks, then the loop starts with recording armed
            RebuildSequence();
            clickSeq.Clear();
            var ticks = new List<NoteEvent>();
            for (int b = 0; b < 4; b++) ticks.Add(new NoteEvent(b, 0f, b == 0 ? 76 : 75, b == 0 ? 110 : 90));
            clickSeq.AddTrack(Services.Instruments.Get("Congas") ?? Services.Instruments.Get("DrumKit"), 12, ticks);
            clickSeq.Tempo = song.tempo;
            clickSeq.Play(0f, 0.1);
            double beatSec = 60.0 / song.tempo;
            countEnd = AudioSettings.dspTime + 0.1 + 4 * beatSec;
            countTicks = 0;
            countIn.gameObject.SetActive(true);
            recording = true;
            pending.Clear();
            open.Clear();
            recordGlow.enabled = true;
            seq.Stop();
            Invoke(nameof(StartLoopAfterCount), (float)(countEnd - AudioSettings.dspTime) - 0.12f);
            Guide.Silence();
        }

        private void StartLoopAfterCount()
        {
            if (!recording) return;
            RebuildSequence();
            // add a soft metronome while recording (channel 12, hi-hat on each beat)
            seq.AddTrack(Services.Instruments.Get("DrumKit"), 12, Metronome());
            seq.Play(0f, countEnd - AudioSettings.dspTime);
            countIn.gameObject.SetActive(false);
            playButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(true);
            melo?.SetBase(MascotController.Base.Dance);
        }

        private List<NoteEvent> Metronome()
        {
            var l = new List<NoteEvent>();
            for (int b = 0; b < song.lengthBeats; b++) l.Add(new NoteEvent(b, 0f, 42, b % 4 == 0 ? 70 : 45));
            return l;
        }

        private void EndRecording()
        {
            recording = false;
            recordGlow.enabled = false;
            MergePending();
            CloseOpenNotes();
            RebuildSequence();
            if (Track(selected) != null && Track(selected).notes.Count > 0) { melo?.Celebrate(); Guide.Say("great_job"); }
        }

        // ------------------------------------------------------------------ capture

        private float NowBeat()
        {
            double b = seq.CurrentBeat;
            float len = song.lengthBeats;
            return (float)(((b % len) + len) % len);
        }

        private void OnNoteOn(InstrumentDefinition d, int note, int vel, int ch)
        {
            if (!recording || ch != MusicalAudioEngine.FreePlayChannel || !seq.IsPlaying) return;
            TrackData t = Track(selected);
            if (t == null || d == null || d.id != t.instrumentId) return;
            float beat = Mathf.Round(NowBeat() / Quantise) * Quantise % song.lengthBeats;
            if (d.oneShot || d.playStyle == PlayStyle.DrumPads || d.playStyle == PlayStyle.Strum || d.playStyle == PlayStyle.Mallets)
                pending.Add(new NoteEvent(beat, d.oneShot ? 0f : 1f, note, vel));
            else open[note] = (beat, AudioSettings.dspTime);
            AddBlock(selected, new NoteEvent(beat, 0.5f, note, vel));
        }

        private void OnNoteOff(InstrumentDefinition d, int note, int ch)
        {
            if (!recording || ch != MusicalAudioEngine.FreePlayChannel || !open.TryGetValue(note, out var o)) return;
            open.Remove(note);
            float held = (float)((AudioSettings.dspTime - o.dsp) * song.tempo / 60.0);
            pending.Add(new NoteEvent(o.beat, Mathf.Clamp(Mathf.Round(held / Quantise) * Quantise, Quantise, 4f), note, 100));
        }

        private void CloseOpenNotes()
        {
            foreach (var kv in open) pending.Add(new NoteEvent(kv.Value.beat, 1f, kv.Key, 100));
            open.Clear();
            MergePending();
        }

        /// <summary>At each loop wrap the notes just played join the track, so the child hears them next time round.</summary>
        private void MergePending()
        {
            if (pending.Count == 0) return;
            TrackData t = Track(selected);
            if (t != null)
            {
                foreach (NoteEvent e in pending)
                    if (!t.notes.Exists(n => n.note == e.note && Mathf.Abs(n.beat - e.beat) < 0.01f)) t.notes.Add(e);
                t.notes.Sort((a, b) => a.beat.CompareTo(b.beat));
                if (selected < seq.Tracks.Count)
                {
                    seq.Tracks[selected].events.Clear();
                    seq.Tracks[selected].events.AddRange(t.notes);
                }
            }
            pending.Clear();
            RefreshLane(selected);
        }

        // ------------------------------------------------------------------ timeline

        private void RefreshAll()
        {
            for (int i = 0; i < lanes.Length; i++) RefreshLane(i);
        }

        private void RefreshLane(int i)
        {
            Lane lane = lanes[i];
            TrackData t = Track(i);
            foreach (GameObject g in blocks[i]) Destroy(g);
            blocks[i].Clear();
            bool exists = t != null;
            lane.instrumentIcon.sprite = exists ? Portrait(t.instrumentId) : UISkin.Instance.iconStar;
            lane.instrumentIcon.color = exists ? Color.white : new Color(1, 1, 1, 0.5f);
            lane.mute.gameObject.SetActive(exists);
            lane.clear.gameObject.SetActive(exists);
            lane.volume.gameObject.SetActive(exists);
            if (!exists) return;
            lane.volume.SetValueWithoutNotify(t.volume);
            lane.muteIcon.color = t.muted ? new Color(1, 1, 1, 0.35f) : Color.white;
            foreach (NoteEvent e in t.notes) AddBlock(i, e);
        }

        private void AddBlock(int laneIndex, NoteEvent e)
        {
            Lane lane = lanes[laneIndex];
            Rect r = lane.notes.rect;
            var go = new GameObject("Note", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(lane.notes, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0.5f);
            float w = Mathf.Max(18f, r.width * Mathf.Max(e.length, 0.4f) / song.lengthBeats);
            float pitchY = Mathf.InverseLerp(30f, 96f, e.note);
            rt.anchoredPosition = new Vector2(r.width * e.beat / song.lengthBeats, Mathf.Lerp(14f, r.height - 14f, pitchY));
            rt.sizeDelta = new Vector2(w, 22f);
            var img = go.GetComponent<Image>();
            img.sprite = blockSprite;
            img.type = Image.Type.Sliced;
            img.color = NoteUtil.ColorOf(e.note);
            NoteEvent captured = e;
            go.GetComponent<Button>().onClick.AddListener(() => RemoveNote(laneIndex, captured));
            blocks[laneIndex].Add(go);
        }

        private void RemoveNote(int laneIndex, NoteEvent e)
        {
            TrackData t = Track(laneIndex);
            if (t == null) return;
            int idx = t.notes.FindIndex(n => n.note == e.note && Mathf.Abs(n.beat - e.beat) < 0.01f);
            if (idx < 0) return;
            t.notes.RemoveAt(idx);
            UISound.Plink(72, 50);
            RefreshLane(laneIndex);
            if (laneIndex < seq.Tracks.Count)
            {
                seq.Tracks[laneIndex].events.Clear();
                seq.Tracks[laneIndex].events.AddRange(t.notes);
            }
        }

        private void Update()
        {
            if (playhead != null)
            {
                bool show = seq.IsPlaying;
                playhead.gameObject.SetActive(show);
                if (show)
                {
                    RectTransform parent = (RectTransform)playhead.parent;
                    playhead.anchoredPosition = new Vector2(parent.rect.width * NowBeat() / song.lengthBeats, playhead.anchoredPosition.y);
                }
            }
            if (recordGlow != null && recordGlow.enabled) recordGlow.color = new Color(1f, 0.25f, 0.25f, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
            if (countIn.gameObject.activeSelf)
            {
                int beat = Mathf.Clamp(4 - Mathf.CeilToInt((float)((countEnd - AudioSettings.dspTime) * song.tempo / 60.0)), 0, 4);
                for (int i = 0; i < countIn.childCount; i++) countIn.GetChild(i).localScale = Vector3.one * (i < beat ? 1.3f : 0.8f);
            }
        }

        // ------------------------------------------------------------------ save / load

        private void SaveSong()
        {
            if (recording) EndRecording();
            Services.Save?.SaveComposition(song);
            melo?.Celebrate();
            UISound.Sparkle();
            Guide.Say("studio_saved");
        }

        private void OpenSongs()
        {
            StopAll();
            songsPanel.SetActive(true);
            for (int i = songsGrid.childCount - 1; i >= 0; i--) Destroy(songsGrid.GetChild(i).gameObject);
            List<CompositionData> list = Services.Save?.LoadAllCompositions() ?? new List<CompositionData>();
            foreach (CompositionData c in list)
            {
                CompositionData pick = c;
                Button b = Card(songsGrid, Portrait(c.tracks.Count > 0 ? c.tracks[0].instrumentId : "GrandPiano"), NoteUtil.PitchColors[(c.id.GetHashCode() & 0x7fffffff) % 12]);
                b.onClick.AddListener(() => { songsPanel.SetActive(false); Load(pick); Play(); });
                // hold-to-delete trash corner (grown-up friendly, child-safe)
                var trash = new GameObject("Delete", typeof(RectTransform), typeof(Image), typeof(HoldButton));
                trash.transform.SetParent(b.transform, false);
                var tr = (RectTransform)trash.transform;
                tr.sizeDelta = new Vector2(64, 64);
                tr.anchoredPosition = new Vector2(80, -80);
                trash.GetComponent<Image>().sprite = UISkin.Instance.iconTrash;
                trash.GetComponent<HoldButton>().Held += () => { Services.Save.DeleteComposition(pick.id); OpenSongs(); };
            }
        }

        private Button Card(RectTransform grid, Sprite icon, Color colour)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback));
            go.transform.SetParent(grid, false);
            var img = go.GetComponent<Image>();
            img.sprite = UISkin.Instance.pebble;
            img.type = Image.Type.Sliced;
            img.color = colour;
            var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            ic.transform.SetParent(go.transform, false);
            ((RectTransform)ic.transform).sizeDelta = new Vector2(140, 140);
            var ii = ic.GetComponent<Image>();
            ii.sprite = icon;
            ii.preserveAspect = true;
            ii.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private Sprite Portrait(string id)
        {
            foreach (Sprite s in portraits) if (s != null && s.name == "Portrait_" + id) return s;
            return UISkin.Instance.note;
        }
    }
}
