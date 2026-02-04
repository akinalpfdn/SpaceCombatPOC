using System.Collections.Generic;
using UnityEngine;

namespace SpaceCombat.Audio
{
    /// <summary>
    /// Library of sound effects and music
    /// ScriptableObject for easy asset management
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "SpaceCombat/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [System.Serializable]
        public class SoundEntry
        {
            public string id;
            public AudioClip clip;

            [Tooltip("Volume percentage: 0 = silent, 100 = normal, 200 = double volume")]
            [Range(0, 200)] public int volume = 100;

            [Tooltip("Pitch multiplier: 1 = normal, 0.5 = half speed, 2 = double speed")]
            [Range(0.5f, 2f)] public float pitch = 1f;

            [Tooltip("Add random pitch variation for variety")]
            public bool randomizePitch = false;

            [Range(0f, 0.3f)] public float pitchVariation = 0.1f;

            /// <summary>
            /// Returns volume as a multiplier (0-2 range).
            /// Handles migration from old 0-1 float values (treats values < 10 as old format).
            /// </summary>
            public float VolumeMultiplier
            {
                get
                {
                    // Migration: if volume is suspiciously low (< 10), treat as unmigrated
                    // Old values were 0-1 float, which become 0 or 1 as int
                    if (volume <= 1)
                        return 1f; // Default to normal volume for unmigrated entries
                    return volume / 100f;
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
