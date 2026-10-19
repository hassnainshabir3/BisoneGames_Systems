using bison.core.Common;
using bison.core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace bison.core.Store
{
    public sealed class PlayerSkin : MonoBehaviour
    {
        // Static entry points

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachToTaggedPlayers();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode) => AttachToTaggedPlayers();

        private static void AttachToTaggedPlayers()
        {
            GameObject[] players;

            try { players = GameObject.FindGameObjectsWithTag(ObjectTags.Player); }
            catch (UnityException) { return; }

            foreach (GameObject player in players) AttachTo(player);
        }

        public static void AttachTo(GameObject target)
        {
            if (target.GetComponent<PlayerSkin>() == null) target.AddComponent<PlayerSkin>();
        }

        // Lifecycle

        private void OnEnable()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Changed -= Apply;
        }

        // Skin

        public void Apply()
        {
            Color tint = Color.white;
            SaveManager saveManager = SaveManager.Instance;

            if (saveManager != null &&
                saveManager.TryGetEquippedItem(ItemSlot.Character, out ShopItem equippedItem) &&
                equippedItem.IsTintEnabled)
            {
                tint = equippedItem.Tint;
            }

            MaterialTinter.Apply(gameObject, tint, true);
        }
    }
}
