using System;
using UnityEngine;

namespace CronicasDeMexico.Core.Feedback
{
    /// <summary>Lo que el HUD muestra del enemigo que el jugador está golpeando (sistema de Ruptura).</summary>
    public readonly struct BreakInfo
    {
        public readonly string Name;
        public readonly int Shield;
        public readonly int MaxShield;
        /// <summary>Nombres de las afinidades débiles, tal como se muestran ("Tierra", "Luz").</summary>
        public readonly string[] Weaknesses;
        public readonly bool Broken;

        public BreakInfo(string name, int shield, int maxShield, string[] weaknesses, bool broken)
        {
            Name = name;
            Shield = shield;
            MaxShield = maxShield;
            Weaknesses = weaknesses;
            Broken = broken;
        }
    }

    /// <summary>
    /// Enlace entre el enemigo golpeado (Gameplay) y el HUD (UI), que no se conocen. Muestra uno a la vez: el último
    /// que lo pidió; solo ese puede ocultarlo.
    /// </summary>
    public static class BreakReadout
    {
        static object owner;

        /// <summary>Lo que se muestra ahora; null si nada.</summary>
        public static BreakInfo? Current { get; private set; }

        public static event Action<BreakInfo?> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            owner = null;
            Current = null;
            Changed = null;
        }

        public static void Show(object source, BreakInfo info)
        {
            owner = source;
            Current = info;
            Changed?.Invoke(info);
        }

        public static void Hide(object source)
        {
            if (owner != source)
                return;
            owner = null;
            Current = null;
            Changed?.Invoke(null);
        }

        public static bool IsShowing(object source) => owner == source;
    }
}
