using System.Collections.Generic;
using bison.core.Common;
using bison.core.Save;
using bison.core.Store;
using UnityEngine;
using UnityEngine.UI;

namespace bison.core.UI
{
    public sealed class StorePanel : MonoBehaviour
    {
        private const bool ShowDemoButtons = true;
        private const int CanvasSortingOrder = 600;

        private const float FrameWidth = 1500f;
        private const float FrameHeight = 860f;
        private const float FrameBorder = 3f;
        private const float RowWidth = 1420f;
        private const float RowHeight = 480f;
        private const float CardMaximumWidth = 260f;
        private const float CardSpacing = 24f;
        private const float CardPadding = 30f;
        private const float DemoButtonWidth = 360f;
        private const float DemoButtonHeight = 64f;
        private const float EdgeMargin = 40f;

        private sealed class StoreCard
        {
            public StoreCard(ShopItem item, Image borderImage, Image buttonImage,
                             Text priceText, Text buttonLabel, Button actionButton)
            {
                Item = item;
                BorderImage = borderImage;
                ButtonImage = buttonImage;
                PriceText = priceText;
                ButtonLabel = buttonLabel;
                ActionButton = actionButton;
            }

            public ShopItem Item { get; }
            public Image BorderImage { get; }
            public Image ButtonImage { get; }
            public Text PriceText { get; }
            public Text ButtonLabel { get; }
            public Button ActionButton { get; }
        }

        private static StorePanel instance;

        private readonly List<StoreCard> cards = new List<StoreCard>();

        private GameObject rootObject;
        private RectTransform cardRow;
        private Text coinsText;
        private Text gemsText;
        private ToastLabel toast;

        public static bool IsOpen =>
            instance != null && instance.rootObject != null && instance.rootObject.activeSelf;

        // Static entry points

        public static void Open()
        {
            if (instance == null) instance = new GameObject(nameof(StorePanel)).AddComponent<StorePanel>();
            instance.Show();
        }

        public static void Close()
        {
            if (instance != null) instance.Hide();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        // Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            UiBuilder.EnsureEventSystem();
            BuildInterface();
            rootObject.SetActive(false);

            if (SaveManager.Instance != null) SaveManager.Instance.Changed += Refresh;
        }

        private void OnDestroy()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Changed -= Refresh;
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (rootObject.activeSelf && UiBuilder.IsEscapePressed()) Hide();
        }

        private void Show()
        {
            rootObject.SetActive(true);
            toast.Clear();
            RebuildCards();
            Refresh();
        }

        private void Hide() => rootObject.SetActive(false);

        // Actions

        private void OnCardClicked(ShopItem item)
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            if (saveManager.Data.OwnsItem(item.Id))
            {
                saveManager.EquipItem(item.Id);
                return;
            }

            PurchaseResult result = item.IsGemOnly
                ? saveManager.BuyWithGems(item.Id)
                : saveManager.BuyWithCoins(item.Id);

