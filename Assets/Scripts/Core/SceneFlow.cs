using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleMusicWorld.Core
{
    /// <summary>
    /// Scene transitions behind a branded loading curtain (logo, sky gradient, bouncing notes). Only one scene is
    /// loaded at a time, which keeps memory low on budget phones; the previous scene's instruments stay decoded in
    /// the audio engine only while they are in the current area's instrument set.
    /// </summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        public const string Boot = "Boot", MainMenu = "MainMenu", Hub = "Hub", PianoParadise = "PianoParadise",
            DrumValley = "DrumValley", StringTown = "StringTown", WindVillage = "WindVillage",
            OrchestraPark = "OrchestraPark", MusicStudio = "MusicStudio", RhythmCarnival = "RhythmCarnival";

        public bool Busy { get; private set; }
        public string Current => SceneManager.GetActiveScene().name;
        public string Previous { get; private set; }
        public event Action<string> Loaded;

        private CanvasGroup curtain;
        private RectTransform[] notes;

        public void Load(string scene, float minimumShow = 0.6f)
        {
            if (Busy || string.IsNullOrEmpty(scene)) return;
            StartCoroutine(Run(scene, minimumShow));
        }

        private IEnumerator Run(string scene, float minimumShow)
        {
            Busy = true;
            Previous = Current;
            EnsureCurtain();
            curtain.gameObject.SetActive(true);
            curtain.blocksRaycasts = true;
            bool shown = false;
            Tween.Fade(curtain, 1f, 0.25f, () => shown = true);
            while (!shown) yield return null;
            Services.Engine?.StopAll();
            Services.Music?.Stop();

            float start = Time.unscaledTime;
            AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError("[LMW] Scene not in build: " + scene);
                Busy = false;
                Tween.Fade(curtain, 0f, 0.2f, () => curtain.gameObject.SetActive(false));
                yield break;
            }
            while (!op.isDone) yield return null;
            Resources.UnloadUnusedAssets();
            while (Time.unscaledTime - start < minimumShow) yield return null;
            yield return null; // let scene Start() run behind the curtain

            Loaded?.Invoke(scene);
            curtain.blocksRaycasts = false;
            Tween.Fade(curtain, 0f, 0.35f, () => curtain.gameObject.SetActive(false));
            Busy = false;
        }

        private void Update()
        {
            if (curtain == null || !curtain.gameObject.activeSelf || notes == null) return;
            float t = Time.unscaledTime;
            for (int i = 0; i < notes.Length; i++)
            {
                float y = Mathf.Abs(Mathf.Sin(t * 3.2f + i * 0.7f)) * 40f;
                notes[i].anchoredPosition = new Vector2(notes[i].anchoredPosition.x, -330f + y);
                notes[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2f + i) * 10f);
            }
        }

        /// <summary>The curtain is built from Resources/Brand sprites so it exists before any scene loads.</summary>
        private void EnsureCurtain()
        {
            if (curtain != null) return;
            var go = new GameObject("Loading Curtain", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            curtain = go.GetComponent<CanvasGroup>();
            curtain.alpha = 0f;

            Image bg = Child<Image>(go.transform, "Background", Vector2.zero, Vector2.zero, true);
            bg.sprite = Resources.Load<Sprite>("Brand/LoadingBackground");
            bg.color = bg.sprite != null ? Color.white : new Color(0.56f, 0.83f, 1f);
            Image logo = Child<Image>(go.transform, "Logo", new Vector2(0, 60), new Vector2(980, 560), false);
            logo.sprite = Resources.Load<Sprite>("Brand/Logo");
            logo.preserveAspect = true;
            logo.enabled = logo.sprite != null;

            Sprite note = Resources.Load<Sprite>("Brand/Note");
            notes = new RectTransform[5];
            for (int i = 0; i < notes.Length; i++)
            {
                Image n = Child<Image>(go.transform, "Note" + i, new Vector2(-240 + i * 120, -330), new Vector2(84, 84), false);
                n.sprite = note;
                n.color = Audio.NoteUtil.PitchColors[(i * 2) % 12];
                n.enabled = note != null;
                notes[i] = n.rectTransform;
            }
            go.SetActive(false);
        }

        private static T Child<T>(Transform parent, string name, Vector2 pos, Vector2 size, bool stretch) where T : Graphic
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var rt = (RectTransform)g.transform;
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
            else { rt.anchoredPosition = pos; rt.sizeDelta = size; }
            T graphic = g.AddComponent<T>();
            graphic.raycastTarget = stretch;
            return graphic;
        }
    }
}
