using System.IO;
using CronicasDeMexico.Editor.SceneGeneration;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CronicasDeMexico.Editor.Build
{
    /// <summary>
    /// Build de desarrollo de Windows para medir el rendimiento de la ciudad olmeca (o de la selva, BuildJungle; del
    /// piso 2 de la mazmorra, BuildDungeon, o de la arena del piso 3, BuildArena). La primera escena es la que se mide
    /// (Boot se añade sola, ver BootSceneLoader). Se ejecuta con -benchmark (ver FrameTimeBenchmark). El ejecutable queda
    /// en Builds/Windows/Ciudad/, Selva/, Mazmorra/ o Arena/, ignorado por Git. Desde consola:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod CronicasDeMexico.Editor.Build.WindowsBenchmarkBuild.Build
    /// </summary>
    public static class WindowsBenchmarkBuild
    {
        public const string OutputPath = "Builds/Windows/Ciudad/CronicasDeMexico.exe";
        public const string JungleOutputPath = "Builds/Windows/Selva/CronicasDeMexico.exe";
        public const string DungeonOutputPath = "Builds/Windows/Mazmorra/CronicasDeMexico.exe";
        public const string ArenaOutputPath = "Builds/Windows/Arena/CronicasDeMexico.exe";
        const string BootScenePath = "Assets/_Project/Scenes/Boot/Boot.unity";

        [MenuItem("Crónicas/Build/Windows: medición de la ciudad (desarrollo)")]
        public static void Build() => Build(OlmecaCityGenerator.ScenePath, OutputPath);

        [MenuItem("Crónicas/Build/Windows: medición de la selva (desarrollo)")]
        public static void BuildJungle() => Build(OlmecaJungleGenerator.ScenePath, JungleOutputPath);

        [MenuItem("Crónicas/Build/Windows: medición de la mazmorra, piso 2 (desarrollo)")]
        public static void BuildDungeon() => Build(OlmecaDungeonGenerator.ScenePath(2), DungeonOutputPath);

        [MenuItem("Crónicas/Build/Windows: medición de la arena del Guardián, piso 3 (desarrollo)")]
        public static void BuildArena() => Build(OlmecaDungeonGenerator.ScenePath(3), ArenaOutputPath);

        static void Build(string scenePath, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath, BootScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            var message = $"[Build Windows] {summary.result}: {summary.totalErrors} errores, {summary.totalWarnings} avisos, " +
                          $"{summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss}. {outputPath}";
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
