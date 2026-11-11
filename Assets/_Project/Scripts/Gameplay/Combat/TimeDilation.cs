using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Ralentiza el tiempo un instante (hitstop, esquiva y bloqueo perfectos) y lo devuelve a 1 al acabar, en tiempo
    /// real. Nunca usa 0, que en este juego significa pausa (menús, controles táctiles ocultos). Si un menú cambió la
    /// escala entretanto, no la toca; si el plazo vence con el juego en pausa, espera a que se reanude, porque el
    /// menú devuelve la escala que tenía al abrirse (la ralentizada).
    /// </summary>
    public sealed class TimeDilation : MonoBehaviour
    {
        static TimeDilation instance;

        float scale = 1f;
        float until;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        /// <summary>Hay una ralentización en curso.</summary>
        public static bool IsActive => instance != null && instance.until > 0f;

        /// <summary>Pide la escala <paramref name="timeScale"/> (0 &lt; x &lt; 1) durante <paramref name="seconds"/> reales.
        /// Si ya hay una, se queda la más lenta y el plazo más largo. Devuelve si se aplicó.</summary>
        public static bool Request(float timeScale, float seconds)
        {
            if (seconds <= 0f || timeScale <= 0f || timeScale >= 1f || Time.timeScale == 0f)
                return false;

            if (instance == null)
            {
                var host = new GameObject(nameof(TimeDilation)) { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(host);
                instance = host.AddComponent<TimeDilation>();
            }

            var active = instance.until > 0f;
            // Solo desde la escala normal o desde la nuestra: otra escala es de alguien más.
            if (!Mathf.Approximately(Time.timeScale, active ? instance.scale : 1f))
                return false;

            instance.scale = active ? Mathf.Min(instance.scale, timeScale) : timeScale;
            instance.until = Mathf.Max(instance.until, Time.unscaledTime + seconds);
            Time.timeScale = instance.scale;
            return true;
        }

        void Update()
        {
            if (until <= 0f || Time.unscaledTime < until || Time.timeScale == 0f)
                return;
            Restore();
        }

        void OnDestroy()
        {
            if (until > 0f && Time.timeScale != 0f)
                Restore();
            if (instance == this)
                instance = null;
        }

        void Restore()
        {
            if (Mathf.Approximately(Time.timeScale, scale))
                Time.timeScale = 1f;
            until = 0f;
            scale = 1f;
        }
    }
}
