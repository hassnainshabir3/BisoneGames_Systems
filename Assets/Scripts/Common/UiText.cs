namespace bison.core.Common
{
    public static class UiText
    {
        public const string GameTitle = "REPAIR RUN";
        public const string Continue = "CONTINUE";
        public const string NewGame = "NEW GAME";
        public const string Back = "BACK";

        public const string StoreTitle = "STORE";
        public const string StoreSubtitle = "Choose a character. Your player changes color right away.";
        public const string PriceFree = "FREE";
        public const string PriceDefault = "DEFAULT";
        public const string PriceOwned = "OWNED";
        public const string StatusEquipped = "EQUIPPED";
        public const string ActionEquip = "EQUIP";
        public const string ActionGet = "GET";
        public const string ActionBuy = "BUY";
        public const string ItemObtained = "Unlocked! Your player color changed.";
        public const string ItemPurchased = "Purchased! Your player color changed.";
        public const string GemsRequired = "Gems required. In-app purchase is coming soon.";

        public const string LevelsTitle = "SELECT LEVEL";
        public const string LevelsHint = "Tap a locked level to unlock it with coins, or finish the previous level.";
        public const string LevelPlay = "PLAY";
        public const string LevelPlayLatest = "PLAY  (LATEST)";
        public const string NoProgress = "No progress yet. Press NEW GAME.";
        public const string NewGameStarted = "New game started. Coins and store items are safe.";
        public const string SaveManagerMissing = "Save manager not found.";
        public const string CatalogMissing = "Catalog not found.";
        public const string CatalogHasNoLevels = "Catalog has no levels.";

        public const string ConfirmTitle = "NEW GAME?";
        public const string ConfirmMessage = "Your level progress will be deleted.\nCoins and store items stay safe.";
        public const string ConfirmYes = "YES, START";
        public const string ConfirmCancel = "CANCEL";

        public const string DemoAddCoins = "+500 COINS (DEMO)";
        public const string DemoResetAll = "RESET (DEMO)";
        public const string DemoCompleteHighestLevel = "WIN HIGHEST LEVEL (DEMO)";

        public const string HudControls = "WASD move    Shift run    Space jump (avoid the spikes)";
        public const string LevelComplete = "LEVEL COMPLETE";
        public const string GameOver = "GAME OVER";
        public const string AllLevelsCompleted = "All levels completed!";
        public const string NextLevel = "NEXT LEVEL";
        public const string Retry = "RETRY";
        public const string Menu = "MENU";

        public static string CoinsLabel(int amount) => $"COINS   {amount:N0}";
        public static string GemsLabel(int amount) => $"GEMS    {amount:N0}";
        public static string PriceInCoins(int amount) => $"{amount:N0} COINS";
        public static string PriceInGems(int amount) => $"{amount:N0} GEMS";
        public static string NotEnoughCoins(int price) => $"Not enough coins ({price:N0} needed).";
        public static string ContinueAtLevel(int levelNumber) => $"{Continue}   LEVEL {levelNumber}";
        public static string LevelLocked(int price) => $"LOCKED   {price:N0} COINS";
        public static string LevelUnlocked(string levelName) => $"{levelName} unlocked.";
        public static string LevelCompletedDemo(int levelNumber) => $"Level {levelNumber} completed (demo).";
        public static string SceneNotInBuild(string sceneName) => $"Scene '{sceneName}' is not in Build Settings.";

        public static string HudLevel(int levelNumber) => $"LEVEL {levelNumber}";
        public static string HudHealth(float current, float maximum) => $"HEALTH  {current:0} / {maximum:0}";
        public static string HudCoins(int collected, int total, int walletTotal) =>
            $"COINS  {collected} / {total}     TOTAL  {walletTotal:N0}";
        public static string Repaired(float amount) => $"Repaired +{amount:0}";
        public static string CoinsEarned(int amount) => $"Coins earned: {amount}";
        public static string CoinsKept(int amount) => $"Coins kept: {amount}";
    }
}
