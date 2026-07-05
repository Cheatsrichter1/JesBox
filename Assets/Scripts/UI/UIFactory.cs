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

            CreateText(rt, label, 28, new Color32(0x2A, 0x1A, 0x08, 0xFF), TextAnchor.MiddleCenter, Vector2.zero, sizeDelta, FontStyle.Bold);
            return go.GetComponent<Button>();
        }
    }
}
