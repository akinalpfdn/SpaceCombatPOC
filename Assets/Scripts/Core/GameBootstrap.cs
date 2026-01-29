using System.Diagnostics;
using UnityEngine;

namespace SpaceCombat.Core
{
    /// <summary>
    /// Bootstrap component that initializes all services
    /// Attach to a GameObject in the first scene
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _persistAcrossScenes = true;

        private static GameBootstrap _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeServices();
        }

        private void InitializeServices()
        {
            //Debug.Log("Game Bootstrap: Services initialized");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Clear();
                Events.EventBus.Clear();
            }
        }
    }
}