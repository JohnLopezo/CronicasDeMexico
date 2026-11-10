using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CronicasDeMexico.Core.Travel
{
    /// <summary>Lo que el jugador lleva consigo al llegar a otra escena.</summary>
    public readonly struct TravelArrival
    {
        public readonly string SceneName;
        public readonly string SpawnId;
        public readonly int Health;

        public TravelArrival(string sceneName, string spawnId, int health)
        {
            SceneName = sceneName;
            SpawnId = spawnId;
            Health = health;
        }
    }

    /// <summary>
    /// Viaje entre escenas de región. Vive en Boot (lo registra GameBootstrapper): funde a negro, descarga la escena de
    /// origen, carga la de destino de forma aditiva, la activa y vuelve a fundir. La llegada queda pendiente hasta que
    /// la recoge el punto de aparición del destino (SpawnPoint, en Gameplay), que coloca al jugador y le devuelve la
    /// vida que traía: la vida no se recupera al cambiar de escena (Diseno.md §9).
    /// </summary>
    public sealed class SceneTravel : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Segundos de cada fundido (a negro y de vuelta).")]
        float fadeTime = 0.25f;

        TravelArrival? pending;
        float fade;
        Texture2D black;

        public bool IsTraveling { get; private set; }

        /// <summary>Al terminar un viaje, con el nombre de la escena de destino.</summary>
        public event Action<string> Arrived;

        void Awake() => enabled = false; // OnGUI solo dibuja durante los fundidos

        void OnDestroy()
        {
            if (black != null)
                Destroy(black);
        }

        /// <summary>
        /// Empieza el viaje desde la escena <paramref name="from"/> (la que se descarga) hasta <paramref name="sceneName"/>,
        /// que debe estar en Build Settings. Devuelve false si ya hay un viaje en curso.
        /// </summary>
        public bool Travel(Scene from, string sceneName, string spawnId, int health)
        {
            if (IsTraveling || string.IsNullOrEmpty(sceneName))
                return false;

            IsTraveling = true;
            pending = new TravelArrival(sceneName, spawnId, health);
            StartCoroutine(Run(from, sceneName));
            return true;
        }

        /// <summary>La recoge una sola vez el punto de aparición <paramref name="spawnId"/> de la escena de destino.</summary>
        public bool TryTakeArrival(string sceneName, string spawnId, out TravelArrival arrival)
        {
            if (pending is { } value && value.SceneName == sceneName && value.SpawnId == spawnId)
            {
                arrival = value;
                pending = null;
                return true;
            }

            arrival = default;
            return false;
        }

        IEnumerator Run(Scene from, string sceneName)
        {
            enabled = true;
            yield return Fade(0f, 1f);

            if (from.IsValid() && from.isLoaded)
                yield return SceneManager.UnloadSceneAsync(from);

            var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[Viaje] No se pudo cargar '{sceneName}'. ¿Está en Build Settings?");
                pending = null;
            }
            else
            {
                yield return load;
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
                if (pending.HasValue)
                    Debug.LogWarning($"[Viaje] '{sceneName}' no tiene el punto de aparición '{pending.Value.SpawnId}'.");
                pending = null;
            }

            yield return Fade(1f, 0f);
            enabled = false;
            IsTraveling = false;
            Arrived?.Invoke(sceneName);
        }

        IEnumerator Fade(float from, float to)
        {
            for (var t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                fade = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            fade = to;
        }

        void OnGUI()
        {
            if (fade <= 0f)
                return;
            if (black == null)
            {
                black = new Texture2D(1, 1);
                black.SetPixel(0, 0, Color.black);
                black.Apply();
            }

            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), black);
            GUI.color = previous;
        }
    }
}
