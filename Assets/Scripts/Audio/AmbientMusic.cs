using LittleMusicWorld.Core;
using UnityEngine;

namespace LittleMusicWorld.Audio
{
    /// <summary>
    /// Soft background music for the menu and hub: a slow, looping arrangement of one of the songs (music-box
    /// xylophone melody, harp chords, cello bass) on the music channel, whose volume is the parent's "Music" setting.
    /// Instrument areas have no background music so the child's own playing is the star.
    /// </summary>
    public sealed class AmbientMusic : MonoBehaviour
    {
        [SerializeField] private string[] songs = { "twinkle", "mary", "rowboat", "odetojoy" };
        [SerializeField] private float tempoScale = 0.8f;
        [SerializeField] private bool bass = true;

        private void Start()
        {
            Sequencer seq = Services.Music;
            InstrumentLibrary lib = Services.Instruments;
            if (seq == null || lib == null) return;
            SongLibrary.Song song = SongLibrary.Get(songs[Random.Range(0, songs.Length)]);
            seq.Clear();
            InstrumentDefinition box = lib.Get("Xylophone"), harp = lib.Get("Harp"), cello = lib.Get("Cello");
            var melody = Arranger.Part(song, box, Arranger.Role.Melody);
            for (int i = 0; i < melody.Count; i++) { NoteEvent e = melody[i]; e.velocity = 58; melody[i] = e; }
            seq.AddTrack(box, Services.MusicChannel, melody, 0.15f);
            var chords = Arranger.ChordTimeline(song, Arranger.SongLength(song));
            var arps = new System.Collections.Generic.List<NoteEvent>();
            foreach (Arranger.ChordSpan c in chords)
                for (float b = 0; b < c.length; b += 1f)
                    arps.Add(new NoteEvent(c.beat + b, 0f, 55 + ((c.pcs[(int)b % 3] - 7 + 12) % 12), 46));
            seq.AddTrack(harp, Services.MusicChannel, arps, -0.2f);
            if (bass)
            {
                var low = Arranger.Part(song, cello, Arranger.Role.Bass);
                for (int i = 0; i < low.Count; i++) { NoteEvent e = low[i]; e.velocity = 50; low[i] = e; }
                seq.AddTrack(cello, Services.MusicChannel, low, 0f);
            }
            seq.LengthBeats = Arranger.SongLength(song) + 4f; // a breath between repeats
            seq.Loop = true;
            seq.Tempo = song.tempo * tempoScale;
            seq.Play(0f, 1.0);
            Services.ApplyVolumes();
        }

        private void OnDestroy()
        {
            if (Services.Music != null) Services.Music.Stop();
        }
    }
}
