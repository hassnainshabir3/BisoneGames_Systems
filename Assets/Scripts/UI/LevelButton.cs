using bison.core.Save;
using bison.core.Store;
using bison.core.Transition;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace bison.core.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class LevelButton : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_levelId")] private string levelId;
        [SerializeField, FormerlySerializedAs("_label")] private TMP_Text labelText;
        [SerializeField, FormerlySerializedAs("_lockIcon")] private GameObject lockIcon;
        [SerializeField, FormerlySerializedAs("_costText")] private TMP_Text costText;
        [SerializeField, FormerlySerializedAs("_style")] private TransitionStyle transitionStyle = TransitionStyle.Splash;

        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            button.onClick.AddListener(OnClicked);

            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            saveManager.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClicked);

            if (SaveManager.Instance != null) SaveManager.Instance.Changed -= Refresh;
        }

        private void Refresh()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null || saveManager.Catalog == null) return;
            if (!saveManager.Catalog.TryGetLevel(levelId, out LevelDefinition level)) return;

            bool is_Unlocked = saveManager.IsLevelUnlocked(levelId);

            labelText.text = level.DisplayName;
            lockIcon.SetActive(!is_Unlocked);
            costText.text = is_Unlocked ? string.Empty : level.CoinCost.ToString("N0");
        }

        private void OnClicked()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null || saveManager.Catalog == null) return;
            if (!saveManager.Catalog.TryGetLevel(levelId, out LevelDefinition level)) return;

            if (!saveManager.IsLevelUnlocked(levelId))
            {
                saveManager.TryUnlockLevel(levelId);
                return;
            }

            SceneTransition.Load(level.SceneName, transitionStyle);
        }
    }
}
