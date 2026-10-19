using System;
using System.Collections;
using bison.core.Common;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace bison.core.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const string PreferencePrefix = "bison.audio.";
        private const string MasterParameter = "MasterVolume";
        private const string MusicParameter = "MusicVolume";
        private const string SfxParameter = "SfxVolume";
        private const int SfxVoiceCount = 12;
        private const float MinimumLinearVolume = 0.0001f;
        private const float DecibelsPerDecade = 20f;

        [SerializeField, FormerlySerializedAs("_mixer")] private AudioMixer mixer;
        [SerializeField, FormerlySerializedAs("_library")] private AudioLibrary library;
        [SerializeField, FormerlySerializedAs("_musicGroup")] private AudioMixerGroup musicGroup;
        [SerializeField, FormerlySerializedAs("_sfxGroup")] private AudioMixerGroup sfxGroup;

        private readonly float[] channelVolumes = { 1f, 0.8f, 1f };

        private AudioSource activeMusicSource;
        private AudioSource idleMusicSource;
        private AudioSource[] sfxVoices;
        private int nextVoiceIndex;
        private Coroutine musicRoutine;

        public static AudioManager Instance { get; private set; }

        public SoundId CurrentMusic { get; private set; }

        public event Action<AudioChannel, float> VolumeChanged;

        // Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            AudioManager prefab = Resources.Load<AudioManager>(ResourcePaths.AudioManager);

            if (prefab == null)
            {
                Debug.LogError(LogMessages.MissingResource(ResourcePaths.AudioManager));
                return;
            }

            Instantiate(prefab);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            name = nameof(AudioManager);
            DontDestroyOnLoad(gameObject);

            LoadVolumes();
            BuildSources();
        }

        private void Start()
        {
            for (int channelIndex = 0; channelIndex < channelVolumes.Length; channelIndex++)
                ApplyVolumeToMixer((AudioChannel)channelIndex);
        }

        private void OnApplicationPause(bool is_Paused)
        {
            if (is_Paused) SaveSettings();
        }

        private void OnApplicationQuit() => SaveSettings();

        private void BuildSources()
        {
            activeMusicSource = CreateSource(musicGroup);
            idleMusicSource = CreateSource(musicGroup);

            sfxVoices = new AudioSource[SfxVoiceCount];
            for (int voiceIndex = 0; voiceIndex < SfxVoiceCount; voiceIndex++)
                sfxVoices[voiceIndex] = CreateSource(sfxGroup);
        }

        private AudioSource CreateSource(AudioMixerGroup mixerGroup)
        {
            GameObject sourceObject = new GameObject(nameof(AudioSource));
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        // Sound effects

        public void PlaySfx(SoundId soundId)
        {
            if (!TryGetPlayableSound(soundId, out SoundDefinition sound)) return;

            AudioSource voice = sfxVoices[nextVoiceIndex];
            nextVoiceIndex = (nextVoiceIndex + 1) % sfxVoices.Length;

            voice.clip = sound.PickClip();
            voice.volume = sound.Volume;
            voice.pitch = sound.PickPitch();
            voice.loop = false;
            voice.Play();
        }

        // Music

        public void PlayMusic(SoundId soundId, float fadeDuration = 1f)
        {
            if (soundId == SoundId.None) return;
            if (CurrentMusic == soundId && activeMusicSource.isPlaying) return;
            if (!TryGetPlayableSound(soundId, out SoundDefinition sound)) return;

            CurrentMusic = soundId;

            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(CrossfadeMusic(sound, fadeDuration));
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            CurrentMusic = SoundId.None;

            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(FadeOutMusic(fadeDuration));
        }

        private IEnumerator CrossfadeMusic(SoundDefinition sound, float fadeDuration)
        {
            AudioSource fadingOutSource = activeMusicSource;
            AudioSource fadingInSource = idleMusicSource;
            activeMusicSource = fadingInSource;
            idleMusicSource = fadingOutSource;

            fadingInSource.clip = sound.PickClip();
            fadingInSource.loop = sound.IsLooping;
            fadingInSource.pitch = 1f;
            fadingInSource.volume = 0f;
            fadingInSource.Play();

            float startVolume = fadingOutSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadingInSource.volume = Mathf.Lerp(0f, sound.Volume, normalizedTime);
                fadingOutSource.volume = Mathf.Lerp(startVolume, 0f, normalizedTime);
                yield return null;
            }

            fadingInSource.volume = sound.Volume;
            fadingOutSource.volume = 0f;
            fadingOutSource.Stop();
            musicRoutine = null;
        }

        private IEnumerator FadeOutMusic(float fadeDuration)
        {
            AudioSource source = activeMusicSource;
            float startVolume = source.volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsedTime / fadeDuration));
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
            musicRoutine = null;
        }

        // Volume

        public float GetVolume(AudioChannel channel) => channelVolumes[(int)channel];

        public void SetVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            channelVolumes[(int)channel] = volume;

            ApplyVolumeToMixer(channel);
            PlayerPrefs.SetFloat(GetPreferenceKey(channel), volume);
            VolumeChanged?.Invoke(channel, volume);
        }

        public void SaveSettings() => PlayerPrefs.Save();

        private void LoadVolumes()
        {
            for (int channelIndex = 0; channelIndex < channelVolumes.Length; channelIndex++)
            {
                string preferenceKey = GetPreferenceKey((AudioChannel)channelIndex);
                channelVolumes[channelIndex] = PlayerPrefs.GetFloat(preferenceKey, channelVolumes[channelIndex]);
            }
        }

        private void ApplyVolumeToMixer(AudioChannel channel)
        {
            if (mixer == null) return;

            float linearVolume = Mathf.Max(channelVolumes[(int)channel], MinimumLinearVolume);
            mixer.SetFloat(GetMixerParameter(channel), Mathf.Log10(linearVolume) * DecibelsPerDecade);
        }

        private static string GetPreferenceKey(AudioChannel channel) => PreferencePrefix + channel;

        private static string GetMixerParameter(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Music: return MusicParameter;
                case AudioChannel.Sfx: return SfxParameter;
                default: return MasterParameter;
            }
        }

        // Lookup

        private bool TryGetPlayableSound(SoundId soundId, out SoundDefinition sound)
        {
            sound = null;
            if (soundId == SoundId.None) return false;

            if (library == null)
            {
                Debug.LogError(LogMessages.AudioLibraryMissing);
                return false;
            }

            if (!library.TryGetSound(soundId, out sound))
            {
                Debug.LogWarning(LogMessages.SoundNotFound(soundId.ToString()));
                return false;
            }

            if (!sound.HasClips)
            {
                Debug.LogWarning(LogMessages.SoundHasNoClips(soundId.ToString()));
                return false;
            }

            return true;
        }
    }
}
