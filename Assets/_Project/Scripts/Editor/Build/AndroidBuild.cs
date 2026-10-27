using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CronicasDeMexico.Editor.Build
{
    /// <summary>
    /// Build de desarrollo para Android con las escenas activas de Build Settings. Sirve para comprobar que el
    /// proyecto compila para Android y para probar en un dispositivo (perfilable y con depuración de scripts).
    /// El APK queda en Builds/Android/, fuera de Assets e ignorado por Git.
    /// Desde consola (cambia el target del proyecto a Android):
    /// Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod
    /// CronicasDeMexico.Editor.Build.AndroidBuild.BuildDevelopment
    /// </summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/CronicasDeMexico-dev.apk";

        [MenuItem("Crónicas/Build/Android (desarrollo)")]
        static void BuildFromMenu()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUtility.DisplayDialog("Build Android",
                    "El target activo no es Android. Unity cambiará a Android y reimportará assets, lo que puede tardar.",
                    "Continuar", "Cancelar"))
                return;

            BuildDevelopment();
        }

        public static void BuildDevelopment()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
            };

            EditorUserBuildSettings.buildAppBundle = false; // APK para instalar directamente en el dispositivo

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            var message = $"[Build Android] {summary.result}: {summary.totalErrors} errores, {summary.totalWarnings} avisos, " +
                          $"{summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss}. {OutputPath}";

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(message);
                // En modo batch, un código de salida distinto de 0 marca el fallo.
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            Debug.Log(message);
        }
    }
}
