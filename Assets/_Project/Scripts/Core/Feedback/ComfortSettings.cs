using System;
using UnityEngine;

namespace CronicasDeMexico.Core.Feedback
{
    /// <summary>Velocidad a la que aparece el texto de los diálogos.</summary>
    public enum TextSpeed
    {
        Lento,
        Normal,
        Rapido,
        Instantaneo,
    }

    /// <summary>
    /// Opciones de comodidad y accesibilidad del dispositivo (no de la partida): se guardan en PlayerPrefs, como la
    /// calidad gráfica (QualityService), y se aplican al momento.
    /// </summary>
    public static class ComfortSettings
    {
        const string ShakeKey = "comodidad.sacudida";
        const string TextSpeedKey = "comodidad.velocidadTexto";
        const string DamageNumbersKey = "comodidad.numerosDano";

        static bool loaded;
        static bool screenShake;
        static bool damageNumbers;
        static TextSpeed textSpeed;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }

        static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            screenShake = PlayerPrefs.GetInt(ShakeKey, 1) != 0;
            damageNumbers = PlayerPrefs.GetInt(DamageNumbersKey, 1) != 0;
            textSpeed = (TextSpeed)Mathf.Clamp(PlayerPrefs.GetInt(TextSpeedKey, (int)TextSpeed.Normal), 0, (int)TextSpeed.Instantaneo);
        }

        /// <summary>Sacudida de cámara (algunas personas se marean con ella).</summary>
        public static bool ScreenShake
        {
            get { Load(); return screenShake; }
            set { Load(); screenShake = value; Save(ShakeKey, value ? 1 : 0); }
        }

        /// <summary>Números de daño sobre los personajes.</summary>
        public static bool DamageNumbers
        {
            get { Load(); return damageNumbers; }
            set { Load(); damageNumbers = value; Save(DamageNumbersKey, value ? 1 : 0); }
        }

        public static TextSpeed TextSpeed
        {
            get { Load(); return textSpeed; }
            set { Load(); textSpeed = value; Save(TextSpeedKey, (int)value); }
        }

        /// <summary>Letras por segundo de la velocidad elegida; 0 = todo de golpe.</summary>
        public static float CharactersPerSecond => TextSpeed switch
        {
            TextSpeed.Lento => 25f,
            TextSpeed.Normal => 50f,
            TextSpeed.Rapido => 100f,
            _ => 0f,
        };

        static void Save(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// El HUD avisa aquí que está escribiendo un mensaje letra a letra, y quien avanza los diálogos (PlayerInteractor)
    /// pide completarlo: la primera pulsación muestra el texto entero y la segunda pasa a la línea siguiente.
    /// </summary>
    public static class MessageReveal
    {
        public static bool IsRevealing { get; set; }

        /// <summary>Pide mostrar ya el mensaje entero.</summary>
        public static event Action CompleteRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsRevealing = false;
            CompleteRequested = null;
        }

        /// <summary>Completa el mensaje si se estaba escribiendo. Devuelve si lo hizo (la pulsación queda usada).</summary>
        public static bool TryComplete()
        {
            if (!IsRevealing)
                return false;
            CompleteRequested?.Invoke();
            IsRevealing = false;
            return true;
        }
    }
}
