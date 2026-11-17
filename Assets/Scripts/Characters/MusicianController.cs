using LittleMusicWorld.Audio;
using LittleMusicWorld.Core;
using UnityEngine;

namespace LittleMusicWorld.Characters
{
    /// <summary>
    /// An animal musician. Holds a small copy of its instrument, loops "Hold&lt;Style&gt;" while performing and
    /// re-triggers "Play&lt;Style&gt;" on every note it plays (driven by the sequencer's NoteVisual), so the movement
    /// happens exactly when the sound starts. Walks on/off stage.
    /// </summary>
    public sealed class MusicianController : MonoBehaviour
    {
        public string animalId;
        public string defaultInstrument;
        [SerializeField] private float walkSpeed = 1.5f;

        public InstrumentDefinition Instrument { get; private set; }
        public string Style { get; private set; } = "Keys";
        public bool Performing { get; private set; }
        public bool Walking { get; private set; }
        public int Spot = -1;

        private Animator animator;
        private Transform held;
        private Vector3 target;
        private float hitUntil;
        private string state;
        private Instruments.InstrumentModel3D heldModel;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        public static string StyleFor(InstrumentDefinition d)
        {
            if (d == null) return "Keys";
            if (d.id == "Maracas" || d.id == "Tambourine") return "Shake";
            switch (d.playStyle)
            {
                case PlayStyle.Keyboard: return "Keys";
                case PlayStyle.DrumPads: case PlayStyle.Mallets: return "Drums";
                case PlayStyle.Strum: case PlayStyle.Pluck: return "Strum";
                case PlayStyle.Bow: return "Bow";
                default: return "Blow";
            }
        }

        /// <summary>Assigns an instrument and builds the small held/placed copy from its prefab (Resources not needed).</summary>
        public void SetInstrument(InstrumentDefinition def, GameObject prefab)
        {
            Instrument = def;
            Style = StyleFor(def);
            if (held != null) Destroy(held.gameObject);
            held = null;
            heldModel = null;
            if (prefab == null) return;
            GameObject copy = Instantiate(prefab);
            foreach (Collider c in copy.GetComponentsInChildren<Collider>()) Destroy(c);
            held = copy.transform;
            heldModel = copy.AddComponent<Instruments.InstrumentModel3D>();
            heldModel.Instrument = def;
            Transform body = FindBone("body") ?? transform;
            // Placement by style: big instruments stand in front, small ones are held against the body.
            switch (Style)
            {
                case "Keys":
                case "Drums":
                    held.SetParent(transform, false);
                    held.localPosition = new Vector3(0f, 0f, 0.62f);
                    held.localRotation = Quaternion.Euler(0, 180f, 0);
                    held.localScale = Vector3.one * (def.id == "DrumKit" ? 0.5f : def.id == "GrandPiano" ? 0.42f : 0.6f);
                    break;
                case "Strum":
                    held.SetParent(body, false);
                    held.position = transform.TransformPoint(new Vector3(0.02f, 0.28f, 0.26f));
                    held.rotation = transform.rotation * Quaternion.Euler(0, 180f, def.id == "Harp" ? 0f : 62f);
                    held.localScale = Vector3.one * (def.id == "Harp" ? 0.45f : 0.62f);
                    if (def.id == "Harp") held.position = transform.TransformPoint(new Vector3(0.3f, 0f, 0.35f));
                    break;
                case "Bow":
                    held.SetParent(body, false);
                    bool cello = def.id == "Cello";
                    held.position = transform.TransformPoint(cello ? new Vector3(0.05f, -0.05f, 0.3f) : new Vector3(-0.1f, 0.25f, 0.22f));
                    held.rotation = transform.rotation * Quaternion.Euler(cello ? -10f : 0f, 180f, cello ? 0f : -40f);
                    held.localScale = Vector3.one * (cello ? 0.38f : 0.6f);
                    break;
                case "Blow":
                    held.SetParent(body, false);
                    held.position = transform.TransformPoint(new Vector3(0f, 0.18f, 0.3f));
                    held.rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
                    held.localScale = Vector3.one * 0.6f;
                    break;
                default: // Shake
                    held.SetParent(body, false);
                    held.position = transform.TransformPoint(new Vector3(0f, 0.05f, 0.3f));
                    held.rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
                    held.localScale = Vector3.one * 0.55f;
                    break;
            }
        }

        private Transform FindBone(string name)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public void SetPerforming(bool on)
        {
            Performing = on;
            Play(on ? "Hold" + Style : "Idle");
        }

        /// <summary>Called on each note this musician plays (exactly when it sounds).</summary>
        public void Hit(int note)
        {
            if (animator == null) return;
            animator.Play("Play" + Style, 0, 0f);
            state = "Play" + Style;
            hitUntil = Time.time + 0.28f;
            heldModel?.Hit(note);
        }

        public void Celebrate()
        {
            Play("Celebrate", 0.1f, true);
            hitUntil = Time.time + 1.2f;
        }

        public void React() { Play("React", 0.1f, true); hitUntil = Time.time + 0.5f; }

        public void WalkTo(Vector3 p)
        {
            target = p;
            Walking = true;
            Play("Walk");
        }

        private void Play(string s, float fade = 0.15f, bool restart = false)
        {
            if (animator == null || (!restart && state == s)) return;
            state = s;
            animator.CrossFadeInFixedTime(s, fade);
        }

        private void Update()
        {
            if (Walking)
            {
                Vector3 to = target - transform.position;
                to.y = 0;
                if (to.magnitude < 0.05f)
                {
                    Walking = false;
                    transform.rotation = Quaternion.LookRotation(Vector3.back); // face the audience (camera at −Z)
                    Play(Performing ? "Hold" + Style : "Idle");
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 540f * Time.deltaTime);
                    transform.position += to.normalized * Mathf.Min(to.magnitude, walkSpeed * Time.deltaTime);
                }
                return;
            }
            if (Time.time < hitUntil) return;
            Play(Performing ? "Hold" + Style : "Idle", 0.12f);
        }
    }
}
