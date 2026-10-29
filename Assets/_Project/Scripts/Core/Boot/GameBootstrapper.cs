using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CronicasDeMexico.Core.Boot
{
    /// <summary>
    /// Vive en la escena Boot, que permanece cargada toda la partida. Registra los servicios globales (calidad,
    /// historia, misiones y guardado) y, si no hay otra escena abierta, carga la escena inicial de forma aditiva.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField, Tooltip("Escena de juego que se carga al arrancar desde Boot. Debe estar en Build Settings.")]
        string initialScene = "Sandbox_HD2D";

        void Awake()
        {
            RegisterServices();

            // Si se pulsó Play desde una escena de juego, BootSceneLoader añadió Boot encima: no cargamos nada más.
            if (SceneManager.sceneCount == 1)
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

            // La partida no se carga sola: lo hará el menú o un santuario de Memoria cuando existan.
            var story = new StoryState();
            var catalog = Resources.Load<ScriptableObject>(QuestCatalogResource) as IQuestCatalog;
            var quests = new QuestService(story, catalog);
            var save = new SaveService(Application.persistentDataPath);
            save.Register(story); // antes que las misiones, que confían en que las banderas ya estén
            save.Register(quests);
            ServiceLocator.Register(story);
            ServiceLocator.Register(quests);
            ServiceLocator.Register(save);
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
