using UnityEngine;

namespace CronicasDeMexico.Core.Events
{
    /// <summary>Vida actual y máxima de un personaje.</summary>
    public readonly struct HealthChange
    {
        public readonly int Current;
        public readonly int Max;

        public HealthChange(int current, int max)
        {
            Current = current;
            Max = max;
        }
    }

    /// <summary>
    /// Canal de eventos con cambios de vida (p. ej. la vida del jugador hacia el HUD).
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Eventos/Evento de vida", fileName = "EVT_Nuevo")]
    public sealed class HealthEventChannel : EventChannel<HealthChange>
    {
    }
}
