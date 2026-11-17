using System;
using LittleMusicWorld.Audio;

namespace LittleMusicWorld.Instruments
{
    /// <summary>Anything a melody can be followed on: piano keys, wind holes, xylophone bars, harp strings.</summary>
    public interface ISongSurface
    {
        InstrumentDefinition Instrument { get; }
        event Action<int, int> NoteDown;   // note, velocity
        event Action<int> Flashed;
        bool Interactable { get; set; }
        bool Has(int note);
        int LowNote { get; }
        int HighNote { get; }
        void SetGuide(int note, bool on);
        void ClearGuides();
        void Flash(int note);
    }
}
