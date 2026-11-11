using UnityEngine;

namespace CronicasDeMexico.Core.Feedback
{
    /// <summary>
    /// Sacudida de cámara: quien golpea o recibe un golpe la pide (Gameplay) y la cámara la aplica (World), sin que
    /// esos assemblies se conozcan. Una petición más débil que la que está en curso no la acorta. Usa tiempo real
    /// para seguir durante el hitstop.
    /// </summary>
    public static class ScreenShake
    {
        static float amplitude;
        static float duration;
        static float endTime;

        /// <summary>Apagada desde opciones (accesibilidad).</summary>
        public static bool Enabled { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            amplitude = duration = endTime = 0f;
            Enabled = true;
        }

        /// <summary>Pide una sacudida de <paramref name="metres"/> que se apaga en <paramref name="seconds"/>.</summary>
        public static void Request(float metres, float seconds)
        {
            if (!Enabled || metres <= 0f || seconds <= 0f || metres < CurrentAmplitude)
                return;
            amplitude = metres;
            duration = seconds;
            endTime = Time.unscaledTime + seconds;
        }

        /// <summary>Amplitud de este momento, que baja lineal hasta 0.</summary>
        public static float CurrentAmplitude
        {
            get
            {
                var remaining = endTime - Time.unscaledTime;
                return remaining > 0f && duration > 0f ? amplitude * (remaining / duration) : 0f;
            }
        }

        /// <summary>Desplazamiento en el plano de la cámara (x: derecha, y: arriba) para este cuadro.</summary>
        public static Vector2 Offset()
        {
            var current = CurrentAmplitude;
            return current > 0f ? Random.insideUnitCircle * current : Vector2.zero;
        }
    }
}
