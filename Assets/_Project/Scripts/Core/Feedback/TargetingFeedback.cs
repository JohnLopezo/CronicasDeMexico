using UnityEngine;

namespace CronicasDeMexico.Core.Feedback
{
    /// <summary>
    /// Avance de la pulsación larga de Interactuar (acción de senda): lo fija PlayerInteractor y el HUD lo dibuja como un
    /// anillo que se llena. Negativo = no se está manteniendo.
    /// </summary>
    public static class HoldProgress
    {
        public static float Value { get; set; } = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Value = -1f;
    }

    /// <summary>Enemigo fijado como objetivo (PlayerAttack); el HUD le pone la mira encima. Null = ninguno.</summary>
    public static class LockOnMarker
    {
        public static Transform Target { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Target = null;
    }
}
