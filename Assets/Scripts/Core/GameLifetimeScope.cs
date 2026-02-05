using VContainer;
using VContainer.Unity;
using UnityEngine;
using StarReapers.Interfaces;
using StarReapers.Utilities;
using StarReapers.Audio;
using StarReapers.VFX;
using StarReapers.Spawning;
using StarReapers.Input;

namespace StarReapers.Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("Core")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private PoolManager _poolManager;

        [Header("Services")]
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private VFXManager _vfxManager;
        [SerializeField] private EnemySpawnService _spawnService;

        [Header("Input")]
        [SerializeField] private KeyboardInputProvider _keyboardInput;
        [SerializeField] private TouchInputProvider _touchInput;

        protected override void Configure(IContainerBuilder builder)
        {
            // Core
            builder.RegisterComponent(_gameManager);
            builder.RegisterComponent(_poolManager);

            // Services
            builder.RegisterComponent(_vfxManager);
            builder.RegisterComponent(_spawnService).As<ISpawnService>();
            builder.RegisterComponent(_audioManager).As<IAudioService>();

            // Input - platform based with auto-find fallback
            RegisterInputProvider(builder);
        }

        private void RegisterInputProvider(IContainerBuilder builder)
        {
            // Try to find input providers if not assigned in inspector
            if (_keyboardInput == null)
                _keyboardInput = FindFirstObjectByType<KeyboardInputProvider>();
            if (_touchInput == null)
                _touchInput = FindFirstObjectByType<TouchInputProvider>();

            // Register based on platform
            if (Application.isMobilePlatform && _touchInput != null)
            {
                builder.RegisterComponent(_touchInput).As<IInputProvider>();
            }
            else if (_keyboardInput != null)
            {
                builder.RegisterComponent(_keyboardInput).As<IInputProvider>();
            }
            else
            {
                Debug.LogError("[GameLifetimeScope] No IInputProvider found! Assign KeyboardInputProvider or TouchInputProvider in inspector.");
            }
        }

        protected override void OnDestroy()
        {
            Events.EventBus.Clear();
            base.OnDestroy();
        }
    }
}
