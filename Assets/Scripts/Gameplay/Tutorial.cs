using LittleMusicWorld.Core;

namespace LittleMusicWorld.Gameplay
{
    /// <summary>
    /// First-play tutorial state (saved). Hub part: walk, golden notes, choose a gate. Area part: choose an instrument,
    /// play it, songs, back. Run by HubController / AreaController; replayable from the grown-ups panel.
    /// </summary>
    public static class Tutorial
    {
        public const int HubPart = 0, AreaPart = 1, Done = 2;

        public static int Step
        {
            get => Services.Save?.Data.progress.tutorialStep ?? Done;
            set
            {
                if (Services.Save == null || Services.Save.Data.progress.tutorialStep == value) return;
                Services.Save.Data.progress.tutorialStep = value;
                Services.Save.Save();
            }
        }

        public static bool Active => Step < Done;

        public static void Restart() => Step = HubPart;
    }
}
