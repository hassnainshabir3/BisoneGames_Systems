using System;
using System.Collections.Generic;
using System.IO;
using bison.core.Common;
using bison.core.Store;
using UnityEngine;

namespace bison.core.Save
{
    public enum PurchaseResult
    {
        Success,
        UnknownItem,
        AlreadyOwned,
        NotEnoughCoins,
        NotEnoughGems,
        NotForSale
    }

    public sealed class SaveManager : MonoBehaviour
    {
        private const float AutosaveDelaySeconds = 1f;

        private string savePath;
        private bool is_Dirty;
        private bool is_ReloadPending;
        private float nextSaveTime;

        public static SaveManager Instance { get; private set; }

        public SaveData Data { get; private set; }
        public GameCatalog Catalog { get; private set; }
        public bool WasTamperDetected { get; private set; }

        public event Action Changed;

        // Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject(nameof(SaveManager)).AddComponent<SaveManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Catalog = Resources.Load<GameCatalog>(ResourcePaths.Catalog);
            if (Catalog == null) Debug.LogError(LogMessages.MissingResource(ResourcePaths.Catalog));

            savePath = Path.Combine(Application.persistentDataPath, SaveFiles.FileName);
            SecureInt.Tampered += OnTamperDetected;

            LoadData();
        }

        private void OnDestroy()
        {
            SecureInt.Tampered -= OnTamperDetected;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (is_ReloadPending)
            {
                is_ReloadPending = false;
                LoadData();
                Changed?.Invoke();
                return;
            }

            if (is_Dirty && Time.unscaledTime >= nextSaveTime) SaveNow();
        }

        private void OnApplicationPause(bool is_Paused)
        {
            if (is_Paused && is_Dirty) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (is_Dirty) SaveNow();
        }

        // Load and save

        private void LoadData()
        {
            SaveData loadedData = null;

            SecureStorage.ReadResult readResult = SecureStorage.TryRead(savePath, out string json);
            if (readResult == SecureStorage.ReadResult.Invalid) WasTamperDetected = true;

            if (readResult != SecureStorage.ReadResult.Ok)
                readResult = SecureStorage.TryRead(savePath + SaveFiles.BackupSuffix, out json);

            if (readResult == SecureStorage.ReadResult.Ok)
                loadedData = JsonUtility.FromJson<SaveData>(json);

            if (loadedData != null)
            {
                Data = loadedData;
                return;
            }

            Data = CreateNewData();
            SaveNow();
        }

        private SaveData CreateNewData()
        {
            SaveData newData = new SaveData();
            if (Catalog == null) return newData;

            foreach (ShopItem item in Catalog.Items)
            {
                if (item == null || !item.IsOwnedByDefault) continue;

                bool has_GrantedItem = newData.GrantItem(item.Id);
                bool is_SlotEmpty = string.IsNullOrEmpty(newData.GetEquippedItemId(item.Slot));

                if (has_GrantedItem && is_SlotEmpty) newData.EquipItem(item.Slot, item.Id);
            }

            return newData;
        }

        public void SaveNow()
        {
            if (Data == null) return;

            Data.Stamp();
            string json = JsonUtility.ToJson(Data);

            if (is_ReloadPending) return;

            try
            {
                SecureStorage.Write(savePath, json);
                is_Dirty = false;
            }
            catch (Exception exception)
            {
                Debug.LogError(LogMessages.SaveWriteFailed(exception.Message));
            }
        }

        public void ResetAllData()
        {
            DeleteFileIfExists(savePath);
            DeleteFileIfExists(savePath + SaveFiles.BackupSuffix);
            DeleteFileIfExists(savePath + SaveFiles.TemporarySuffix);

            Data = CreateNewData();
            SaveNow();
            Changed?.Invoke();
        }

        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private void MarkDirty()
        {
            if (!is_Dirty)
            {
                is_Dirty = true;
                nextSaveTime = Time.unscaledTime + AutosaveDelaySeconds;
            }

            Changed?.Invoke();
        }

        private void OnTamperDetected()
        {
            WasTamperDetected = true;
            is_ReloadPending = true;
            is_Dirty = false;
            Debug.LogWarning(LogMessages.SaveTamperDetected);
        }

        // Currency

        public void AddCoins(int amount)
        {
            if (Data == null || amount <= 0) return;

            Data.AddCoins(amount);
            MarkDirty();
        }

