using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using CronicasDeMexico.World.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CronicasDeMexico.World.Diagnostics
{
    /// <summary>
    /// Medición automática de tiempos de frame en un build (Docs/Rendimiento.md). Solo se activa con el argumento
    /// -benchmark: recorre con la cámara varios puntos de la escena en cada nivel de calidad, sin V-Sync ni límite de
    /// FPS para ver el margen real, guarda un resumen, el CSV de frames y una captura por punto, y cierra el juego.
    /// Ejemplo: CronicasDeMexico.exe -benchmark -benchmarkOutput C:\ruta -screen-width 1920 -screen-height 1080
    /// </summary>
    public sealed class FrameTimeBenchmark : MonoBehaviour
    {
        const float WarmUpSeconds = 1.5f;
        const float SampleSeconds = 6f;
        const float OrbitRadius = 2f; // la cámara se mueve durante la muestra: incluye el coste del scroll
        const int MaxSamples = 60000;

        // Puntos en coordenadas del flujo de arte (x, y), como las capturas de OlmecaCityCapture.
        static readonly (string name, Vector2 point)[] Views =
        {
            ("Plaza", new Vector2(0f, 5f)),
            ("Monticulo", new Vector2(0f, 16f)),
            ("Recinto", new Vector2(0f, -9f)),
            ("Aldea", new Vector2(15f, -2f)),
            ("Selva", new Vector2(-14f, -6f)),
        };

        // En Olmeca_Selva (planificar_selva): llegada, un claro, la entrada y selva densa.
        const string JungleSceneName = "Olmeca_Selva";
        static readonly (string name, Vector2 point)[] JungleViews =
        {
            ("Llegada", new Vector2(15f, 0f)),
            ("Claro", new Vector2(2f, 0.5f)),
            ("Entrada", new Vector2(-11f, -2f)),
            ("SelvaDensa", new Vector2(6f, -6f)),
        };

        // En Olmeca_Mazmorra_P2 (planificar_piso, el piso más cargado: ecos, agua y el Jaguar de Piedra): llegada, sala
        // inundada, santuario de Vida y arena.
        const string DungeonSceneName = "Olmeca_Mazmorra_P2";
        static readonly (string name, Vector2 point)[] DungeonViews =
        {
            ("Llegada", new Vector2(-11.5f, 1f)),
            ("Sala", new Vector2(0f, 0.5f)),
            ("Vida", new Vector2(0f, -7f)),
            ("Arena", new Vector2(12f, 0f)),
        };

        // En Olmeca_Mazmorra_P3: la antesala y la arena del Guardián de la Piedra Viva (centro y borde).
        const string ArenaSceneName = "Olmeca_Mazmorra_P3";
        static readonly (string name, Vector2 point)[] ArenaViews =
        {
            ("Antesala", new Vector2(3.5f, 0.5f)),
            ("Arena", new Vector2(5f, 12f)),
            ("BordeArena", new Vector2(8f, 16f)),
        };

        readonly float[] samples = new float[MaxSamples];
        string outputFolder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-benchmark") < 0)
                return;

            // Sin esto el juego se pausa si la ventana pierde el foco y la medición queda con frames de minutos.
            Application.runInBackground = true;

            var benchmark = new GameObject("FrameTimeBenchmark").AddComponent<FrameTimeBenchmark>();
            var index = Array.IndexOf(args, "-benchmarkOutput");
            benchmark.outputFolder = index >= 0 && index + 1 < args.Length
                ? args[index + 1]
                : Path.Combine(Application.persistentDataPath, "Benchmark");
            DontDestroyOnLoad(benchmark.gameObject);
        }

        IEnumerator Start()
        {
            Application.wantsToQuit += LogQuitRequest;
            Directory.CreateDirectory(outputFolder);
            yield return null; // la escena termina de iniciarse

            var follow = FindAnyObjectByType<IsometricCameraFollow>();
            if (follow == null)
            {
                Debug.LogError("[Benchmark] La escena no tiene IsometricCameraFollow.");
                Application.Quit(1);
                yield break;
            }

            // -sinPantallaPixel: la cámara dibuja directamente en pantalla, para comparar con PixelArtScreen.
            var screen = follow.GetComponent<PixelArtScreen>();
            var lowResolution = screen != null && Array.IndexOf(Environment.GetCommandLineArgs(), "-sinPantallaPixel") < 0;
            if (screen != null)
                screen.enabled = lowResolution;

            var probe = new GameObject("BenchmarkProbe").transform;
            follow.Target = probe;

            var summary = new StringBuilder();
            summary.AppendLine($"Equipo: {SystemInfo.processorType} | {SystemInfo.graphicsDeviceName} | {SystemInfo.systemMemorySize} MB");
            summary.AppendLine($"Pantalla: {Screen.width}x{Screen.height} | Unity {Application.unityVersion} | build de desarrollo: {Debug.isDebugBuild}");
            summary.AppendLine(lowResolution
                ? $"Presentación: PixelArtScreen ({screen.Target.width}x{screen.Target.height} ×{screen.Scale})"
                : "Presentación: cámara directa a pantalla (Render Scale del nivel de calidad)");
            summary.AppendLine("Calidad;Punto;Frames;FPS medio;FPS 1% bajo;ms medio;ms p95;ms máx");
            var csv = new StringBuilder("calidad;punto;frame;ms\n");

            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                var levelName = QualitySettings.names[level];

                var views = SceneManager.GetActiveScene().name switch
                {
                    JungleSceneName => JungleViews,
                    DungeonSceneName => DungeonViews,
                    ArenaSceneName => ArenaViews,
                    _ => Views,
                };
                foreach (var (viewName, point) in views)
                {
                    Debug.Log($"[Benchmark] {levelName} / {viewName}");
                    var center = IsometricProjection.FromPipeline(point.x, point.y, 0f);
                    probe.position = center;
                    for (var t = 0f; t < WarmUpSeconds; t += Time.unscaledDeltaTime)
                        yield return null;

                    if (level == 0)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(outputFolder, $"Build_{viewName}.png"));
                        yield return null;
                    }

                    var count = 0;
                    for (var t = 0f; t < SampleSeconds && count < MaxSamples; t += Time.unscaledDeltaTime)
                    {
                        var angle = t / SampleSeconds * Mathf.PI * 2f;
                        probe.position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * OrbitRadius;
                        yield return null;
                        samples[count++] = Time.unscaledDeltaTime * 1000f;
                    }

                    for (var i = 0; i < count; i++)
                        csv.Append(levelName).Append(';').Append(viewName).Append(';').Append(i).Append(';')
                            .Append(samples[i].ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
                    summary.AppendLine(Summarize(levelName, viewName, count));
                    // Se reescribe tras cada punto: si el juego se cierra antes de terminar, lo medido se conserva.
                    File.WriteAllText(Path.Combine(outputFolder, "resumen.txt"), summary.ToString(), Encoding.UTF8);
                }
            }

            File.WriteAllText(Path.Combine(outputFolder, "resumen.txt"), summary.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(outputFolder, "frames.csv"), csv.ToString(), Encoding.UTF8);
            Debug.Log("[Benchmark] Terminado:\n" + summary);
            Application.Quit(0);
        }

        static bool LogQuitRequest()
        {
            Debug.Log("[Benchmark] Cierre solicitado:\n" + Environment.StackTrace);
            return true;
        }

        string Summarize(string level, string view, int count)
        {
            Array.Sort(samples, 0, count);
            var total = 0.0;
            for (var i = 0; i < count; i++)
                total += samples[i];
            var mean = total / count;
            var p95 = samples[Mathf.Min(count - 1, (int)(count * 0.95f))];
            var worstStart = Mathf.Min(count - 1, (int)(count * 0.99f));
            var worst = 0.0;
            for (var i = worstStart; i < count; i++)
                worst += samples[i];
            worst /= count - worstStart;
            var c = CultureInfo.InvariantCulture;
            return string.Join(";", level, view, count.ToString(c), (1000.0 / mean).ToString("0", c),
                (1000.0 / worst).ToString("0", c), mean.ToString("0.00", c), p95.ToString("0.00", c),
                samples[count - 1].ToString("0.00", c));
        }
    }
}
