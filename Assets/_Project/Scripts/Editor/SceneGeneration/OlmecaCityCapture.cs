using System;
using System.IO;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Capturas a 1920×1080 de la cámara real de Olmeca_Ciudad en varios puntos, para revisar el resultado en Unity
    /// sin abrir el Editor (orden de dibujo, alineación de píxeles). Desde consola:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.OlmecaCityCapture.Capture -capturePath &lt;carpeta&gt;
    /// Sin -capturePath se guardan en Logs/Capturas.
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
        };

        [MenuItem("Crónicas/Capturar ciudad olmeca")]
        public static void Capture()
        {
            var folder = CommandLineValue("-capturePath") ?? Path.Combine("Logs", "Capturas");
            Directory.CreateDirectory(folder);

            EditorSceneManager.OpenScene(OlmecaCityGenerator.ScenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindAnyObjectByType<IsometricCameraFollow>().GetComponent<Camera>();
            camera.orthographicSize = IsometricProjection.OrthographicSize(Height);
            var pixel = 1f / (IsometricProjection.PixelsPerUnit * IsometricProjection.IntegerScale(Height));

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                foreach (var (name, focus) in Views)
                {
                    var point = IsometricProjection.FromPipeline(focus.x, focus.y, 0f);
                    camera.transform.position = IsometricProjection.SnapToPixelGrid(point - camera.transform.forward * 60f, pixel);
                    camera.Render();

                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    image.Apply();
                    var path = Path.Combine(folder, $"Ciudad_Unity_{name}.png");
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
