using bison.core.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace bison.core.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSound : MonoBehaviour, ISelectHandler
    {
        private const float StartupSilenceSeconds = 0.3f;

        [SerializeField] private SoundId hoverSound = SoundId.UiHover;
        [SerializeField] private SoundId clickSound = SoundId.UiClick;

        private Button button;
        private float silentUntilTime;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            silentUntilTime = Time.unscaledTime + StartupSilenceSeconds;
            button.onClick.AddListener(OnClicked);
        }

        private void OnDisable() => button.onClick.RemoveListener(OnClicked);

        public void OnSelect(BaseEventData eventData)
        {
            if (Time.unscaledTime < silentUntilTime) return;
            PlaySound(hoverSound);
        }

        private void OnClicked() => PlaySound(clickSound);

        private static void PlaySound(SoundId soundId)
        {
            if (soundId == SoundId.None || AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySfx(soundId);
        }
    }
}
