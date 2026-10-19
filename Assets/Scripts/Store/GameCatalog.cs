using System;
using System.Collections.Generic;
using bison.core.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace bison.core.Store
{
    public enum ItemSlot { Character, Hat, Trail }

    [Serializable]
    public sealed class ShopItem
    {
        [SerializeField, FormerlySerializedAs("_id")] private string id;
        [SerializeField, FormerlySerializedAs("_displayName")] private string displayName;
        [SerializeField] private ItemSlot itemSlot = ItemSlot.Character;
        [SerializeField, FormerlySerializedAs("_icon")] private Sprite icon;
        [SerializeField, Min(0), FormerlySerializedAs("_coinPrice")] private int coinPrice;
        [SerializeField, Min(0), FormerlySerializedAs("_gemPrice")] private int gemPrice;
        [SerializeField, FormerlySerializedAs("_ownedByDefault")] private bool is_OwnedByDefault;

        [Header("Character")]
        [SerializeField, FormerlySerializedAs("_useTint")] private bool is_TintEnabled;
        [SerializeField, FormerlySerializedAs("_tint")] private Color tint = Color.white;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemSlot Slot => itemSlot;
        public Sprite Icon => icon;
        public int CoinPrice => coinPrice;
        public int GemPrice => gemPrice;
        public bool IsOwnedByDefault => is_OwnedByDefault;
        public bool IsTintEnabled => is_TintEnabled;
        public Color Tint => tint;
        public bool IsFree => coinPrice <= 0 && gemPrice <= 0;
        public bool IsGemOnly => coinPrice <= 0 && gemPrice > 0;
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField, FormerlySerializedAs("_id")] private string id;
        [SerializeField, FormerlySerializedAs("_displayName")] private string displayName;
        [SerializeField, FormerlySerializedAs("_sceneName")] private string sceneName;
        [SerializeField, Min(0), FormerlySerializedAs("_coinCost")] private int coinCost;

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public int CoinCost => coinCost;
    }

    [CreateAssetMenu(fileName = "GameCatalog", menuName = "Bison/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("_items")] private List<ShopItem> items = new List<ShopItem>();
        [SerializeField, FormerlySerializedAs("_levels")] private List<LevelDefinition> levels = new List<LevelDefinition>();

        private Dictionary<string, ShopItem> itemLookup;
        private Dictionary<string, LevelDefinition> levelLookup;

        public IReadOnlyList<ShopItem> Items => items;
        public IReadOnlyList<LevelDefinition> Levels => levels;

        private void OnEnable()
        {
            itemLookup = null;
            levelLookup = null;
        }

        public bool TryGetItem(string itemId, out ShopItem item)
        {
            if (itemLookup == null) BuildLookups();
            return itemLookup.TryGetValue(itemId ?? string.Empty, out item);
        }

        public bool TryGetLevel(string levelId, out LevelDefinition level)
        {
            if (levelLookup == null) BuildLookups();
            return levelLookup.TryGetValue(levelId ?? string.Empty, out level);
        }

        public bool TryFindLevelByScene(string sceneName, out int levelIndex)
        {
            for (levelIndex = 0; levelIndex < levels.Count; levelIndex++)
                if (levels[levelIndex] != null && levels[levelIndex].SceneName == sceneName) return true;

            levelIndex = -1;
            return false;
        }

        private void BuildLookups()
        {
            itemLookup = new Dictionary<string, ShopItem>();
            levelLookup = new Dictionary<string, LevelDefinition>();

            foreach (ShopItem item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;

                if (!itemLookup.ContainsKey(item.Id)) itemLookup.Add(item.Id, item);
                else Debug.LogWarning(LogMessages.DuplicateId(item.Id));
            }

            foreach (LevelDefinition level in levels)
            {
                if (level == null || string.IsNullOrEmpty(level.Id)) continue;

                if (!levelLookup.ContainsKey(level.Id)) levelLookup.Add(level.Id, level);
                else Debug.LogWarning(LogMessages.DuplicateId(level.Id));
            }
        }
    }
}
