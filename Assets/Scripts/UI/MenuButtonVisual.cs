using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace bison.core.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuButtonVisual : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [Serializable]
        private struct Palette
        {
            [SerializeField, FormerlySerializedAs("Border")] private Color border;
            [SerializeField, FormerlySerializedAs("Fill")] private Color fill;
            [SerializeField, FormerlySerializedAs("Diamond")] private Color diamond;
            [SerializeField, FormerlySerializedAs("Label")] private Color label;
            [SerializeField, FormerlySerializedAs("TagFill")] private Color tagFill;
            [SerializeField, FormerlySerializedAs("TagText")] private Color tagText;

            public Color Border => border;
            public Color Fill => fill;
            public Color Diamond => diamond;
            public Color Label => label;
            public Color TagFill => tagFill;
            public Color TagText => tagText;
        }

        [Header("Parts")]
        [SerializeField, FormerlySerializedAs("_border")] private Image borderImage;
        [SerializeField, FormerlySerializedAs("_fill")] private Image fillImage;
        [SerializeField, FormerlySerializedAs("_diamond")] private Image diamondImage;
        [SerializeField, FormerlySerializedAs("_label")] private TMP_Text labelText;
        [SerializeField, FormerlySerializedAs("_tagFill")] private Image tagFillImage;
        [SerializeField, FormerlySerializedAs("_tagText")] private TMP_Text tagText;

        [Header("Colors")]
        [SerializeField, FormerlySerializedAs("_normal")] private Palette normalPalette;
        [SerializeField, FormerlySerializedAs("_selected")] private Palette selectedPalette;

        [Header("Feel")]
        [SerializeField, FormerlySerializedAs("_speed")] private float blendSpeed = 8f;

        private float blendAmount;
        private bool is_Selected;

        // Lifecycle

        private void OnEnable()
        {
            is_Selected = EventSystem.current != null &&
                          EventSystem.current.currentSelectedGameObject == gameObject;
            blendAmount = is_Selected ? 1f : 0f;
            ApplyColors(blendAmount);
        }

        private void Update()
        {
            float targetAmount = is_Selected ? 1f : 0f;
            if (Mathf.Approximately(blendAmount, targetAmount)) return;

            blendAmount = Mathf.MoveTowards(blendAmount, targetAmount, blendSpeed * Time.unscaledDeltaTime);
            ApplyColors(blendAmount);
        }

        // Events

        public void OnSelect(BaseEventData eventData) => is_Selected = true;

        public void OnDeselect(BaseEventData eventData) => is_Selected = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        // Colors

        private void ApplyColors(float amount)
        {
            borderImage.color = Color.Lerp(normalPalette.Border, selectedPalette.Border, amount);
            fillImage.color = Color.Lerp(normalPalette.Fill, selectedPalette.Fill, amount);
            diamondImage.color = Color.Lerp(normalPalette.Diamond, selectedPalette.Diamond, amount);
            labelText.color = Color.Lerp(normalPalette.Label, selectedPalette.Label, amount);
            tagFillImage.color = Color.Lerp(normalPalette.TagFill, selectedPalette.TagFill, amount);
            tagText.color = Color.Lerp(normalPalette.TagText, selectedPalette.TagText, amount);
        }
    }
}
