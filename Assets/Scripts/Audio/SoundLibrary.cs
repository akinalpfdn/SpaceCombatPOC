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
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
            public bool randomizePitch = false;
            [Range(0f, 0.5f)] public float pitchVariation = 0.1f;
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
