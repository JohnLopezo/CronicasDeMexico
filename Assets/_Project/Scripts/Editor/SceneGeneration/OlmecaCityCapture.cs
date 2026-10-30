using System;
using System.IO;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Capturas a 1920×1080 de la cámara real de Olmeca_Ciudad, Olmeca_Selva y los pisos de la mazmorra en varios
    /// puntos, para revisar el resultado en Unity sin abrir el Editor (orden de dibujo, alineación de píxeles). Desde
    /// consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.OlmecaCityCapture.Capture -capturePath &lt;carpeta&gt;
    /// (CaptureJungle para la selva, CaptureDungeon para los pisos). Sin -capturePath se guardan en Logs/Capturas.
    /// </summary>
    public static class OlmecaCityCapture
    {
        const int Width = 1920;
        const int Height = 1080;

        // Puntos que mira la cámara, en coordenadas del flujo de arte (x, y): los mismos recortes que la vista previa.
        static readonly (string name, Vector2 focus)[] Views =
        {
            ("Plaza", new Vector2(0f, 5f)),
            ("Monticulo", new Vector2(0f, 16f)),
            ("Recinto", new Vector2(0f, -9f)),
            ("Aldea", new Vector2(15f, -2f)),
            ("Muelle", new Vector2(20f, -4f)),
            // Habitantes del asentamiento (OlmecaCityGenerator.Settlers).
            ("Artesano", new Vector2(13.5f, 3.5f)),
            ("Anciana", new Vector2(12.4f, 15f)),
            ("Guerrero", new Vector2(11f, 9.5f)),
            ("Despertar", new Vector2(18f, 22.5f)), // orilla del río: jugador, figura espiritual y reflejo
            ("SenderoOeste", new Vector2(-19f, 4f)), // salida hacia la selva (MP_OLM_03)
        };

        // Selva (planificar_selva): llegada desde la ciudad, los tres claros y la entrada a la estructura.
        static readonly (string name, Vector2 focus)[] JungleViews =
        {
            ("Llegada", new Vector2(15f, 0f)),
            ("Claro1", new Vector2(11f, 1.6f)),
            ("Claro2", new Vector2(2f, 0.5f)),
            ("Claro3", new Vector2(-6.5f, -3f)),
            ("Entrada", new Vector2(-11f, -2f)),
        };

        [MenuItem("Crónicas/Capturar ciudad olmeca")]
        public static void Capture() => CaptureScene(OlmecaCityGenerator.ScenePath, "Ciudad", Views);

        [MenuItem("Crónicas/Capturar selva olmeca")]
        public static void CaptureJungle() => CaptureScene(OlmecaJungleGenerator.ScenePath, "Selva", JungleViews);

        // Pisos de la mazmorra (PISOS en generar_olmeca.py): llegada y santuarios, salas, arena y escalera de bajada.
        static readonly (string name, Vector2 focus)[][] DungeonViews =
        {
            new[]
            {
                ("Llegada", new Vector2(-11.5f, -1f)), ("Sala", new Vector2(-1.5f, 0f)), ("Grupo", new Vector2(8f, 0f)),
                ("Bajada", new Vector2(15.5f, 0f)),
            },
            new[]
            {
                ("Llegada", new Vector2(-11.5f, 1f)), ("Sala", new Vector2(0f, 0.5f)), ("Vida", new Vector2(0f, -7f)),
                ("Arena", new Vector2(12f, 1f)),
            },
            new[] { ("Llegada", new Vector2(-7f, 0f)), ("Antesala", new Vector2(3.5f, 0.5f)), ("Arena", new Vector2(5f, 12f)) },
        };

        [MenuItem("Crónicas/Capturar mazmorra olmeca")]
        public static void CaptureDungeon()
        {
            for (var floor = 1; floor <= OlmecaDungeonGenerator.FloorCount; floor++)
                CaptureScene(OlmecaDungeonGenerator.ScenePath(floor), $"Mazmorra_P{floor}", DungeonViews[floor - 1]);
        }

        static void CaptureScene(string scenePath, string prefix, (string name, Vector2 focus)[] views)
        {
            var folder = CommandLineValue("-capturePath") ?? Path.Combine("Logs", "Capturas");
            Directory.CreateDirectory(folder);

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            // Lo que en el juego aparece después (los fragmentos, al vencer a su espíritu) se muestra para revisarlo.
            // La escena no se guarda.
            foreach (var fragment in UnityEngine.Object.FindObjectsByType<MemoryFragmentInteractable>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                fragment.transform.parent.gameObject.SetActive(true);
            var camera = UnityEngine.Object.FindAnyObjectByType<IsometricCameraFollow>().GetComponent<Camera>();
            camera.orthographicSize = IsometricProjection.OrthographicSize(Height);
            var pixel = 1f / (IsometricProjection.PixelsPerUnit * IsometricProjection.IntegerScale(Height));

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                foreach (var (name, focus) in views)
                {
                    var point = IsometricProjection.FromPipeline(focus.x, focus.y, 0f);
                    camera.transform.position = IsometricProjection.SnapToPixelGrid(point - camera.transform.forward * 60f, pixel);
                    camera.Render();

                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    image.Apply();
                    var path = Path.Combine(folder, $"{prefix}_Unity_{name}.png");
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    Debug.Log($"[Captura] {path}");
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static string CommandLineValue(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, flag);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
