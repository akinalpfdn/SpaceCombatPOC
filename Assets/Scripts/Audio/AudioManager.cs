// ============================================
// AUDIO SYSTEM - Event-Driven
// Centralized audio management with pooling
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Events;
using SpaceCombat.Core;

namespace SpaceCombat.Audio
{
    /// <summary>
    /// Central audio manager
    /// Handles all sound effects and music
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        public static AudioManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private float _sfxVolume = 1f;
        [SerializeField] private float _musicVolume = 0.7f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private int _sfxPoolSize = 10;

        [Header("Sound Library")]
        [SerializeField] private SoundLibrary _soundLibrary;

        // Pooled audio sources for SFX
        private List<AudioSource> _sfxPool;
        private int _currentSfxIndex;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register as service
            ServiceLocator.Register<IAudioService>(this);

            InitializeAudioSources();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeAudioSources()
        {
            // Setup music source
            if (_musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }

            // Create SFX pool
            _sfxPool = new List<AudioSource>(_sfxPoolSize);
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var sfxObj = new GameObject($"SFXSource_{i}");
                sfxObj.transform.SetParent(transform);
                var source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D sound
                _sfxPool.Add(source);
            }

            UpdateVolumes();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<PlaySFXEvent>(OnPlaySFXEvent);
            EventBus.Subscribe<ExplosionEvent>(OnExplosionEvent);
            EventBus.Subscribe<ProjectileFiredEvent>(OnProjectileFiredEvent);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<PlaySFXEvent>(OnPlaySFXEvent);
            EventBus.Unsubscribe<ExplosionEvent>(OnExplosionEvent);
            EventBus.Unsubscribe<ProjectileFiredEvent>(OnProjectileFiredEvent);
        }

        // ============================================
        // IAudioService Implementation
        // ============================================

        public void PlaySFX(string sfxId, Vector2? position = null)
        {
            if (_soundLibrary == null)
            {
                Debug.LogWarning($"Sound library not assigned!");
                return;
            }

            var clip = _soundLibrary.GetClip(sfxId);
            if (clip == null)
            {
                Debug.LogWarning($"Sound not found: {sfxId}");
                return;
            }

            PlaySFXClip(clip, position);
        }

        public void PlaySFXClip(AudioClip clip, Vector2? position = null, float volumeScale = 1f)
        {
            if (clip == null) return;

            // Get next available source from pool
            var source = GetNextSFXSource();
            
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            
            // Position for 3D sound (if needed)
            if (position.HasValue)
            {
                source.transform.position = position.Value;
            }

            source.Play();
        }

        public void PlayMusic(string musicId, bool loop = true)
        {
            if (_soundLibrary == null) return;

            var clip = _soundLibrary.GetMusicClip(musicId);
            if (clip == null)
            {
                Debug.LogWarning($"Music not found: {musicId}");
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _musicVolume * _masterVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicVolume * _masterVolume;
        }

        // ============================================
        // Event Handlers
        // ============================================

        private void OnPlaySFXEvent(PlaySFXEvent evt)
        {
            PlaySFX(evt.SFXId, evt.Position);
        }

        private void OnExplosionEvent(ExplosionEvent evt)
        {
            string sfxId = evt.Size switch
            {
                ExplosionSize.Small => "explosion_small",
                ExplosionSize.Medium => "explosion_medium",
                ExplosionSize.Large => "explosion_large",
                _ => "explosion_medium"
            };

            PlaySFX(sfxId, evt.Position);
        }

        private void OnProjectileFiredEvent(ProjectileFiredEvent evt)
        {
            // Could play weapon-specific sounds based on WeaponType
            // For now, handled by WeaponController
        }

        // ============================================
        // Helpers
        // ============================================

        private AudioSource GetNextSFXSource()
        {
            var source = _sfxPool[_currentSfxIndex];
            _currentSfxIndex = (_currentSfxIndex + 1) % _sfxPool.Count;
            return source;
        }

        private void UpdateVolumes()
        {
            _musicSource.volume = _musicVolume * _masterVolume;
        }
    }

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
