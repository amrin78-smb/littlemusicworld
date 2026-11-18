using System.Collections.Generic;
using LittleMusicWorld.Characters;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using E = LittleMusicWorld.EditorTools.EditorAssetUtil;

namespace LittleMusicWorld.EditorTools
{
    /// <summary>
    /// Editor-side UI construction kit: the brand font (Fredoka, Thai via Mitr fallback), pebble buttons with icons,
    /// panels, sliders, labels and Melo's speech bubble. Layout reference 1920×1080; child targets ≥ 150 px.
    /// </summary>
    public static class UIKit
    {
        public const string FontPath = "Assets/Art/UI/Fonts/Fredoka-SemiBold.ttf";
        public const string FontAssetPath = "Assets/Art/UI/Fonts/Fredoka SDF.asset";
        public const string ThaiFontPath = "Assets/Art/UI/Fonts/Mitr-Medium.ttf";
        public const string ThaiFontAssetPath = "Assets/Art/UI/Fonts/Mitr SDF.asset";
        public static readonly Color Ink = E.Hex("#2E2A4A");
        public static readonly Color Cream = E.Hex("#FFF4E0");

        private static TMP_FontAsset s_Font;

        public static TMP_FontAsset Font()
        {
            if (s_Font != null) return s_Font;
            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (s_Font == null)
            {
                s_Font = Create(FontPath, FontAssetPath, "Fredoka SDF");
                s_Font.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·");
            }
            TMP_FontAsset thai = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ThaiFontAssetPath) ?? Create(ThaiFontPath, ThaiFontAssetPath, "Mitr SDF");
            string thaiChars = ThaiCharacters();
            if (thaiChars.Length > 0) thai.TryAddCharacters(thaiChars, out _);
            s_Font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!s_Font.fallbackFontAssetTable.Contains(thai)) s_Font.fallbackFontAssetTable.Add(thai);
            EditorUtility.SetDirty(thai);
            EditorUtility.SetDirty(s_Font);
            AssetDatabase.SaveAssets();
            return s_Font;
        }

        private static TMP_FontAsset Create(string ttf, string path, string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            var fa = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = name;
            AssetDatabase.CreateAsset(fa, path);
            fa.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (Texture2D t in fa.atlasTextures)
            {
                t.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(t, fa);
            }
            return fa;
        }

        /// <summary>Every Thai character used by Melo's lines (so the atlas is pre-filled; it stays dynamic too).</summary>
        private static string ThaiCharacters()
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Voice/voice_lines.json");
            if (ta == null) return "";
            var set = new HashSet<char>();
            foreach (char c in ta.text) if (c >= 0x0E00 && c <= 0x0E7F) set.Add(c);
            return new string(new List<char>(set).ToArray());
        }

        // ------------------------------------------------------------------ primitives

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
            => SceneBuilder.Rect(name, parent, anchor, pos, size);

        public static RectTransform Stretch(string name, Transform parent, float margin = 0f)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static Image Image(string name, Transform parent, string sprite, Color c, Vector2 anchor, Vector2 pos, Vector2 size, bool raycast = false)
        {
            RectTransform rt = Rect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? UISprites.Get(sprite) : null;
            if (img.sprite != null && img.sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = c;
            img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color c, Vector2 anchor, Vector2 pos, Vector2 box,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
        {
            RectTransform rt = Rect(name, parent, anchor, pos, box);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font();
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            if (outline)
            {
                t.outlineWidth = 0.2f;
                t.outlineColor = Ink;
            }
            return t;
        }

        public static Button Pebble(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string colour, string icon,
            float iconScale = 0.55f, Color? iconColour = null)
        {
            Button b = SceneBuilder.PebbleButton(name, parent, anchor, pos, size, E.Hex(colour));
            if (icon != null)
            {
                Image i = Image("Icon", b.transform, icon, iconColour ?? Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 6), size * iconScale);
                i.preserveAspect = true;
            }
            return b;
        }

        public static Slider Slider(string name, Transform parent, Vector2 anchor, Vector2 pos, float width, string colour)
        {
            RectTransform root = Rect(name, parent, anchor, pos, new Vector2(width, 70));
            var s = root.gameObject.AddComponent<Slider>();
            Image bg = Image("Track", root, "Panel", E.Hex("#E8DCCB"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 26));
            RectTransform fillArea = Rect("Fill Area", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 20, 26));
            Image fill = Image("Fill", fillArea, "Panel", E.Hex(colour), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 26));
            fill.rectTransform.anchorMin = new Vector2(0, 0);
            fill.rectTransform.anchorMax = new Vector2(1, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            RectTransform handleArea = Rect("Handle Area", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 60, 70));
            Image handle = Image("Handle", handleArea, "Circle", Color.white, new Vector2(0, 0.5f), Vector2.zero, new Vector2(66, 66), true);
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0;
            s.maxValue = 1;
            s.value = 0.8f;
            return s;
        }

        /// <summary>
        /// Melo's dialog box: coral frame, Melo's portrait in a round window on the left, the line's words on the right
        /// (left-aligned, auto-sized to fit). Text-only; never takes touches.
        /// </summary>
        public static SpeechBubble Bubble(Transform parent, Vector2 anchor, Vector2 pos, float width = 900f, float height = 170f)
        {
            RectTransform rt = Rect("Dialog Box", parent, anchor, pos, new Vector2(width, height));
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            Color coral = E.Hex("#FF7A59");
            Image("Shadow", rt, "Panel", new Color(0.18f, 0.16f, 0.29f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(0, -9), new Vector2(width + 12, height + 12));
            Image("Frame", rt, "Panel", coral, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width + 12, height + 12));
            Image("Panel", rt, "Panel", new Color(1f, 0.985f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            float px = -width / 2f + height / 2f + 4f;
            float pd = height - 34f;
            Image("PortraitRing", rt, "Circle", coral, new Vector2(0.5f, 0.5f), new Vector2(px, 0), new Vector2(pd + 12, pd + 12));
            Image("PortraitBg", rt, "Circle", E.Hex("#BFE6FF"), new Vector2(0.5f, 0.5f), new Vector2(px, 0), new Vector2(pd, pd));
            Image face = Image("Portrait", rt, null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(px, -2), new Vector2(pd * 0.98f, pd * 0.98f));
            face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitRenderer.Folder + "/Portrait_MeloFace.png");
            if (face.sprite == null) face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitRenderer.Folder + "/Portrait_Melo.png");
            face.preserveAspect = true;
            if (face.sprite == null) face.enabled = false;
            float left = px + pd / 2f + 26f, right = width / 2f - 30f;
            TextMeshProUGUI t = Text("Text", rt, "", 46, Ink, new Vector2(0.5f, 0.5f), new Vector2((left + right) / 2f, 0), new Vector2(right - left, height - 30),
                TextAlignmentOptions.MidlineLeft);
            t.enableAutoSizing = true;
            t.fontSizeMin = 30;
            t.fontSizeMax = 46;
            var bubble = rt.gameObject.AddComponent<SpeechBubble>();
            E.Set(bubble, "label", t);
            E.Set(bubble, "group", cg);
            return bubble;
        }
    }
}
