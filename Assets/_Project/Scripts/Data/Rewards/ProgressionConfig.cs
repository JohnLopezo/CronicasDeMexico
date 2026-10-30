using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Data.Characters;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Reglas de la progresión (Docs/Sistemas/Sistema_Progresion.md), editables sin tocar código: el Guardián en uso,
    /// la curva de experiencia (base × nivel^exponente) y la protección contra el farmeo. Vive en Resources.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Configuración de progresión", fileName = "ConfigProgresion")]
    public sealed class ProgressionConfig : ScriptableObject, IProgressionConfig
    {
        public const string ResourcePath = "Recompensas/ConfigProgresion";

        [SerializeField, Tooltip("Guardián en uso (sus atributos por nivel).")] GuardianData guardian;
        [SerializeField, Min(2)] int maxLevel = 20;
        [SerializeField, Min(1f), Tooltip("Experiencia para pasar del nivel 1 al 2.")] float baseExperience = 100f;
        [SerializeField, Min(1f), Tooltip("Cuánto crece la experiencia necesaria con el nivel.")] float exponent = 1.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Se pierde por cada nivel por encima del recomendado de la zona.")]
        float overLevelPenalty = 0.25f;
        [SerializeField, Range(0f, 1f), Tooltip("Lo mínimo que se conserva por estar por encima de la zona.")]
        float overLevelFloor = 0.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Lo que se conserva cada vez que se vence otra vez al mismo enemigo.")]
        float repeatFactor = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Por debajo de este multiplicador, el enemigo repetido no da experiencia.")]
        float repeatCutoff = 0.2f;

        public IStatProfile Profile => guardian != null ? guardian : DefaultProgressionConfig.Instance;
        public int MaxLevel => maxLevel;
        public int ExperienceToNext(int level) => Mathf.RoundToInt(baseExperience * Mathf.Pow(Mathf.Max(1, level), exponent));
        public float OverLevelPenalty => overLevelPenalty;
        public float OverLevelFloor => overLevelFloor;
        public float RepeatFactor => repeatFactor;
        public float RepeatCutoff => repeatCutoff;

        public GuardianData Guardian
        {
            get => guardian;
            set => guardian = value;
        }
    }
}
