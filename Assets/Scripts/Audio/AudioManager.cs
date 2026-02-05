// ============================================
// AUDIO SYSTEM - Event-Driven
// Centralized audio management with pooling
// ============================================

using System;
using System.Collections.Generic;
using UnityEngine;
using StarReapers.Interfaces;
using StarReapers.Events;

namespace StarReapers.Audio
{
    /// <summary>
    /// Central audio manager
    /// Handles all sound effects and music
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
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
            InitializeAudioSources();
            SubscribeToEvents();

            // Fallback: Load SoundLibrary from Resources if not assigned
            if (_soundLibrary == null)
            {
                Debug.LogWarning("[AudioManager] SoundLibrary not assigned, trying to load from Resources...");
                _soundLibrary = Resources.Load<SoundLibrary>("SoundLibrary");

                if (_soundLibrary != null)
                {
                    Debug.Log("[AudioManager] SoundLibrary loaded from Resources!");
                }
                else
                {
                    Debug.LogError("[AudioManager] SoundLibrary not found in Resources folder!");
                }
            }

            // Diagnostic logging
            Debug.Log($"[AudioManager] Awake - SoundLibrary: {(_soundLibrary != null ? _soundLibrary.name : "NULL!")}");
            if (_soundLibrary != null)
            {
                var testEntry = _soundLibrary.GetSoundEntry("laser_player");
                Debug.Log($"[AudioManager] Test entry 'laser_player': {(testEntry != null ? (testEntry.clip != null ? testEntry.clip.name : "clip NULL") : "entry NULL")}");
            }
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
                Debug.LogWarning($"[AudioManager] PlaySFX({sfxId}) - SoundLibrary is NULL!");
                return;
            }

            var entry = _soundLibrary.GetSoundEntry(sfxId);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioManager] PlaySFX({sfxId}) - Entry not found in SoundLibrary!");
                return;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] PlaySFX({sfxId}) - Entry found but clip is NULL!");
                return;
            }

            // Use the entry's volume multiplier (0 = silent, 100 = normal, 200 = 2x)
            PlaySFXClip(entry.clip, position, entry.VolumeMultiplier, entry.pitch, entry.randomizePitch, entry.pitchVariation);
        }

        public void PlaySFXClip(AudioClip clip, Vector2? position = null, float volumeScale = 1f,
            float pitch = 1f, bool randomizePitch = false, float pitchVariation = 0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlaySFXClip - clip is NULL!");
                return;
            }

            // Android: Ensure clip is loaded (important for mobile!)
            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                Debug.Log($"[AudioManager] Loading clip: {clip.name}");
                clip.LoadAudioData();
            }

            // Skip if clip isn't ready yet
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"[AudioManager] Clip not ready: {clip.name}, state: {clip.loadState}");
                return;
            }

            // Get next available source from pool
            var source = GetNextSFXSource();

            // Reset source state (important for pooled sources)
            source.Stop();
            source.clip = clip;

            // Calculate final volume
            float finalVolume = _sfxVolume * _masterVolume * volumeScale;

            // Apply pitch - always reset to avoid leftover values from pool
            float finalPitch = pitch;
            if (randomizePitch && pitchVariation > 0f)
            {
                finalPitch = pitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            }
            source.pitch = Mathf.Clamp(finalPitch, 0.5f, 2f);

            // Position for 3D sound (if needed)
            if (position.HasValue)
            {
                source.transform.position = position.Value;
            }

            // Use PlayOneShot - more reliable on mobile
            source.volume = finalVolume;
            source.PlayOneShot(clip, finalVolume);
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

}
