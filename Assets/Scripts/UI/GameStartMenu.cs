using System.Collections.Generic;
using bison.core.Common;
using bison.core.Save;
using bison.core.Store;
using bison.core.Transition;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace bison.core.UI
{
    public sealed class GameStartMenu : MonoBehaviour
    {
        private const bool ShowDemoButtons = true;
        private const int CanvasSortingOrder = 500;

        private const int LevelColumnCount = 3;
        private const float LevelCardWidth = 380f;
        private const float LevelCardHeight = 250f;
        private const float LevelCardSpacing = 30f;
        private const float CardBorder = 3f;
        private const float EdgeMargin = 40f;

        [Header("Menu Buttons")]
        [SerializeField, FormerlySerializedAs("_continueButton")] private Button continueButton;
        [SerializeField, FormerlySerializedAs("_newGameButton")] private Button newGameButton;
        [SerializeField, FormerlySerializedAs("_continueLabel")] private TMP_Text continueLabel;

        [Header("Transition")]
        [SerializeField, FormerlySerializedAs("_transition")] private TransitionStyle transitionStyle = TransitionStyle.Splash;

        private GameObject levelsRoot;
        private GameObject confirmRoot;
        private RectTransform levelGrid;
        private Text coinsText;
        private Text ownContinueLabel;
        private ToastLabel toast;

        // Lifecycle

        private void Awake()
        {
            UiBuilder.EnsureEventSystem();

            Canvas canvas = UiBuilder.CreateCanvas(transform, CanvasSortingOrder);

            if (continueButton == null || newGameButton == null) BuildOwnButtons(canvas.transform);

            BuildLevelsPopup(canvas.transform);
            BuildConfirmDialog(canvas.transform);
            toast = ToastLabel.Create(canvas.transform);

            levelsRoot.SetActive(false);
            confirmRoot.SetActive(false);
        }

        private void OnEnable()
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            newGameButton.onClick.AddListener(OnNewGameClicked);

            if (SaveManager.Instance != null) SaveManager.Instance.Changed += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);

            if (SaveManager.Instance != null) SaveManager.Instance.Changed -= Refresh;
        }

        private void Update()
        {
            if (!UiBuilder.IsEscapePressed()) return;

            if (confirmRoot.activeSelf) confirmRoot.SetActive(false);
            else if (levelsRoot.activeSelf) levelsRoot.SetActive(false);
        }

        // Continue and new game

        private void OnContinueClicked()
        {
            if (!HasProgress())
            {
                toast.Show(UiText.NoProgress);
                return;
            }

            OpenLevels();
        }

        private void OnNewGameClicked()
        {
            if (SaveManager.Instance == null)
            {
                toast.Show(UiText.SaveManagerMissing);
                return;
            }

            if (HasProgress()) confirmRoot.SetActive(true);
            else OpenLevels();
        }

        private void ConfirmNewGame()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.ResetLevelProgress();

            confirmRoot.SetActive(false);
            toast.Show(UiText.NewGameStarted);
            OpenLevels();
        }

        private void OnLevelSelected(LevelDefinition level)
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            if (saveManager.IsLevelUnlocked(level.Id))
            {
                if (!Application.CanStreamedLevelBeLoaded(level.SceneName))
                {
                    toast.Show(UiText.SceneNotInBuild(level.SceneName));
                    return;
                }

                SceneTransition.Load(level.SceneName, transitionStyle);
                return;
            }

            if (saveManager.TryUnlockLevel(level.Id)) toast.Show(UiText.LevelUnlocked(level.DisplayName));
            else toast.Show(UiText.NotEnoughCoins(level.CoinCost));
        }

        // Demo actions

        private void AddDemoCoins()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.AddCoins(500);
        }

        private void CompleteHighestLevel()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null || saveManager.Catalog == null) return;

            int highestNumber = saveManager.GetHighestUnlockedLevelNumber();
            saveManager.CompleteLevel(saveManager.Catalog.Levels[highestNumber - 1].Id);
            toast.Show(UiText.LevelCompletedDemo(highestNumber));
        }

        // Display

        private static bool HasProgress()
        {
            SaveManager saveManager = SaveManager.Instance;
            return saveManager != null && saveManager.Catalog != null && saveManager.HasLevelProgress();
        }

        private void OpenLevels()
        {
            levelsRoot.SetActive(true);
            RebuildLevelCards();
            RefreshWallet();
        }

        private void Refresh()
        {
            SaveManager saveManager = SaveManager.Instance;
            bool has_Progress = HasProgress();

            continueButton.interactable = has_Progress;

            string labelContent = has_Progress && saveManager != null
                ? UiText.ContinueAtLevel(saveManager.GetHighestUnlockedLevelNumber())
                : UiText.Continue;

            if (continueLabel != null) continueLabel.text = labelContent;

            if (ownContinueLabel != null)
            {
                ownContinueLabel.text = labelContent;
                ownContinueLabel.color = has_Progress ? Color.white : UiPalette.Dim;
            }

            if (levelsRoot != null && levelsRoot.activeSelf)
            {
                RebuildLevelCards();
                RefreshWallet();
            }
        }

        private void RefreshWallet()
        {
            SaveManager saveManager = SaveManager.Instance;
            int coins = saveManager != null && saveManager.Data != null ? saveManager.Data.Coins : 0;
            coinsText.text = UiText.CoinsLabel(coins);
        }

        private void RebuildLevelCards()
        {
            for (int childIndex = levelGrid.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = levelGrid.GetChild(childIndex);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            SaveManager saveManager = SaveManager.Instance;

            if (saveManager == null || saveManager.Catalog == null)
            {
                toast.Show(UiText.CatalogMissing);
                return;
            }

            IReadOnlyList<LevelDefinition> levels = saveManager.Catalog.Levels;

            if (levels.Count == 0)
            {
                toast.Show(UiText.CatalogHasNoLevels);
                return;
            }

            int highestNumber = saveManager.GetHighestUnlockedLevelNumber();
            bool has_Progress = saveManager.HasLevelProgress();

            for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
            {
                bool is_Unlocked = saveManager.IsLevelUnlocked(levels[levelIndex].Id);
                bool is_Latest = is_Unlocked && has_Progress && levelIndex + 1 == highestNumber;

                BuildLevelCard(levels[levelIndex], levelIndex, is_Unlocked, is_Latest);
            }
        }

        // Interface

        private void BuildOwnButtons(Transform parent)
        {
            Image background = UiBuilder.CreateImage(parent, UiPalette.MenuBackground);
            UiBuilder.Stretch(background.rectTransform, 0f);

            Text title = UiBuilder.CreateText(background.transform, UiText.GameTitle, 110,
                                              TextAnchor.MiddleLeft, Color.white);
            UiBuilder.Place(title.rectTransform, UiAnchor.TopLeft, new Vector2(140f, -90f), new Vector2(1000f, 140f));

            Vector2 buttonSize = new Vector2(640f, 96f);

            continueButton = UiBuilder.CreateButton(background.transform, UiText.Continue, 40, UiPalette.Panel,
                                                    UiAnchor.TopLeft, new Vector2(140f, -330f), buttonSize);
            ownContinueLabel = continueButton.GetComponentInChildren<Text>();

            newGameButton = UiBuilder.CreateButton(background.transform, UiText.NewGame, 40, UiPalette.Panel,
                                                   UiAnchor.TopLeft, new Vector2(140f, -448f), buttonSize);
        }

        private void BuildLevelsPopup(Transform parent)
        {
            Image dimmer = UiBuilder.CreateImage(parent, UiPalette.OverlayDim);
            UiBuilder.Stretch(dimmer.rectTransform, 0f);
            levelsRoot = dimmer.gameObject;

            Image border = UiBuilder.CreateImage(dimmer.transform, UiPalette.Orange);
            UiBuilder.Place(border.rectTransform, UiAnchor.Center, Vector2.zero, new Vector2(1400f, 880f));

            Image panel = UiBuilder.CreateImage(border.transform, UiPalette.Panel);
            UiBuilder.Stretch(panel.rectTransform, CardBorder);

            Text title = UiBuilder.CreateText(panel.transform, UiText.LevelsTitle, 76, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(title.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -22f), new Vector2(800f, 100f));

            Text hint = UiBuilder.CreateText(panel.transform, UiText.LevelsHint, 28, TextAnchor.MiddleCenter, UiPalette.Dim);
            UiBuilder.Place(hint.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -126f), new Vector2(1200f, 46f));

            coinsText = UiBuilder.CreateText(panel.transform, string.Empty, 38, TextAnchor.MiddleRight, UiPalette.Gold);
            UiBuilder.Place(coinsText.rectTransform, UiAnchor.TopRight, new Vector2(-EdgeMargin, -34f), new Vector2(420f, 50f));

            Button backButton = UiBuilder.CreateButton(panel.transform, UiText.Back, 36, UiPalette.Grey, UiAnchor.TopLeft,
                                                       new Vector2(EdgeMargin, -30f), new Vector2(220f, 70f));
            backButton.onClick.AddListener(() => levelsRoot.SetActive(false));

            levelGrid = UiBuilder.CreateRect(panel.transform);
            UiBuilder.Place(levelGrid, UiAnchor.Center, new Vector2(0f, -10f), new Vector2(1240f, 540f));

            if (ShowDemoButtons) BuildDemoButtons(panel.transform);
        }

        private void BuildDemoButtons(Transform parent)
        {
            Button coinsButton = UiBuilder.CreateButton(parent, UiText.DemoAddCoins, 28, UiPalette.Grey, UiAnchor.BottomLeft,
                                                        new Vector2(EdgeMargin, 24f), new Vector2(380f, 62f));
            coinsButton.onClick.AddListener(AddDemoCoins);

            Button winButton = UiBuilder.CreateButton(parent, UiText.DemoCompleteHighestLevel, 28, UiPalette.Grey,
                                                      UiAnchor.BottomRight, new Vector2(-EdgeMargin, 24f),
                                                      new Vector2(480f, 62f));
            winButton.onClick.AddListener(CompleteHighestLevel);
        }

        private void BuildLevelCard(LevelDefinition level, int levelIndex, bool is_Unlocked, bool is_Latest)
        {
            int column = levelIndex % LevelColumnCount;
            int row = levelIndex / LevelColumnCount;
            Vector2 cardOffset = new Vector2(column * (LevelCardWidth + LevelCardSpacing),
                                             -row * (LevelCardHeight + LevelCardSpacing));

            Image border = UiBuilder.CreateImage(levelGrid, is_Unlocked ? UiPalette.Orange : UiPalette.Line);
            UiBuilder.Place(border.rectTransform, UiAnchor.TopLeft, cardOffset,
                            new Vector2(LevelCardWidth, LevelCardHeight));

            Image inner = UiBuilder.CreateImage(border.transform, is_Unlocked ? UiPalette.OrangeDark : UiPalette.LockedFill);
            UiBuilder.Stretch(inner.rectTransform, CardBorder);

            Text numberText = UiBuilder.CreateText(inner.transform, (levelIndex + 1).ToString(), 110,
                                                   TextAnchor.MiddleCenter, is_Unlocked ? Color.white : UiPalette.Dim);
            UiBuilder.Place(numberText.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -8f), new Vector2(340f, 150f));

            string statusContent = !is_Unlocked ? UiText.LevelLocked(level.CoinCost)
                                 : is_Latest ? UiText.LevelPlayLatest
                                 : UiText.LevelPlay;

            Text statusText = UiBuilder.CreateText(inner.transform, statusContent, 28, TextAnchor.MiddleCenter,
                                                   is_Unlocked ? UiPalette.Orange : UiPalette.Dim);
            UiBuilder.Place(statusText.rectTransform, UiAnchor.BottomCenter, new Vector2(0f, 24f), new Vector2(350f, 50f));

            Button button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnLevelSelected(level));
        }

        private void BuildConfirmDialog(Transform parent)
        {
            Image dimmer = UiBuilder.CreateImage(parent, UiPalette.DialogDim);
            UiBuilder.Stretch(dimmer.rectTransform, 0f);
            confirmRoot = dimmer.gameObject;

            Image border = UiBuilder.CreateImage(dimmer.transform, UiPalette.Orange);
            UiBuilder.Place(border.rectTransform, UiAnchor.Center, Vector2.zero, new Vector2(1040f, 440f));

            Image panel = UiBuilder.CreateImage(border.transform, UiPalette.Panel);
            UiBuilder.Stretch(panel.rectTransform, CardBorder);

            Text title = UiBuilder.CreateText(panel.transform, UiText.ConfirmTitle, 70, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(title.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -30f), new Vector2(900f, 90f));

            Text message = UiBuilder.CreateText(panel.transform, UiText.ConfirmMessage, 32, TextAnchor.MiddleCenter,
                                                UiPalette.MessageText);
            UiBuilder.Place(message.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -140f), new Vector2(940f, 140f));

            Vector2 dialogButtonSize = new Vector2(400f, 90f);

            Button yesButton = UiBuilder.CreateButton(panel.transform, UiText.ConfirmYes, 38, UiPalette.Orange,
                                                      UiAnchor.BottomLeft, new Vector2(70f, 40f), dialogButtonSize);
            yesButton.onClick.AddListener(ConfirmNewGame);

            Button cancelButton = UiBuilder.CreateButton(panel.transform, UiText.ConfirmCancel, 38, UiPalette.Grey,
                                                         UiAnchor.BottomRight, new Vector2(-70f, 40f), dialogButtonSize);
            cancelButton.onClick.AddListener(() => confirmRoot.SetActive(false));
        }
    }
}
