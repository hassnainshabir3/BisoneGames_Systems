using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace bison.core.UI
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("panel")] private GameObject settingsPanel;
        [SerializeField, FormerlySerializedAs("settingButton")] private Button openButton;

        private void Start()
        {
            if (openButton != null && settingsPanel != null) openButton.onClick.AddListener(OpenPanel);
        }

        private void Update()
        {
            if (settingsPanel != null && settingsPanel.activeSelf && UiBuilder.IsEscapePressed()) ClosePanel();
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(OpenPanel);
        }

        private void OpenPanel() => settingsPanel.SetActive(true);

        private void ClosePanel() => settingsPanel.SetActive(false);
    }
}
