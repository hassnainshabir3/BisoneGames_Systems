using UnityEngine;
using UnityEngine.UI;

namespace bison.core.UI
{
    public sealed class ToastLabel : MonoBehaviour
    {
        private const float DisplaySeconds = 2.2f;
        private const float BottomOffset = 90f;
        private const int FontSize = 32;

        private Image backgroundImage;
        private Text messageText;
        private float hideTime;

        public static ToastLabel Create(Transform parent)
        {
            Image background = UiBuilder.CreateImage(parent, UiPalette.ToastBackground);
            UiBuilder.Place(background.rectTransform, UiAnchor.BottomCenter,
                            new Vector2(0f, BottomOffset), new Vector2(900f, 80f));
            background.raycastTarget = false;

            ToastLabel toast = background.gameObject.AddComponent<ToastLabel>();
            toast.backgroundImage = background;
            toast.messageText = UiBuilder.CreateText(background.transform, string.Empty, FontSize,
                                                     TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Stretch(toast.messageText.rectTransform, 0f);

            toast.Clear();
            return toast;
        }

        public void Show(string message)
        {
            messageText.text = message;
            backgroundImage.enabled = true;
            hideTime = Time.unscaledTime + DisplaySeconds;
        }

        public void Clear()
        {
            messageText.text = string.Empty;
            backgroundImage.enabled = false;
        }

        private void Update()
        {
            if (backgroundImage.enabled && Time.unscaledTime >= hideTime) Clear();
        }
    }
}
