using VContainer;
using VContainer.Unity;
using UnityEngine;
using SpaceCombat.Interfaces;
using SpaceCombat.Utilities;
using SpaceCombat.Audio;
using SpaceCombat.VFX;
using SpaceCombat.Spawning;
using SpaceCombat.Input;

namespace SpaceCombat.Core
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

            // Input - platform based
            if (Application.isMobilePlatform && _touchInput != null)
            {
                builder.RegisterComponent(_touchInput).As<IInputProvider>();
            }
            else if (_keyboardInput != null)
            {
                builder.RegisterComponent(_keyboardInput).As<IInputProvider>();
            }
        }

        protected override void OnDestroy()
        {
            Events.EventBus.Clear();
            base.OnDestroy();
        }
    }
}
