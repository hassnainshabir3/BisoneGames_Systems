using System;
using System.Collections.Generic;
using bison.core.Store;
using UnityEngine;

namespace bison.core.Save
{
    [Serializable]
    public sealed class SaveData : ISerializationCallbackReceiver
    {
        [Serializable]
        private sealed class EquippedEntry
        {
            [SerializeField] private ItemSlot slot;
            [SerializeField] private string itemId;

            public EquippedEntry(ItemSlot slot, string itemId)
            {
                this.slot = slot;
                this.itemId = itemId;
            }

            public ItemSlot Slot => slot;
            public string ItemId
            {
                get => itemId;
                set => itemId = value;
            }
        }

        [SerializeField] private int version = 1;
        [SerializeField] private long revision;
        [SerializeField] private long savedAtTicks;
        [SerializeField] private int coins;
        [SerializeField] private int gems;
        [SerializeField] private List<string> ownedItemIds = new List<string>();
        [SerializeField] private List<EquippedEntry> equippedEntries = new List<EquippedEntry>();
        [SerializeField] private List<string> unlockedLevelIds = new List<string>();

        private SecureInt liveCoins;
        private SecureInt liveGems;

        public int Coins => liveCoins.Value;
        public int Gems => liveGems.Value;
        public long Revision => revision;
        public long SavedAtTicks => savedAtTicks;
        public IReadOnlyList<string> OwnedItemIds => ownedItemIds;

        // Currency

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            long newTotal = (long)Coins + amount;
            liveCoins.Value = (int)Math.Min(newTotal, int.MaxValue);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount) return false;

            liveCoins.Value = Coins - amount;
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;

            long newTotal = (long)Gems + amount;
            liveGems.Value = (int)Math.Min(newTotal, int.MaxValue);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount < 0 || Gems < amount) return false;

            liveGems.Value = Gems - amount;
            return true;
        }

        // Items

        public bool OwnsItem(string itemId) => ownedItemIds.Contains(itemId);

        public bool GrantItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || ownedItemIds.Contains(itemId)) return false;

            ownedItemIds.Add(itemId);
            return true;
        }

        public string GetEquippedItemId(ItemSlot slot)
        {
            foreach (EquippedEntry entry in equippedEntries)
                if (entry.Slot == slot) return entry.ItemId;

            return null;
        }

        public void EquipItem(ItemSlot slot, string itemId)
        {
            foreach (EquippedEntry entry in equippedEntries)
            {
                if (entry.Slot != slot) continue;

                entry.ItemId = itemId;
                return;
            }

            equippedEntries.Add(new EquippedEntry(slot, itemId));
        }

        // Levels

        public bool IsLevelUnlocked(string levelId) => unlockedLevelIds.Contains(levelId);

        public bool UnlockLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || unlockedLevelIds.Contains(levelId)) return false;

            unlockedLevelIds.Add(levelId);
            return true;
        }

        public void ClearUnlockedLevels() => unlockedLevelIds.Clear();

        // Versioning

        internal void Stamp()
        {
            revision++;
            savedAtTicks = DateTime.UtcNow.Ticks;
        }

        // Serialization

        public void OnBeforeSerialize()
        {
            coins = liveCoins.Value;
            gems = liveGems.Value;
        }

        public void OnAfterDeserialize()
        {
            liveCoins = new SecureInt(coins);
            liveGems = new SecureInt(gems);

            if (ownedItemIds == null) ownedItemIds = new List<string>();
            if (equippedEntries == null) equippedEntries = new List<EquippedEntry>();
            if (unlockedLevelIds == null) unlockedLevelIds = new List<string>();
        }
    }
}
