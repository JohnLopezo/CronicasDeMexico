using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CronicasDeMexico.Editor.Build
{
    /// <summary>
    /// Build de desarrollo para Windows con las escenas activas de Build Settings: arranca en Boot, con el menú de
    /// inicio. Sirve para jugar fuera del Editor. Queda en Builds/Windows/Juego/ (ignorado por Git).
    /// Desde consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.Build.WindowsGameBuild.BuildDevelopment
    /// </summary>
    public static class WindowsGameBuild
    {
        public const string OutputPath = "Builds/Windows/Juego/CronicasDeMexico.exe";

        [MenuItem("Crónicas/Build/Windows: juego completo (desarrollo)")]
        public static void BuildDevelopment()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            });
            var summary = report.summary;
            var message = $"[Build Windows] {summary.result}: {summary.totalErrors} errores, {summary.totalWarnings} avisos, " +
                          $"{summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss}. {OutputPath}";

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(message);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1); // en modo batch, un código distinto de 0 marca el fallo
                return;
            }

            Debug.Log(message);
        }
    }
}
