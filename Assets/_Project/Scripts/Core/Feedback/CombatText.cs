using System;
using UnityEngine;

namespace CronicasDeMexico.Core.Feedback
{
    public enum DamageKind
    {
        /// <summary>Daño a un enemigo.</summary>
        Dealt,
        /// <summary>Daño a un enemigo roto (ruptura): más grande y en otro color.</summary>
        Broken,
        /// <summary>Daño al jugador.</summary>
        Taken,
    }

    /// <summary>
    /// Números de daño: Health los informa (Gameplay) y el HUD los dibuja (UI). Posición en el mundo, sobre la
    /// cabeza; el HUD la pasa a la pantalla.
    /// </summary>
    public static class DamageNumbers
    {
        public static event Action<Vector3, int, DamageKind> Reported;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reported = null;

        public static void Report(Vector3 worldPosition, int amount, DamageKind kind)
        {
            if (amount > 0 && ComfortSettings.DamageNumbers)
                Reported?.Invoke(worldPosition, amount, kind);
        }
    }

    /// <summary>
    /// Retrato de quien habla en el mensaje que se va a mostrar. DialogueInteractable lo fija justo antes de publicar
    /// la línea; el HUD lo usa si el nombre coincide con el del mensaje y lo olvida con el mensaje.
    /// </summary>
    public static class SpeakerPortrait
    {
        public static string Name { get; private set; }
        public static Sprite Sprite { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();

        public static void Set(string speakerName, Sprite sprite)
        {
            Name = speakerName;
            Sprite = sprite;
        }

        public static void Clear()
        {
            Name = null;
            Sprite = null;
        }
    }
}
