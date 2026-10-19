using bison.core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace bison.core.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CurrencyText : MonoBehaviour
    {
        private const string DefaultNumberFormat = "{0:N0}";

        private enum CurrencyKind { Coins, Gems }

        [SerializeField, FormerlySerializedAs("_kind")] private CurrencyKind currencyKind = CurrencyKind.Coins;
        [SerializeField, FormerlySerializedAs("_format")] private string numberFormat = DefaultNumberFormat;

        private TMP_Text valueText;

        private void Awake() => valueText = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            saveManager.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Changed -= Refresh;
        }

        private void Refresh()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null || saveManager.Data == null) return;

            int amount = currencyKind == CurrencyKind.Coins ? saveManager.Data.Coins : saveManager.Data.Gems;
            valueText.text = string.Format(numberFormat, amount);
        }
    }
}
