using System;
using UnityEngine;

namespace CronicasDeMexico.Core.Events
{
    /// <summary>
    /// Canal de eventos sin datos (p. ej. "JugadorMurió", "PausaSolicitada").
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Eventos/Evento sin datos", fileName = "EVT_Nuevo")]
    public sealed class VoidEventChannel : ScriptableObject
    {
        event Action Raised;

        public void Subscribe(Action listener) => Raised += listener;
        public void Unsubscribe(Action listener) => Raised -= listener;
        public void Raise() => Raised?.Invoke();

        void OnDisable() => Raised = null;
    }
}
