using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CronicasDeMexico.Core.Boot
{
    /// <summary>
    /// Vive en la escena Boot, que permanece cargada toda la partida. Registra los servicios globales (calidad, viaje,
    /// historia, misiones, punto de control y guardado). Si no hay otra escena abierta, pide el menú de inicio
    /// (StartMenuRequested; lo muestra StartMenuController, en la misma escena), que llama a ContinueSavedGame o a
    /// StartNewGame.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField, Tooltip("Escena de juego que carga Nueva partida. Debe estar en Build Settings.")]
        string initialScene = "Sandbox_HD2D";

        /// <summary>Boot arrancó sola (el juego empieza aquí): el menú de inicio debe mostrarse.</summary>
        public static bool StartMenuRequested { get; private set; }

        public string InitialScene => initialScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => StartMenuRequested = false;

        void Awake()
        {
            RegisterServices();
            if (!TryGetComponent(out SceneTravel travel))
                travel = gameObject.AddComponent<SceneTravel>();
            ServiceLocator.Register(travel);

            // Si se pulsó Play desde una escena de juego, BootSceneLoader añadió Boot encima: no hay menú ni se carga
            // la partida guardada.
            StartMenuRequested = SceneManager.sceneCount == 1;
        }

        void OnDestroy() => ServiceLocator.Clear();

        // Igual que QuestCatalog.ResourcePath y los ResourcePath de Data/Rewards, que Core no puede referenciar.
        const string QuestCatalogResource = "Narrativa/CatalogoMisiones";
        const string ItemCatalogResource = "Recompensas/CatalogoObjetos";
        const string BlessingCatalogResource = "Recompensas/CatalogoBendiciones";
        const string ProgressionConfigResource = "Recompensas/ConfigProgresion";

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

            // Recompensas y progresión (M5). El inventario se restaura antes que la progresión, que suma sus reliquias.
            var inventory = new Inventory(Resources.Load<ScriptableObject>(ItemCatalogResource) as IItemCatalog);
            var progression = new Progression(Resources.Load<ScriptableObject>(ProgressionConfigResource) as IProgressionConfig,
                inventory, Resources.Load<ScriptableObject>(BlessingCatalogResource) as IBlessingCatalog);
            var rewards = new RewardService(inventory, progression, story, quests);
            save.Register(inventory);
            save.Register(progression);
            save.Register(rewards);
            ServiceLocator.Register(inventory);
            ServiceLocator.Register(progression);
            ServiceLocator.Register(rewards);
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

        /// <summary>
        /// Borra la partida guardada (y su copia) y carga la escena inicial de forma aditiva. Devuelve false si la
        /// escena no se puede cargar.
        /// shortcut: no reinicia los servicios en memoria, que siguen limpios salvo que antes fallara un Continuar
        /// tras leer la partida (partida sin punto de control); si eso llega a pasar, reiniciar los servicios aquí.
        /// </summary>
        public static bool StartNewGame(SaveService save, string scene)
        {
            save?.Delete();
            if (string.IsNullOrEmpty(scene))
                return false;

            var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[Boot] No se pudo cargar '{scene}'. ¿Está en Build Settings?");
                return false;
            }

            operation.completed += _ => SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            return true;
        }
    }
}
