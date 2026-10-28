using System.IO;
using CronicasDeMexico.Editor.SceneGeneration;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CronicasDeMexico.Editor.Build
{
    /// <summary>
    /// Build de desarrollo de Windows para medir el rendimiento de la ciudad olmeca. La primera escena es Olmeca_Ciudad
    /// (Boot se añade sola, ver BootSceneLoader). Se ejecuta con -benchmark (ver FrameTimeBenchmark).
    /// El ejecutable queda en Builds/Windows/Ciudad/, ignorado por Git. Desde consola:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod CronicasDeMexico.Editor.Build.WindowsBenchmarkBuild.Build
    /// </summary>
    public static class WindowsBenchmarkBuild
    {
        public const string OutputPath = "Builds/Windows/Ciudad/CronicasDeMexico.exe";
        const string BootScenePath = "Assets/_Project/Scenes/Boot/Boot.unity";

        [MenuItem("Crónicas/Build/Windows: medición de la ciudad (desarrollo)")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { OlmecaCityGenerator.ScenePath, BootScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            var message = $"[Build Windows] {summary.result}: {summary.totalErrors} errores, {summary.totalWarnings} avisos, " +
                          $"{summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss}. {OutputPath}";
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(message);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            Debug.Log(message);
        }
    }
}
