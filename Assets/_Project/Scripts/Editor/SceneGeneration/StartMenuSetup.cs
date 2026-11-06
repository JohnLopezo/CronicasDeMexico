using CronicasDeMexico.UI.Hud;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Añade (o rehace) el menú de inicio en la escena Boot: un UIDocument con StartMenuController y, como hijos
    /// inactivos, la cámara de fondo y el EventSystem del menú. El resto de Boot (GameBootstrapper) no se toca.
    /// Desde consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.StartMenuSetup.Setup
    /// </summary>
    public static class StartMenuSetup
    {
        const string BootScenePath = "Assets/_Project/Scenes/Boot/Boot.unity";
        const string PanelSettingsPath = "Assets/_Project/Settings/UI/PanelSettings_Hud.asset";
        const string StyleSheetPath = "Assets/_Project/Art/UI/Hud/StartMenu.uss";
        const string MenuName = "Menu_Inicio";
        static readonly Color Background = new(0.075f, 0.141f, 0.102f); // #13241A, como el fondo del menú

        [MenuItem("Crónicas/Crear menú de inicio en Boot")]
        static void SetupFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                Setup();
        }

        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            // Después de OpenScene, que descarga lo que se cargó antes y dejaría las referencias vacías en la escena.
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath)
                ?? throw new System.InvalidOperationException($"No se encontró {PanelSettingsPath}.");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath)
                ?? throw new System.InvalidOperationException($"No se encontró {StyleSheetPath}.");
            foreach (var old in scene.GetRootGameObjects())
                if (old.name == MenuName)
                    Object.DestroyImmediate(old);

            var menu = new GameObject(MenuName);
            var document = menu.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = 100; // por encima del HUD, que comparte PanelSettings

            var cameraObject = new GameObject("Camara_Fondo");
            cameraObject.transform.SetParent(menu.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.cullingMask = 0;
            camera.depth = -100;
            cameraObject.SetActive(false);

            var eventSystem = OlmecaPrototypeGenerator.CreateEventSystem();
            eventSystem.transform.SetParent(menu.transform, false);
            eventSystem.SetActive(false);

            var controller = menu.AddComponent<StartMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("styleSheet").objectReferenceValue = styleSheet;
            serialized.FindProperty("backgroundCamera").objectReferenceValue = camera;
            serialized.FindProperty("eventSystem").objectReferenceValue = eventSystem;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Generador] Menú de inicio creado en {BootScenePath}.");
        }
    }
}
