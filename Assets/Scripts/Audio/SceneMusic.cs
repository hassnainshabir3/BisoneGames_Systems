using UnityEngine;
using UnityEngine.Serialization;

namespace bison.core.Audio
{
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] private SoundId musicId = SoundId.None;
        [SerializeField, FormerlySerializedAs("_fade")] private float fadeDuration = 1f;
        [SerializeField, FormerlySerializedAs("_stopMusicIfEmpty")] private bool should_StopWhenNone;

        private void Start()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null) return;

            if (musicId == SoundId.None)
            {
                if (should_StopWhenNone) audioManager.StopMusic(fadeDuration);
                return;
            }

            audioManager.PlayMusic(musicId, fadeDuration);
        }
    }
}
