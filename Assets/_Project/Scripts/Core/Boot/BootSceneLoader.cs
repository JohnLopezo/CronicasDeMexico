using UnityEngine;
using UnityEngine.SceneManagement;

namespace CronicasDeMexico.Core.Boot
{
    /// <summary>
    /// Garantiza que la escena Boot esté cargada aunque se pulse Play desde cualquier otra escena.
    /// </summary>
    public static class BootSceneLoader
    {
        public const string BootSceneName = "Boot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureBootLoaded()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == BootSceneName)
                    return;
            }

            SceneManager.LoadScene(BootSceneName, LoadSceneMode.Additive);
        }
    }
}
