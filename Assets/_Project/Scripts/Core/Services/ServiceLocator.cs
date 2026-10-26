using System;
using System.Collections.Generic;
using UnityEngine;

namespace CronicasDeMexico.Core.Services
{
    /// <summary>
    /// Registro global de servicios (audio, guardado, calidad...). Los registra GameBootstrapper al arrancar.
    /// </summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> Services = new();

        // La recarga de dominio está desactivada (Enter Play Mode Options): los estáticos se limpian a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Services.Clear();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            if (Services.ContainsKey(typeof(T)))
                Debug.LogWarning($"[ServiceLocator] Reemplazando el servicio {typeof(T).Name}.");

            Services[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class => Services.Remove(typeof(T));

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service))
                return (T)service;

            throw new InvalidOperationException($"[ServiceLocator] El servicio {typeof(T).Name} no está registrado.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        public static void Clear() => Services.Clear();
    }
}
