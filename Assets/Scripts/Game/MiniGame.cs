using System.Collections.Generic;
using bison.core.Audio;
using bison.core.Common;
using bison.core.Save;
using bison.core.Store;
using bison.core.Transition;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace bison.game.Mini
{
    public sealed class MiniGame : MonoBehaviour
    {
        private enum GameState { Playing, Won, Lost }

        private const float ReferenceHeight = 1080f;
        private const float WallThickness = 1f;
        private const float CoinCollectRadius = 1f;
        private const float RepairCollectRadius = 1.2f;
        private const float SpikeHalfExtent = 1.2f;
        private const float BladeHalfWidth = 2.4f;
        private const float BladeHalfDepth = 0.7f;
        private const float FlashFadeSpeed = 2.5f;
        private const float MessageSeconds = 1.5f;
        private const float CameraSmoothTime = 0.15f;
        private const float BladeBaseSpeed = 0.3f;
        private const float BladeSpeedGrowthPerLevel = 0.15f;
        private const float RoomGrowthPerLevel = 0.5f;
        private const int CoinGrowthPerLevel = 2;
        private const int PlacementAttempts = 40;

        [Header("Player")]
        [SerializeField, FormerlySerializedAs("_playerPrefab")] private GameObject playerPrefab;
        [SerializeField, Min(1f), FormerlySerializedAs("_maxHealth")] private float maximumHealth = 100f;
        [SerializeField, FormerlySerializedAs("_cameraOffset")] private Vector3 cameraOffset = new Vector3(0f, 11f, -8f);

        [Header("Level")]
        [SerializeField, Min(6f), FormerlySerializedAs("_roomHalfSize")] private float baseRoomHalfSize = 10f;
        [SerializeField, Min(1), FormerlySerializedAs("_coinCount")] private int baseCoinCount = 8;
        [SerializeField, Min(0), FormerlySerializedAs("_spikeCount")] private int baseSpikeCount = 4;
        [SerializeField, Min(0), FormerlySerializedAs("_repairCount")] private int baseRepairCount = 2;
        [SerializeField, Min(0), FormerlySerializedAs("_winBonusCoins")] private int baseWinBonusCoins = 20;

        [Header("Damage and Repair")]
        [SerializeField, FormerlySerializedAs("_spikeDamage")] private float spikeDamage = 20f;
        [SerializeField, FormerlySerializedAs("_bladeDamage")] private float bladeDamage = 25f;
        [SerializeField, FormerlySerializedAs("_repairAmount")] private float repairAmount = 35f;
        [SerializeField, Min(0.1f), FormerlySerializedAs("_hitCooldown")] private float hitCooldownSeconds = 0.8f;

        [Header("Scenes")]
        [SerializeField, FormerlySerializedAs("_menuScene")] private string menuSceneName = SceneNames.MainMenu;
        [SerializeField, FormerlySerializedAs("_transition")] private TransitionStyle transitionStyle = TransitionStyle.Splash;

        private readonly List<Transform> coins = new List<Transform>();
        private readonly List<Transform> spikes = new List<Transform>();
        private readonly List<Transform> repairs = new List<Transform>();

        private GameState gameState = GameState.Playing;
        private Transform levelRoot;
        private Transform playerTransform;
        private Transform bladeTransform;
        private Camera mainCamera;
        private Vector3 cameraVelocity;

        private int levelIndex;
        private string levelId = string.Empty;
        private string nextSceneName = string.Empty;
        private bool has_NextLevel;

        private float roomHalfSize;
        private float bladeSpeed;
        private int coinCount;
        private int spikeCount;
        private int repairCount;
        private int winBonusCoins;

        private float health;
        private float nextHitTime;
        private float flashAmount;
        private int collectedCoins;
        private int totalCoins;
        private int earnedCoins;

        private string hudMessage = string.Empty;
        private float hudMessageUntilTime;

        private GUIStyle labelStyle;
        private GUIStyle centerStyle;
        private GUIStyle bigStyle;
        private GUIStyle buttonStyle;

        // Lifecycle

        private void Start()
        {
            Time.timeScale = 1f;
            health = maximumHealth;

            ResolveLevel();
            ApplyDifficulty();

            BuildRoom();
            PlaceItems();
            SpawnPlayer();
            SetupCamera();
        }

        private void OnDestroy() => Time.timeScale = 1f;

        private void Update()
        {
            if (flashAmount > 0f)
                flashAmount = Mathf.MoveTowards(flashAmount, 0f, Time.unscaledDeltaTime * FlashFadeSpeed);

            if (playerTransform == null) return;

            AnimateObjects();

            if (gameState != GameState.Playing) return;

            CheckCoins();
            CheckRepairs();
            CheckHazards();
        }

        private void LateUpdate()
        {
            if (playerTransform == null || mainCamera == null) return;

            Vector3 targetPosition = playerTransform.position + cameraOffset;
            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position, targetPosition, ref cameraVelocity, CameraSmoothTime,
                float.MaxValue, Time.unscaledDeltaTime);

            Vector3 lookTarget = playerTransform.position + Vector3.up;
            mainCamera.transform.rotation = Quaternion.LookRotation(lookTarget - mainCamera.transform.position);
        }

        // Level setup

        private void ResolveLevel()
        {
            levelIndex = 0;
            has_NextLevel = false;

            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null || saveManager.Catalog == null) return;

            GameCatalog catalog = saveManager.Catalog;

            if (!catalog.TryFindLevelByScene(SceneManager.GetActiveScene().name, out int foundIndex)) return;

            levelIndex = foundIndex;
            levelId = catalog.Levels[levelIndex].Id;

            int nextIndex = levelIndex + 1;
            if (nextIndex >= catalog.Levels.Count) return;

            nextSceneName = catalog.Levels[nextIndex].SceneName;
            has_NextLevel = true;
        }

        private void ApplyDifficulty()
        {
            roomHalfSize = baseRoomHalfSize + levelIndex * RoomGrowthPerLevel;
            coinCount = baseCoinCount + levelIndex * CoinGrowthPerLevel;
            spikeCount = baseSpikeCount + levelIndex;
            repairCount = baseRepairCount + levelIndex / 2;
            winBonusCoins = baseWinBonusCoins * (levelIndex + 1);
            bladeSpeed = BladeBaseSpeed * (1f + levelIndex * BladeSpeedGrowthPerLevel);
        }

        private void BuildRoom()
        {
            levelRoot = new GameObject(nameof(MiniGame)).transform;

            Color floorColor = new Color(0.78f, 0.80f, 0.82f);
            Color wallColor = new Color(0.45f, 0.48f, 0.52f);
            float fullSize = roomHalfSize * 2f;
            float wallOffset = roomHalfSize + WallThickness * 0.5f;
            float wallLength = fullSize + WallThickness * 2f;

            CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, new Vector3(0f, -0.5f, 0f),
                                  new Vector3(fullSize, 1f, fullSize), floorColor, true);

            CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, new Vector3(0f, 1f, wallOffset),
                                  new Vector3(wallLength, 2f, WallThickness), wallColor, true);
            CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, new Vector3(0f, 1f, -wallOffset),
                                  new Vector3(wallLength, 2f, WallThickness), wallColor, true);
            CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, new Vector3(wallOffset, 1f, 0f),
                                  new Vector3(WallThickness, 2f, fullSize), wallColor, true);
            CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, new Vector3(-wallOffset, 1f, 0f),
                                  new Vector3(WallThickness, 2f, fullSize), wallColor, true);
        }

        private void PlaceItems()
        {
            List<Vector3> takenSpots = new List<Vector3>();

            for (int coinIndex = 0; coinIndex < coinCount; coinIndex++)
            {
                Vector3 spot = FindFreeSpot(takenSpots, 2.5f);

                Transform coin = new GameObject().transform;
                coin.SetParent(levelRoot, false);
                coin.position = spot + Vector3.up;

                Transform disc = CreatePrimitiveObject(PrimitiveType.Cylinder, coin, Vector3.zero,
                                                       new Vector3(0.6f, 0.05f, 0.6f), new Color(1f, 0.82f, 0.1f), false);
                disc.localRotation = Quaternion.Euler(90f, 0f, 0f);

                coins.Add(coin);
            }

            totalCoins = coins.Count;

            for (int spikeIndex = 0; spikeIndex < spikeCount; spikeIndex++)
            {
                Vector3 spot = FindFreeSpot(takenSpots, 3.5f);
                spikes.Add(CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, spot + Vector3.up * 0.1f,
                                                 new Vector3(2f, 0.2f, 2f), new Color(0.85f, 0.15f, 0.15f), false));
            }

            for (int repairIndex = 0; repairIndex < repairCount; repairIndex++)
            {
                Vector3 spot = FindFreeSpot(takenSpots, 3f);

                Transform repair = new GameObject().transform;
                repair.SetParent(levelRoot, false);
                repair.position = spot + Vector3.up * 0.9f;

                CreatePrimitiveObject(PrimitiveType.Cube, repair, Vector3.zero, Vector3.one * 0.7f,
                                      new Color(0.25f, 0.85f, 0.4f), false);

                repairs.Add(repair);
            }

            bladeTransform = CreatePrimitiveObject(PrimitiveType.Cube, levelRoot, new Vector3(0f, 0.4f, -3f),
                                                   new Vector3(4f, 0.8f, 0.5f), new Color(0.5f, 0.05f, 0.08f), false);
        }

        private Vector3 FindFreeSpot(List<Vector3> takenSpots, float minimumGap)
        {
            float limit = roomHalfSize - 1.5f;
            Vector3 candidate = Vector3.zero;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                candidate = new Vector3(Random.Range(-limit, limit), 0f, Random.Range(-limit, limit));

                if (candidate.sqrMagnitude < 12f) continue;

                bool is_Free = true;

                foreach (Vector3 takenSpot in takenSpots)
                {
                    if ((takenSpot - candidate).sqrMagnitude >= minimumGap * minimumGap) continue;

                    is_Free = false;
                    break;
                }

                if (is_Free) break;
            }

            takenSpots.Add(candidate);
            return candidate;
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError(LogMessages.PlayerPrefabMissing);
                return;
            }

            GameObject playerObject = Instantiate(playerPrefab, new Vector3(0f, 0.2f, 0f), Quaternion.identity);
            playerObject.tag = ObjectTags.Player;
            playerTransform = playerObject.transform;

            PlayerSkin.AttachTo(playerObject);
        }

        private void SetupCamera()
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject(nameof(Camera), typeof(Camera), typeof(AudioListener));
                cameraObject.tag = ObjectTags.MainCamera;
                mainCamera = cameraObject.GetComponent<Camera>();
            }

            if (playerTransform != null) mainCamera.transform.position = playerTransform.position + cameraOffset;
        }

        // Gameplay

        private void AnimateObjects()
        {
            foreach (Transform coin in coins)
                if (coin != null) coin.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);

            foreach (Transform repair in repairs)
                if (repair != null) repair.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

            if (bladeTransform == null) return;

            float travelRange = roomHalfSize - 3f;
            float pingPong = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(Time.time * bladeSpeed, 1f));
            Vector3 bladePosition = bladeTransform.position;
            bladePosition.x = Mathf.Lerp(-travelRange, travelRange, pingPong);
            bladeTransform.position = bladePosition;
        }

        private void CheckCoins()
        {
            Vector3 playerPosition = playerTransform.position;

            for (int coinIndex = coins.Count - 1; coinIndex >= 0; coinIndex--)
            {
                if (HorizontalDistance(coins[coinIndex].position, playerPosition) > CoinCollectRadius) continue;

                Destroy(coins[coinIndex].gameObject);
                coins.RemoveAt(coinIndex);

                collectedCoins++;
                earnedCoins++;
                if (SaveManager.Instance != null) SaveManager.Instance.AddCoins(1);
                PlaySound(SoundId.Coin);

                if (collectedCoins >= totalCoins)
                {
                    WinLevel();
                    return;
                }
            }
        }

        private void CheckRepairs()
        {
            if (health >= maximumHealth) return;

            Vector3 playerPosition = playerTransform.position;

            for (int repairIndex = repairs.Count - 1; repairIndex >= 0; repairIndex--)
            {
                if (HorizontalDistance(repairs[repairIndex].position, playerPosition) > RepairCollectRadius) continue;

                Destroy(repairs[repairIndex].gameObject);
                repairs.RemoveAt(repairIndex);

                health = Mathf.Min(maximumHealth, health + repairAmount);
                ShowMessage(UiText.Repaired(repairAmount));
                PlaySound(SoundId.Repair);
                return;
            }
        }

        private void CheckHazards()
        {
            if (Time.time < nextHitTime) return;

            Vector3 playerPosition = playerTransform.position;

            if (playerPosition.y < 0.7f)
            {
                foreach (Transform spike in spikes)
                {
                    bool is_InsideX = Mathf.Abs(playerPosition.x - spike.position.x) < SpikeHalfExtent;
                    bool is_InsideZ = Mathf.Abs(playerPosition.z - spike.position.z) < SpikeHalfExtent;

                    if (!is_InsideX || !is_InsideZ) continue;

                    TakeDamage(spikeDamage);
                    return;
                }
            }

            if (bladeTransform == null || playerPosition.y >= 1.2f) return;

            bool is_InsideBladeX = Mathf.Abs(playerPosition.x - bladeTransform.position.x) < BladeHalfWidth;
            bool is_InsideBladeZ = Mathf.Abs(playerPosition.z - bladeTransform.position.z) < BladeHalfDepth;

            if (is_InsideBladeX && is_InsideBladeZ) TakeDamage(bladeDamage);
        }

        private void TakeDamage(float amount)
        {
            nextHitTime = Time.time + hitCooldownSeconds;
            health = Mathf.Max(0f, health - amount);
            flashAmount = 1f;
            PlaySound(SoundId.Hurt);

            if (health <= 0f) LoseLevel();
        }

        // Win and lose

        private void WinLevel()
        {
            gameState = GameState.Won;

            SaveManager saveManager = SaveManager.Instance;

            if (saveManager != null)
            {
                saveManager.AddCoins(winBonusCoins);
                saveManager.CompleteLevel(levelId);
            }

            earnedCoins += winBonusCoins;
            PlaySound(SoundId.Win);
            EndLevel();
        }

        private void LoseLevel()
        {
            gameState = GameState.Lost;
            PlaySound(SoundId.Lose);
            EndLevel();
        }

        private void EndLevel()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (SaveManager.Instance != null) SaveManager.Instance.SaveNow();
        }

        private void RetryLevel() => SceneTransition.Load(SceneManager.GetActiveScene().name, transitionStyle);

        private void LoadNextLevel() => SceneTransition.Load(nextSceneName, transitionStyle);

        private void LoadMenu() => SceneTransition.Load(menuSceneName, transitionStyle);

        private void ShowMessage(string message)
        {
            hudMessage = message;
            hudMessageUntilTime = Time.unscaledTime + MessageSeconds;
        }

        private static void PlaySound(SoundId soundId)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(soundId);
        }

        // Interface

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;

            Texture2D whiteTexture = Texture2D.whiteTexture;

            if (flashAmount > 0f)
            {
                GUI.color = new Color(1f, 0.1f, 0.1f, flashAmount * 0.35f);
                GUI.DrawTexture(new Rect(0f, 0f, screenWidth, ReferenceHeight), whiteTexture);
            }

            DrawHud(screenWidth, whiteTexture);

            if (gameState != GameState.Playing) DrawEndScreen(screenWidth, whiteTexture);
        }

        private void DrawHud(float screenWidth, Texture2D whiteTexture)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(30f, 30f, 640f, 230f), whiteTexture);

            float healthRatio = maximumHealth > 0f ? health / maximumHealth : 0f;
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(new Rect(50f, 50f, 600f, 34f), whiteTexture);
            GUI.color = Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.3f, 0.8f, 0.4f), healthRatio);
            GUI.DrawTexture(new Rect(50f, 50f, 600f * healthRatio, 34f), whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(50f, 92f, 600f, 44f), UiText.HudLevel(levelIndex + 1), labelStyle);
            GUI.Label(new Rect(50f, 136f, 600f, 44f),
                      UiText.HudHealth(Mathf.CeilToInt(health), Mathf.CeilToInt(maximumHealth)), labelStyle);

            int walletTotal = SaveManager.Instance != null ? SaveManager.Instance.Data.Coins : 0;
            GUI.Label(new Rect(50f, 180f, 600f, 44f), UiText.HudCoins(collectedCoins, totalCoins, walletTotal), labelStyle);

            if (Time.unscaledTime < hudMessageUntilTime)
            {
                Rect messageRect = new Rect(screenWidth * 0.5f - 300f, ReferenceHeight - 190f, 600f, 70f);
                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(messageRect, whiteTexture);
                GUI.color = Color.white;
                GUI.Label(messageRect, hudMessage, centerStyle);
            }

            GUI.Label(new Rect(30f, ReferenceHeight - 70f, 1400f, 50f), UiText.HudControls, labelStyle);
        }

        private void DrawEndScreen(float screenWidth, Texture2D whiteTexture)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0f, 0f, screenWidth, ReferenceHeight), whiteTexture);
            GUI.color = Color.white;

            bool is_Won = gameState == GameState.Won;
            string titleContent = is_Won ? UiText.LevelComplete : UiText.GameOver;
            string detailContent = is_Won ? UiText.CoinsEarned(earnedCoins) : UiText.CoinsKept(earnedCoins);

            GUI.Label(new Rect(0f, 240f, screenWidth, 130f), titleContent, bigStyle);
            GUI.Label(new Rect(0f, 380f, screenWidth, 60f), detailContent, centerStyle);

            float buttonX = screenWidth * 0.5f - 210f;
            float buttonY = 500f;

            if (is_Won && has_NextLevel)
            {
                if (GUI.Button(new Rect(buttonX, buttonY, 420f, 80f), UiText.NextLevel, buttonStyle)) LoadNextLevel();
                buttonY += 100f;
            }
            else if (is_Won)
            {
                GUI.Label(new Rect(0f, buttonY, screenWidth, 60f), UiText.AllLevelsCompleted, centerStyle);
                buttonY += 100f;
            }

            if (GUI.Button(new Rect(buttonX, buttonY, 420f, 80f), UiText.Retry, buttonStyle)) RetryLevel();
            if (GUI.Button(new Rect(buttonX, buttonY + 100f, 420f, 80f), UiText.Menu, buttonStyle)) LoadMenu();
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;

            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.white;

            centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };
            bigStyle = new GUIStyle(centerStyle) { fontSize = 90 };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 40, fontStyle = FontStyle.Bold };
        }

        // Helpers

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            float differenceX = first.x - second.x;
            float differenceZ = first.z - second.z;
            return Mathf.Sqrt(differenceX * differenceX + differenceZ * differenceZ);
        }

        private static Transform CreatePrimitiveObject(PrimitiveType primitiveType, Transform parent,
                                                       Vector3 localPosition, Vector3 localScale,
                                                       Color color, bool has_Collider)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;

            MaterialTinter.Apply(primitive, color, false);

            if (!has_Collider) Destroy(primitive.GetComponent<Collider>());
            return primitive.transform;
        }
    }
}
