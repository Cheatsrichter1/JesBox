using UnityEngine;
using UnityEngine.UI;

namespace JesBox.UI
{
    /// <summary>
    /// Builds the TV-screen UI purely from code so the game works right
    /// after dropping GameManager onto an empty GameObject, with no manual
    /// scene wiring required.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color BgDeep = new Color32(0x1B, 0x10, 0x42, 0xFF);
        public static readonly Color Gold = new Color32(0xE8, 0xC7, 0x66, 0xFF);
        public static readonly Color Cream = new Color32(0xFB, 0xF6, 0xEA, 0xFF);
        public static readonly Color PanelTint = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color ChipUnselected = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color ChipTextDark = new Color32(0x2A, 0x1A, 0x08, 0xFF);

        private static Font _font;
        private static Font BuiltinFont => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateFullStretchPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Text CreateText(Transform parent, string content, int fontSize, Color color,
            TextAnchor anchor, Vector2 anchoredPos, Vector2 sizeDelta, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.font = BuiltinFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject("Button", typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            go.GetComponent<Image>().color = Gold;

            CreateText(rt, label, 28, ChipTextDark, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta, FontStyle.Bold);
            return go.GetComponent<Button>();
        }

        /// <summary>An invisible, positionless RectTransform used purely to group
        /// child controls so a whole cluster can be shown/hidden as one unit.</summary>
        public static RectTransform CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        /// <summary>A labeled -/value/+ stepper. Calls onChange immediately with the
        /// clamped initial value, then again on every click.</summary>
        public static Text CreateStepper(Transform parent, string label, Vector2 anchoredPos,
            int min, int max, int step, int initial, System.Action<int> onChange, string suffix = "")
        {
            var container = CreateGroup(parent, $"Stepper_{label}");
            container.anchoredPosition = anchoredPos;

            CreateText(container, label, 18, Gold, TextAnchor.MiddleCenter, new Vector2(0, 34), new Vector2(400, 26));
            var valueText = CreateText(container, "", 34, Cream, TextAnchor.MiddleCenter, new Vector2(0, -14), new Vector2(140, 50), FontStyle.Bold);
            var minusBtn = CreateButton(container, "-", new Vector2(-150, -14), new Vector2(70, 60));
            var plusBtn = CreateButton(container, "+", new Vector2(150, -14), new Vector2(70, 60));

            int value = Mathf.Clamp(initial, min, max);

            void Apply(int v)
            {
                value = Mathf.Clamp(v, min, max);
                valueText.text = value + suffix;
                onChange(value);
            }

            minusBtn.onClick.AddListener(() => Apply(value - step));
            plusBtn.onClick.AddListener(() => Apply(value + step));
            Apply(value);

            return valueText;
        }
    }
}
