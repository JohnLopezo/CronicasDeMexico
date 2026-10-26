using UnityEngine;

namespace CronicasDeMexico.Core.Services
{
    /// <summary>
    /// Cambia entre los niveles de calidad Bajo / Medio / Alto (cada uno con su URP Asset) y recuerda la elección.
    /// </summary>
    public sealed class QualityService
    {
        const string PrefKey = "calidad.nivel";

        public int CurrentLevel => QualitySettings.GetQualityLevel();
        public string[] LevelNames => QualitySettings.names;

        public void ApplySaved()
        {
            if (PlayerPrefs.HasKey(PrefKey))
                SetLevel(PlayerPrefs.GetInt(PrefKey), save: false);
        }

        public void SetLevel(int level, bool save = true)
        {
            level = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);

            if (!save) return;
            PlayerPrefs.SetInt(PrefKey, level);
            PlayerPrefs.Save();
        }
    }
}
