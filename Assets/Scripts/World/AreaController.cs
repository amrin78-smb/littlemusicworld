using System.Collections.Generic;
using LittleMusicWorld.Audio;
using LittleMusicWorld.Characters;
using LittleMusicWorld.Core;
using LittleMusicWorld.Gameplay;
using LittleMusicWorld.Instruments;
using LittleMusicWorld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleMusicWorld.World
{
    /// <summary>
    /// Area flow shared by Piano Paradise, Drum Valley, String Town and Wind Village:
    /// overview (Melo introduces the area, instruments bob to invite a tap) → tap an instrument → the camera glides to
    /// it and its child-sized play panel opens → Back returns to the overview, Home to the hub. Keyboards also offer
    /// Follow the Melody songs. Playing an instrument the first time "discovers" it (saved, celebrated by Melo).
    /// </summary>
    public sealed class AreaController : MonoBehaviour
    {
        [SerializeField] private string areaId;
        [SerializeField] private string introVoice;
        [SerializeField] private Camera cam;
        [SerializeField] private Transform overview;
        [SerializeField] private MascotController melo;
        [SerializeField] private InstrumentStation[] stations;
        [SerializeField] private InstrumentPanel panel;
        [SerializeField] private GameObject playBar;      // Back button + song controls (shown while playing)
        [SerializeField] private GameObject overviewBar;  // Home button (shown in the overview only)
        [SerializeField] private Button backButton, songsButton, listenButton;
        [SerializeField] private MelodyGuide guide;
        [SerializeField] private SongPicker songPicker;
        [SerializeField] private Image progressFill;

        private InstrumentStation current;
        private readonly System.Collections.Generic.Dictionary<InstrumentStation, Vector3> baseScale =
            new System.Collections.Generic.Dictionary<InstrumentStation, Vector3>();
        private Vector3 camFromPos, camToPos;
        private Quaternion camFromRot, camToRot;
        private float camT = 1f;
        private Vector2 downPos;
        private bool pressing;

        private void Start()
        {
            if (cam == null) cam = Camera.main;
            cam.transform.SetPositionAndRotation(overview.position, overview.rotation);
            panel.Hide();
            playBar.SetActive(false);
            if (overviewBar != null) overviewBar.SetActive(true);
            backButton.onClick.AddListener(Leave);
            if (songsButton != null) songsButton.onClick.AddListener(() => songPicker.Open(StartLesson));
            if (listenButton != null) listenButton.onClick.AddListener(() => guide.Replay());
            panel.Played += OnPlayed;
            foreach (InstrumentStation s in stations)
            {
                if (s.model != null) s.model.Instrument = s.Definition;
                Services.Engine?.Load(s.Definition); // decode behind the loading curtain
            }
            if (guide != null)
            {
                guide.PhaseChanged += OnGuidePhase;
                guide.Progress += (i, n) => { if (progressFill != null) progressFill.fillAmount = n == 0 ? 0 : (float)i / n; };
                guide.HintShown += n => { melo?.Point(); Guide.Say("follow_hint"); };
                guide.Completed += l => { melo?.Celebrate(); Guide.Say("follow_done"); };
            }
            Services.Discover(Services.Save?.Data.progress.visitedAreas, areaId);
            Invoke(nameof(Intro), 0.5f);
            string st = DeviceArgs.Get("lmw_station");
            if (int.TryParse(st, out int si) && si >= 0 && si < stations.Length) StartCoroutine(EnterLater(stations[si]));
        }

        private System.Collections.IEnumerator EnterLater(InstrumentStation s)
        {
            yield return new WaitForSeconds(2.5f);
            Enter(s);
        }

        private void Intro()
        {
            melo?.Wave();
            Guide.Say(introVoice);
        }

        private void Update()
        {
            if (camT < 1f)
            {
                camT = Mathf.Min(1f, camT + Time.deltaTime / 0.7f);
                float e = Ease.InOutSine(camT);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(camFromPos, camToPos, e), Quaternion.Slerp(camFromRot, camToRot, e));
            }
            float bob = Mathf.Sin(Time.time * 2.4f);
            SaveSystem.ProgressData p = Services.Save?.Data.progress;
            foreach (InstrumentStation s in stations)
                if (s.bounce != null)
                {
                    if (!baseScale.TryGetValue(s, out Vector3 b0)) baseScale[s] = b0 = s.bounce.localScale;
                    bool fresh = current == null && p != null && !p.discoveredInstruments.Contains(s.instrumentId);
                    s.bounce.localScale = b0 * (fresh ? 1f + 0.03f * Mathf.Max(0, bob) : 1f);
                }
            if (current == null) HandleTap();
        }

        private void HandleTap()
        {
            Pointer ptr = Pointer.current;
            if (ptr == null || Services.Scenes == null || Services.Scenes.Busy) return;
            if (ptr.press.wasPressedThisFrame)
            {
                pressing = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(PointerId());
                downPos = ptr.position.ReadValue();
            }
            else if (ptr.press.wasReleasedThisFrame && pressing)
            {
                pressing = false;
                Vector2 up = ptr.position.ReadValue();
                if ((up - downPos).magnitude > 40f) return;
                if (Physics.Raycast(cam.ScreenPointToRay(up), out RaycastHit hit, 100f))
                {
                    InstrumentStation s = hit.collider.GetComponentInParent<InstrumentStation>();
                    if (s != null) Enter(s);
                }
            }
        }

        private static int PointerId()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return Touchscreen.current.primaryTouch.touchId.ReadValue();
            return PointerInputModule.kMouseLeftId;
        }

        public InstrumentStation[] Stations => stations;

        public void Enter(InstrumentStation s)
        {
            current = s;
            MoveCamera(s.view);
            InstrumentDefinition def = s.Definition;
            panel.Show(def);
            playBar.SetActive(true);
            if (overviewBar != null) overviewBar.SetActive(false);
            bool keys = def != null && def.playStyle == PlayStyle.Keyboard && guide != null;
            if (songsButton != null) songsButton.gameObject.SetActive(keys);
            if (listenButton != null) listenButton.gameObject.SetActive(false);
            if (progressFill != null) progressFill.transform.parent.gameObject.SetActive(false);
            UISound.Plink(88, 60);
            melo?.SetBase(MascotController.Base.Idle);
        }

        public void Leave()
        {
            if (current == null) return;
            guide?.Stop();
            current = null;
            panel.Hide();
            playBar.SetActive(false);
            if (overviewBar != null) overviewBar.SetActive(true);
            MoveCamera(overview);
        }

        private void MoveCamera(Transform to)
        {
            camFromPos = cam.transform.position;
            camFromRot = cam.transform.rotation;
            camToPos = to.position;
            camToRot = to.rotation;
            camT = 0f;
        }

        private void OnPlayed(int note)
        {
            if (current == null) return;
            if (Services.Discover(Services.Save?.Data.progress.discoveredInstruments, current.instrumentId))
            {
                melo?.Celebrate();
                Guide.Say("new_instrument");
                UISound.Sparkle();
            }
        }

        private void StartLesson(SongLibrary.Lesson l)
        {
            if (panel.Keyboard == null) return;
            if (listenButton != null) listenButton.gameObject.SetActive(true);
            if (progressFill != null) progressFill.transform.parent.gameObject.SetActive(true);
            panel.Keyboard.Flashed -= Pulse;
            panel.Keyboard.Flashed += Pulse;
            guide.Begin(panel.Keyboard, l);
        }

        private void Pulse(int note) => current?.model?.Hit(note);

        private void OnGuidePhase(MelodyGuide.Phase ph)
        {
            if (melo == null) return;
            switch (ph)
            {
                case MelodyGuide.Phase.Demonstrating: melo.SetBase(MascotController.Base.Sing); Guide.Say("follow_watch"); break;
                case MelodyGuide.Phase.Following: melo.SetBase(MascotController.Base.Idle); melo.Point(); Guide.Say("follow_turn"); break;
                case MelodyGuide.Phase.Celebrating: melo.SetBase(MascotController.Base.Dance); break;
                default: melo.SetBase(MascotController.Base.Idle); break;
            }
        }
    }
}