        public void AddGems(int amount)
        {
            if (Data == null || amount <= 0) return;

            Data.AddGems(amount);
            MarkDirty();
            SaveNow();
        }

        // Store

        public PurchaseResult BuyWithCoins(string itemId)
        {
            if (Catalog == null || !Catalog.TryGetItem(itemId, out ShopItem item))
                return PurchaseResult.UnknownItem;

            if (Data.OwnsItem(itemId)) return PurchaseResult.AlreadyOwned;
            if (item.CoinPrice <= 0 && item.GemPrice > 0) return PurchaseResult.NotForSale;

            if (item.CoinPrice > 0 && !Data.TrySpendCoins(item.CoinPrice))
                return PurchaseResult.NotEnoughCoins;

            CompletePurchase(item);
            return PurchaseResult.Success;
        }

        public PurchaseResult BuyWithGems(string itemId)
        {
            if (Catalog == null || !Catalog.TryGetItem(itemId, out ShopItem item))
                return PurchaseResult.UnknownItem;

            if (Data.OwnsItem(itemId)) return PurchaseResult.AlreadyOwned;
            if (item.GemPrice <= 0) return PurchaseResult.NotForSale;

            if (!Data.TrySpendGems(item.GemPrice)) return PurchaseResult.NotEnoughGems;

            CompletePurchase(item);
            return PurchaseResult.Success;
        }

        private void CompletePurchase(ShopItem item)
        {
            Data.GrantItem(item.Id);
            Data.EquipItem(item.Slot, item.Id);

            MarkDirty();
            SaveNow();
        }

        public bool EquipItem(string itemId)
        {
            if (Catalog == null || !Catalog.TryGetItem(itemId, out ShopItem item)) return false;
            if (!Data.OwnsItem(itemId)) return false;

            Data.EquipItem(item.Slot, itemId);
            MarkDirty();
            return true;
        }

        public bool TryGetEquippedItem(ItemSlot slot, out ShopItem item)
        {
            item = null;
            if (Catalog == null || Data == null) return false;

            string itemId = Data.GetEquippedItemId(slot);
            return !string.IsNullOrEmpty(itemId) && Catalog.TryGetItem(itemId, out item);
        }

        // Levels

        public bool IsLevelUnlocked(string levelId)
        {
            if (Catalog == null || !Catalog.TryGetLevel(levelId, out LevelDefinition level)) return false;

            return level.CoinCost <= 0 || Data.IsLevelUnlocked(levelId);
        }

        public bool TryUnlockLevel(string levelId)
        {
            if (Catalog == null || !Catalog.TryGetLevel(levelId, out LevelDefinition level)) return false;
            if (IsLevelUnlocked(levelId)) return false;
            if (!Data.TrySpendCoins(level.CoinCost)) return false;

            Data.UnlockLevel(levelId);
            MarkDirty();
            SaveNow();
            return true;
        }

        public void CompleteLevel(string levelId)
        {
            if (Catalog == null || Data == null) return;

            IReadOnlyList<LevelDefinition> levels = Catalog.Levels;

            for (int levelIndex = 0; levelIndex < levels.Count - 1; levelIndex++)
            {
                if (levels[levelIndex].Id != levelId) continue;

                if (Data.UnlockLevel(levels[levelIndex + 1].Id))
                {
                    MarkDirty();
                    SaveNow();
                }

                return;
            }
        }

        public void ResetLevelProgress()
        {
            if (Data == null) return;

            Data.ClearUnlockedLevels();
            MarkDirty();
            SaveNow();
        }

        public bool HasLevelProgress()
        {
            if (Catalog == null || Data == null) return false;

            foreach (LevelDefinition level in Catalog.Levels)
                if (level.CoinCost > 0 && Data.IsLevelUnlocked(level.Id)) return true;

            return false;
        }

        public int GetHighestUnlockedLevelNumber()
        {
            int highestNumber = 1;
            if (Catalog == null) return highestNumber;

            IReadOnlyList<LevelDefinition> levels = Catalog.Levels;

            for (int levelIndex = 0; levelIndex < levels.Count; levelIndex++)
                if (IsLevelUnlocked(levels[levelIndex].Id)) highestNumber = levelIndex + 1;

            return highestNumber;
        }
    }
}
