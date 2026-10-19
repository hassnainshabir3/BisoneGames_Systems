using UnityEngine;
using UnityEngine.UI;

namespace bison.core.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class OpenStoreOnClick : MonoBehaviour
    {
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable() => button.onClick.AddListener(StorePanel.Open);

        private void OnDisable() => button.onClick.RemoveListener(StorePanel.Open);
    }
}
