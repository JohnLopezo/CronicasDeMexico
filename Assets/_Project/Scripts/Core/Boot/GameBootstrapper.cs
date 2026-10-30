using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CronicasDeMexico.Core.Boot
{
    /// <summary>
    /// Vive en la escena Boot, que permanece cargada toda la partida. Registra los servicios globales (calidad, viaje,
    /// historia, misiones, punto de control y guardado) y, si no hay otra escena abierta, continúa la partida guardada
    /// en su santuario de Memoria o, si no la hay, carga la escena inicial de forma aditiva. Hasta que exista el menú de
    /// inicio, la partida se borra con Crónicas → Borrar partida guardada.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField, Tooltip("Escena de juego que se carga al arrancar desde Boot. Debe estar en Build Settings.")]
        string initialScene = "Sandbox_HD2D";

        void Awake()
        {
            RegisterServices();
            if (!TryGetComponent(out SceneTravel travel))
                travel = gameObject.AddComponent<SceneTravel>();
            ServiceLocator.Register(travel);

            // Si se pulsó Play desde una escena de juego, BootSceneLoader añadió Boot encima: no cargamos nada más (ni
            // la partida guardada).
            if (SceneManager.sceneCount == 1 &&
                !ContinueSavedGame(ServiceLocator.Get<SaveService>(), ServiceLocator.Get<PlayerCheckpoint>(), travel))
                LoadInitialScene();
        }

        void OnDestroy() => ServiceLocator.Clear();

        // Igual que QuestCatalog.ResourcePath (Data), que Core no puede referenciar.
        const string QuestCatalogResource = "Narrativa/CatalogoMisiones";

        static void RegisterServices()
        {
            var quality = new QualityService();
            quality.ApplySaved();
            ServiceLocator.Register(quality);

            // Guardan los santuarios de Memoria; se carga al arrancar desde Boot (ContinueSavedGame).
            var story = new StoryState();
            var catalog = Resources.Load<ScriptableObject>(QuestCatalogResource) as IQuestCatalog;
            var quests = new QuestService(story, catalog);
            var checkpoint = new PlayerCheckpoint();
            var save = new SaveService(Application.persistentDataPath);
            save.Register(story); // antes que las misiones, que confían en que las banderas ya estén
            save.Register(quests);
            save.Register(checkpoint);
            ServiceLocator.Register(story);
            ServiceLocator.Register(quests);
            ServiceLocator.Register(checkpoint);
            ServiceLocator.Register(save);
        }

        /// <summary>
        /// Carga la partida y viaja a su santuario de Memoria con la vida que tenía al descansar. Devuelve false, sin
        /// viajar, si no hay partida legible o no tiene punto de control.
        /// </summary>
        public static bool ContinueSavedGame(SaveService save, PlayerCheckpoint checkpoint, SceneTravel travel)
        {
            if (save == null || checkpoint == null || travel == null || !save.HasSave || !save.Load() || !checkpoint.HasCheckpoint)
                return false;

            Debug.Log($"[Boot] Continuando la partida en {checkpoint.Scene} ({checkpoint.SpawnId}).");
            return travel.Travel(default, checkpoint.Scene, checkpoint.SpawnId, checkpoint.Health);
        }

        void LoadInitialScene()
        {
            if (string.IsNullOrEmpty(initialScene))
                return;

            var operation = SceneManager.LoadSceneAsync(initialScene, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[Boot] No se pudo cargar '{initialScene}'. ¿Está en Build Settings?");
                return;
            }

            operation.completed += _ => SceneManager.SetActiveScene(SceneManager.GetSceneByName(initialScene));
        }
    }
}
