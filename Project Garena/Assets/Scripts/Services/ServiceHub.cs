using System;
using System.Collections.Generic;
using UnityEngine;

namespace Template.Core
{
    public class ServiceHub : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] services;

        private static ServiceHub instance;
        private readonly Dictionary<Type, IGameService> registry = new Dictionary<Type, IGameService>();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterServices();
        }

        void OnDestroy()
        {
            if (instance != this) return;
            foreach (var s in registry.Values)
            {
                s.Dispose();
            }
            registry.Clear();
            instance = null;
        }

        private void RegisterServices()
        {
            registry.Clear();
            if (services == null) return;

            foreach (var mb in services)
            {
                if (mb is not IGameService svc) continue;
                registry[mb.GetType()] = svc;
                svc.Initialize();
            }
        }

        public static T Get<T>() where T : class, IGameService
        {
            if (instance == null) return null;
            return instance.registry.TryGetValue(typeof(T), out var svc) ? svc as T : null;
        }
    }
}
