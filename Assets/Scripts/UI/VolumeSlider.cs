using bison.core.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace bison.core.UI
{
    [RequireComponent(typeof(Slider))]
    public sealed class VolumeSlider : MonoBehaviour, IPointerUpHandler
    {
        [SerializeField, FormerlySerializedAs("_channel")] private AudioChannel channel = AudioChannel.Master;
        [SerializeField] private SoundId previewSound = SoundId.None;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private void OnEnable()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null) slider.SetValueWithoutNotify(audioManager.GetVolume(channel));

            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);

            if (AudioManager.Instance != null) AudioManager.Instance.SaveSettings();
        }

        private void OnValueChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetVolume(channel, value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (previewSound == SoundId.None || AudioManager.Instance == null) return;
            AudioManager.Instance.PlaySfx(previewSound);
        }
    }
}
