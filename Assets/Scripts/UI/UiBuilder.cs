using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace bison.core.UI
{
    public enum UiAnchor
    {
        Center,
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public static class UiPalette
    {
        public static Color Orange { get; } = new Color(1.00f, 0.48f, 0.10f);
        public static Color OrangeDark { get; } = new Color(0.29f, 0.18f, 0.11f);
        public static Color Panel { get; } = new Color(0.12f, 0.12f, 0.13f);
        public static Color LockedFill { get; } = new Color(0.13f, 0.13f, 0.14f);
        public static Color Line { get; } = new Color(0.30f, 0.30f, 0.32f);
        public static Color Gold { get; } = new Color(1.00f, 0.80f, 0.25f);
        public static Color Cyan { get; } = new Color(0.40f, 0.85f, 1.00f);
        public static Color Green { get; } = new Color(0.35f, 0.85f, 0.45f);
        public static Color Dim { get; } = new Color(0.60f, 0.60f, 0.63f);
        public static Color Grey { get; } = new Color(0.25f, 0.25f, 0.27f);
        public static Color NeutralArt { get; } = new Color(0.75f, 0.77f, 0.80f);
        public static Color MessageText { get; } = new Color(0.85f, 0.85f, 0.88f);
        public static Color MenuBackground { get; } = new Color(0.07f, 0.07f, 0.08f);
        public static Color ToastBackground { get; } = new Color(0.05f, 0.05f, 0.06f, 0.95f);
        public static Color OverlayDim { get; } = new Color(0f, 0f, 0f, 0.75f);
        public static Color DialogDim { get; } = new Color(0f, 0f, 0f, 0.8f);
    }

    public static class UiBuilder
    {
        private const int FontCreationSize = 40;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ScreenMatchBalance = 0.5f;

        private static readonly string[] FontNames =
        {
            "Arial", "Roboto", "Helvetica", "Liberation Sans", "DejaVu Sans", "Segoe UI"
        };

        private static Font defaultFont;

        // Canvas

        public static Canvas CreateCanvas(Transform parent, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(nameof(Canvas));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ScreenMatchBalance;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // Elements

        public static RectTransform CreateRect(Transform parent)
        {
            GameObject rectObject = new GameObject(nameof(RectTransform), typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return (RectTransform)rectObject.transform;
        }

        public static Image CreateImage(Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(
                nameof(Image), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(Transform parent, string content, int fontSize,
                                      TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(
                nameof(Text), typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string label, int fontSize, Color fillColor,
                                          UiAnchor anchor, Vector2 offset, Vector2 size)
        {
            Image background = CreateImage(parent, fillColor);
            Place(background.rectTransform, anchor, offset, size);

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            Text labelText = CreateText(background.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(labelText.rectTransform, 0f);

            return button;
        }

        // Layout

        public static void Place(RectTransform rectTransform, UiAnchor anchor, Vector2 offset, Vector2 size)
        {
            Vector2 anchorPoint = GetAnchorPoint(anchor);

            rectTransform.anchorMin = anchorPoint;
            rectTransform.anchorMax = anchorPoint;
            rectTransform.pivot = anchorPoint;
            rectTransform.anchoredPosition = offset;
            rectTransform.sizeDelta = size;
        }

        public static void Stretch(RectTransform rectTransform, float inset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(inset, inset);
            rectTransform.offsetMax = new Vector2(-inset, -inset);
        }

        private static Vector2 GetAnchorPoint(UiAnchor anchor)
        {
            switch (anchor)
            {
                case UiAnchor.TopLeft: return new Vector2(0f, 1f);
                case UiAnchor.TopCenter: return new Vector2(0.5f, 1f);
                case UiAnchor.TopRight: return new Vector2(1f, 1f);
                case UiAnchor.BottomLeft: return new Vector2(0f, 0f);
                case UiAnchor.BottomCenter: return new Vector2(0.5f, 0f);
                case UiAnchor.BottomRight: return new Vector2(1f, 0f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        // Input

        public static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject(nameof(EventSystem), typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                eventSystemObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
                return;
            }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystem.GetComponent<InputSystemUIInputModule>() != null) return;

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();

            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                Object.Destroy(legacyModule);
            }

            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#endif
        }

        public static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // Font

        private static Font GetDefaultFont()
        {
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont(FontNames, FontCreationSize);
            return defaultFont;
        }
    }
}
