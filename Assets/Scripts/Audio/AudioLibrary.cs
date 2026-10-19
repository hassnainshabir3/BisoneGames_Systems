using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace bison.core.Audio
{
    public enum AudioChannel
    {
        Master,
        Music,
        Sfx
    }

    public enum SoundId
    {
        None,
        UiHover,
        UiClick,
        MenuMusic,
        GameMusic,
        Coin,
        Hurt,
        Repair,
        Win,
        Lose,
        Purchase
    }

    [Serializable]
    public sealed class SoundDefinition
    {
        [SerializeField] private SoundId soundId;
        [SerializeField, FormerlySerializedAs("_clips")] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("_volume")] private float volume = 1f;
        [SerializeField, Range(0.5f, 2f), FormerlySerializedAs("_pitchMin")] private float minimumPitch = 1f;
        [SerializeField, Range(0.5f, 2f), FormerlySerializedAs("_pitchMax")] private float maximumPitch = 1f;
        [SerializeField, FormerlySerializedAs("_loop")] private bool is_Looping;

        public SoundId Identifier => soundId;
        public float Volume => volume;
        public bool IsLooping => is_Looping;
        public bool HasClips => clips != null && clips.Length > 0;

        public AudioClip PickClip() => clips[UnityEngine.Random.Range(0, clips.Length)];
        public float PickPitch() => UnityEngine.Random.Range(minimumPitch, maximumPitch);
    }

    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Bison/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("_sounds")] private List<SoundDefinition> sounds = new List<SoundDefinition>();

        private Dictionary<SoundId, SoundDefinition> soundLookup;

        private void OnEnable() => soundLookup = null;

        public bool TryGetSound(SoundId soundId, out SoundDefinition sound)
        {
            if (soundLookup == null) BuildLookup();
            return soundLookup.TryGetValue(soundId, out sound);
        }

        private void BuildLookup()
        {
            soundLookup = new Dictionary<SoundId, SoundDefinition>(sounds.Count);

            foreach (SoundDefinition sound in sounds)
            {
                if (sound == null || sound.Identifier == SoundId.None) continue;
                if (!soundLookup.ContainsKey(sound.Identifier)) soundLookup.Add(sound.Identifier, sound);
            }
        }
    }
}
