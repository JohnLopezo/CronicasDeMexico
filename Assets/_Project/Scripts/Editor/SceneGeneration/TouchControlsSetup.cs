using CronicasDeMexico.UI.Touch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Controles táctiles (TouchControls) como prefab y en la escena Boot, que dura toda la partida. Distribución aprobada
    /// por el autor: joystick abajo a la izquierda; abajo a la derecha, Atacar grande con Esquivar, Defender y Especial
    /// alrededor, Golpe de tierra e Interactuar encima y Usar (consumible) más arriba; Menú arriba a la derecha. Cada botón es un OnScreenButton con el
    /// control del mando que el juego ya entiende. Menú Crónicas → Crear controles táctiles en Boot.
    /// </summary>
    public static class TouchControlsSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/Controles_Tactiles.prefab";
        const string BootScenePath = "Assets/_Project/Scenes/Boot/Boot.unity";
        const string RootName = "Controles_Tactiles";
        static readonly Color Fill = new(0.08f, 0.12f, 0.1f, 0.45f);
        static readonly Color Ring = new(0.31f, 0.7f, 0.6f, 0.75f); // jade, el acento del HUD

        // Nombre, texto, control del mando, posición desde la esquina inferior derecha (px de 1920×1080) y diámetro.
        static readonly (string name, string label, string control, Vector2 position, float size)[] Buttons =
        {
            ("Atacar", "Atacar", "<Gamepad>/buttonWest", new(-230f, 230f), 190f),
            ("Esquivar", "Esquivar", "<Gamepad>/buttonSouth", new(-90f, 100f), 120f),
            ("Defender", "Defender", "<Gamepad>/leftShoulder", new(-440f, 170f), 120f),
            ("Especial", "Especial", "<Gamepad>/rightShoulder", new(-410f, 340f), 120f),
            ("Golpe_Tierra", "Tierra", "<Gamepad>/buttonEast", new(-250f, 450f), 110f),
            ("Interactuar", "Hablar", "<Gamepad>/buttonNorth", new(-90f, 330f), 110f),
            ("Usar", "Usar", "<Gamepad>/dpad/left", new(-90f, 510f), 100f), // el consumible más útil (acción UseItem)
        };

        [MenuItem("Crónicas/Crear controles táctiles en Boot")]
        public static void Setup()
        {
            OlmecaPrototypeGenerator.EnsureFolder("Assets/_Project/Prefabs/UI");
            var built = Build();
            var prefab = PrefabUtility.SaveAsPrefabAsset(built, PrefabPath, out _);
            Object.DestroyImmediate(built);

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == RootName)
                    Object.DestroyImmediate(root);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = RootName;
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Generador] Controles táctiles en {BootScenePath} ({PrefabPath}).");
        }

        static GameObject Build()
        {
            var root = new GameObject(RootName, typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // por encima de otros lienzos de uGUI
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var controls = Rect("Controles", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var safe = Rect("Zona_Segura", controls.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.AddComponent<SafeArea>();
            controls.SetActive(false);

            // Joystick: el aro fijo y la palanca que se arrastra (OnScreenStick mueve el stick izquierdo del mando).
            var stickBase = Circle("Joystick", safe.transform, new Vector2(0f, 0f), new Vector2(270f, 260f), 300f, Fill, true);
            var knob = Circle("Palanca", stickBase.transform, new Vector2(0.5f, 0.5f), Vector2.zero, 140f, Ring, false);
            var stick = knob.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 110f;

            foreach (var (name, label, control, position, size) in Buttons)
                AddButton(name, label, control, safe.transform, new Vector2(1f, 0f), position, size);
            AddButton("Menu", "Menú", "<Gamepad>/select", safe.transform, new Vector2(1f, 1f), new Vector2(-90f, -80f), 100f);

            var touch = root.AddComponent<TouchControls>();
            SetProperty(touch, "controls", property => property.objectReferenceValue = controls);
            return root;
        }

        static void AddButton(string name, string label, string control, Transform parent, Vector2 anchor, Vector2 position, float size)
        {
            var button = Circle(name, parent, anchor, position, size, Fill, true);
            button.AddComponent<OnScreenButton>().controlPath = control;
            var text = Rect("Texto", button.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(size * 0.2f);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false; // el toque lo recibe el círculo
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        }

        static GameObject Circle(string name, Transform parent, Vector2 anchor, Vector2 position, float size, Color color, bool ring)
        {
            var circle = Rect(name, parent, anchor, anchor, position, new Vector2(size, size));
            var image = circle.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = color;
            if (ring)
                circle.AddComponent<Outline>().effectColor = Ring;
            return circle;
        }

        static GameObject Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return go;
        }
    }
}