            switch (result)
            {
                case PurchaseResult.Success:
                    toast.Show(item.IsFree ? UiText.ItemObtained : UiText.ItemPurchased);
                    break;
                case PurchaseResult.NotEnoughCoins:
                    toast.Show(UiText.NotEnoughCoins(item.CoinPrice));
                    break;
                case PurchaseResult.NotEnoughGems:
                    toast.Show(UiText.GemsRequired);
                    break;
            }
        }

        private void AddDemoCoins()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.AddCoins(500);
        }

        private void ResetAllData()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.ResetAllData();
        }

        // Refresh

        private void Refresh()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (coinsText == null || saveManager == null || saveManager.Data == null) return;

            coinsText.text = UiText.CoinsLabel(saveManager.Data.Coins);
            gemsText.text = UiText.GemsLabel(saveManager.Data.Gems);

            foreach (StoreCard card in cards) RefreshCard(card, saveManager);
        }

        private static void RefreshCard(StoreCard card, SaveManager saveManager)
        {
            ShopItem item = card.Item;
            bool is_Owned = saveManager.Data.OwnsItem(item.Id);
            bool is_Equipped = saveManager.Data.GetEquippedItemId(item.Slot) == item.Id;

            card.BorderImage.color = is_Equipped ? UiPalette.Orange : UiPalette.Line;

            if (is_Owned)
            {
                card.PriceText.text = is_Equipped ? UiText.StatusEquipped
                                    : item.IsOwnedByDefault ? UiText.PriceDefault
                                    : UiText.PriceOwned;
                card.PriceText.color = UiPalette.Orange;
            }
            else if (item.IsFree)
            {
                card.PriceText.text = UiText.PriceFree;
                card.PriceText.color = UiPalette.Green;
            }
            else if (item.CoinPrice > 0)
            {
                card.PriceText.text = UiText.PriceInCoins(item.CoinPrice);
                card.PriceText.color = UiPalette.Gold;
            }
            else
            {
                card.PriceText.text = UiText.PriceInGems(item.GemPrice);
                card.PriceText.color = UiPalette.Cyan;
            }

            card.ButtonLabel.text = is_Equipped ? UiText.StatusEquipped
                                  : is_Owned ? UiText.ActionEquip
                                  : item.IsFree ? UiText.ActionGet
                                  : UiText.ActionBuy;

            card.ActionButton.interactable = !is_Equipped;
            card.ButtonImage.color = is_Equipped ? UiPalette.Grey : UiPalette.Orange;
        }

        // Interface

        private void BuildInterface()
        {
            Canvas canvas = UiBuilder.CreateCanvas(transform, CanvasSortingOrder);

            Image dimmer = UiBuilder.CreateImage(canvas.transform, UiPalette.OverlayDim);
            UiBuilder.Stretch(dimmer.rectTransform, 0f);
            rootObject = dimmer.gameObject;

            Image border = UiBuilder.CreateImage(dimmer.transform, UiPalette.Orange);
            UiBuilder.Place(border.rectTransform, UiAnchor.Center, Vector2.zero, new Vector2(FrameWidth, FrameHeight));

            Image panel = UiBuilder.CreateImage(border.transform, UiPalette.Panel);
            UiBuilder.Stretch(panel.rectTransform, FrameBorder);

            BuildHeader(panel.transform);

            cardRow = UiBuilder.CreateRect(panel.transform);
            UiBuilder.Place(cardRow, UiAnchor.Center, new Vector2(0f, -10f), new Vector2(RowWidth, RowHeight));

            HorizontalLayoutGroup layout = cardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = CardSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            toast = ToastLabel.Create(panel.transform);

            if (ShowDemoButtons) BuildDemoButtons(panel.transform);
        }

        private void BuildHeader(Transform parent)
        {
            Text title = UiBuilder.CreateText(parent, UiText.StoreTitle, 80, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(title.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -24f), new Vector2(700f, 110f));

            Text subtitle = UiBuilder.CreateText(parent, UiText.StoreSubtitle, 30, TextAnchor.MiddleCenter, UiPalette.Dim);
            UiBuilder.Place(subtitle.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -140f), new Vector2(1200f, 50f));

            coinsText = UiBuilder.CreateText(parent, string.Empty, 40, TextAnchor.MiddleRight, UiPalette.Gold);
            UiBuilder.Place(coinsText.rectTransform, UiAnchor.TopRight, new Vector2(-EdgeMargin, -28f), new Vector2(460f, 52f));

            gemsText = UiBuilder.CreateText(parent, string.Empty, 40, TextAnchor.MiddleRight, UiPalette.Cyan);
            UiBuilder.Place(gemsText.rectTransform, UiAnchor.TopRight, new Vector2(-EdgeMargin, -82f), new Vector2(460f, 52f));

            Button backButton = UiBuilder.CreateButton(parent, UiText.Back, 38, UiPalette.Grey, UiAnchor.TopLeft,
                                                       new Vector2(EdgeMargin, -30f), new Vector2(220f, 74f));
            backButton.onClick.AddListener(Hide);
        }

        private void BuildDemoButtons(Transform parent)
        {
            Button resetButton = UiBuilder.CreateButton(parent, UiText.DemoResetAll, 30, UiPalette.Grey,
                                                        UiAnchor.BottomLeft, new Vector2(EdgeMargin, 24f),
                                                        new Vector2(DemoButtonWidth, DemoButtonHeight));
            resetButton.onClick.AddListener(ResetAllData);

            Button coinsButton = UiBuilder.CreateButton(parent, UiText.DemoAddCoins, 30, UiPalette.Grey,
                                                        UiAnchor.BottomRight, new Vector2(-EdgeMargin, 24f),
                                                        new Vector2(DemoButtonWidth, DemoButtonHeight));
            coinsButton.onClick.AddListener(AddDemoCoins);
        }

        private void RebuildCards()
        {
            for (int childIndex = cardRow.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = cardRow.GetChild(childIndex);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            cards.Clear();

            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null || saveManager.Catalog == null) return;

            List<ShopItem> characters = new List<ShopItem>();

            foreach (ShopItem item in saveManager.Catalog.Items)
                if (item != null && item.Slot == ItemSlot.Character) characters.Add(item);

            if (characters.Count == 0) return;

            float totalSpacing = CardSpacing * (characters.Count - 1);
            float cardWidth = Mathf.Min(CardMaximumWidth, (RowWidth - totalSpacing) / characters.Count);

            foreach (ShopItem item in characters) BuildCard(item, cardWidth);
        }

        private void BuildCard(ShopItem item, float cardWidth)
        {
            Image border = UiBuilder.CreateImage(cardRow, UiPalette.Line);
            border.rectTransform.sizeDelta = new Vector2(cardWidth, RowHeight);

            Image inner = UiBuilder.CreateImage(border.transform, UiPalette.Panel);
            UiBuilder.Stretch(inner.rectTransform, FrameBorder);

            float artSize = cardWidth - CardPadding * 2f;
            float nameOffset = -(20f + artSize + 16f);
            float priceOffset = nameOffset - 52f;

            Color artColor = item.IsTintEnabled ? item.Tint : UiPalette.NeutralArt;
            Image art = UiBuilder.CreateImage(inner.transform, artColor);
            UiBuilder.Place(art.rectTransform, UiAnchor.TopCenter, new Vector2(0f, -20f), new Vector2(artSize, artSize));

            Text initial = UiBuilder.CreateText(art.transform, GetInitial(item.DisplayName), 110,
                                                TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.35f));
            UiBuilder.Stretch(initial.rectTransform, 0f);

            Text nameText = UiBuilder.CreateText(inner.transform, item.DisplayName, 36, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(nameText.rectTransform, UiAnchor.TopCenter, new Vector2(0f, nameOffset), new Vector2(cardWidth - 20f, 50f));

            Text priceText = UiBuilder.CreateText(inner.transform, string.Empty, 28, TextAnchor.MiddleCenter, UiPalette.Gold);
            UiBuilder.Place(priceText.rectTransform, UiAnchor.TopCenter, new Vector2(0f, priceOffset), new Vector2(cardWidth - 20f, 44f));

            Button actionButton = UiBuilder.CreateButton(inner.transform, string.Empty, 32, UiPalette.Orange,
                                                         UiAnchor.BottomCenter, new Vector2(0f, 22f),
                                                         new Vector2(cardWidth - 40f, 64f));
            Image buttonImage = (Image)actionButton.targetGraphic;
            Text buttonLabel = actionButton.GetComponentInChildren<Text>();
            actionButton.onClick.AddListener(() => OnCardClicked(item));

            cards.Add(new StoreCard(item, border, buttonImage, priceText, buttonLabel, actionButton));
        }

        private static string GetInitial(string displayName)
        {
            return string.IsNullOrEmpty(displayName) ? string.Empty : displayName.Substring(0, 1);
        }
    }
}
