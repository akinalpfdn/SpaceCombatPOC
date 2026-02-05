using System.Collections.Generic;
using UnityEngine;

namespace StarReapers.Audio
{
    /// <summary>
    /// Library of sound effects and music
    /// ScriptableObject for easy asset management
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "StarReapers/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [System.Serializable]
        public class SoundEntry
        {
            public string id;
            public AudioClip clip;

            [Tooltip("Volume: 0 = silent, 100 = full volume")]
            [Range(0, 100)] public int volume = 100;

            [Tooltip("Pitch: 1 = normal, <1 = slower/deeper, >1 = faster/higher")]
            [Range(0.5f, 2f)] public float pitch = 1f;

            [Tooltip("Add random pitch variation for variety")]
            public bool randomizePitch = false;

            [Range(0f, 0.2f)] public float pitchVariation = 0.05f;

            /// <summary>
            /// Returns volume as 0-1 multiplier for AudioSource.
            /// Note: AudioSource.volume is capped at 1.0 by Unity.
            /// </summary>
            public float VolumeMultiplier
            {
                get
                {
                    // Handle migration: old float values (0-1) become 0 or 1 as int
                    if (volume <= 1)
                        return 1f; // Treat as full volume for unmigrated entries
                    return Mathf.Clamp01(volume / 100f);
                }
            }
        }

        [Header("Sound Effects")]
        public List<SoundEntry> soundEffects = new List<SoundEntry>();

        [Header("Music")]
        public List<SoundEntry> musicTracks = new List<SoundEntry>();

        private Dictionary<string, SoundEntry> _sfxLookup;
        private Dictionary<string, SoundEntry> _musicLookup;

        private void OnEnable()
        {
            BuildLookups();
        }

        private void BuildLookups()
        {
            _sfxLookup = new Dictionary<string, SoundEntry>();
            foreach (var entry in soundEffects)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    _sfxLookup[entry.id] = entry;
            }

            _musicLookup = new Dictionary<string, SoundEntry>();
            foreach (var entry in musicTracks)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    _musicLookup[entry.id] = entry;
            }
        }

        public AudioClip GetClip(string id)
        {
            if (_sfxLookup == null) BuildLookups();

            if (_sfxLookup.TryGetValue(id, out var entry))
            {
                return entry.clip;
            }
            return null;
        }

        public AudioClip GetMusicClip(string id)
        {
            if (_musicLookup == null) BuildLookups();

            if (_musicLookup.TryGetValue(id, out var entry))
            {
                return entry.clip;
            }
            return null;
        }

        public SoundEntry GetSoundEntry(string id)
        {
            if (_sfxLookup == null) BuildLookups();
            _sfxLookup.TryGetValue(id, out var entry);
            return entry;
        }
    }
}
