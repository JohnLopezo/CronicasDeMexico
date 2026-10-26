using System;
using UnityEngine;

namespace CronicasDeMexico.Core.Events
{
    /// <summary>
    /// Canal de eventos como asset: emisores y oyentes comparten la referencia al asset, no a los objetos.
    /// Los oyentes deben suscribirse en OnEnable y desuscribirse en OnDisable.
    /// </summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        event Action<T> Raised;

        public void Subscribe(Action<T> listener) => Raised += listener;
        public void Unsubscribe(Action<T> listener) => Raised -= listener;
        public void Raise(T value) => Raised?.Invoke(value);

        void OnDisable() => Raised = null;
    }
}
