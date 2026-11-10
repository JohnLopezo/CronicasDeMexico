using System;
using CronicasDeMexico.Core.Narrative;
using UnityEngine;

namespace CronicasDeMexico.Data.Narrative
{
    /// <summary>
    /// Misión: pasos en orden, cada uno cumplido por un aviso de StoryState (StoryEvents: "hablar:id",
    /// "examinar:id", "vencer:id", "llegar:id") repetido las veces indicadas. Ids y contenido en
    /// Docs/Historia/Misiones.md. Debe estar en el catálogo para recuperarse de una partida guardada.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Narrativa/Misión", fileName = "MIS_")]
    public sealed class QuestData : ScriptableObject, IQuestDefinition
    {
        [SerializeField, Tooltip("Identificador estable, p. ej. \"MP_OLM_02\". Es lo que se guarda.")]
        string id;

        [SerializeField] string title;

        [SerializeField] QuestStep[] steps = Array.Empty<QuestStep>();

        [SerializeField, Tooltip("Banderas que se activan una sola vez al completarla.")]
        string[] rewardFlags = Array.Empty<string>();

        [SerializeField, Tooltip("Opcional: experiencia y objetos que da al completarse (RewardService).")]
        Rewards.RewardBundle reward;

        public string Id => id;
        public string Title => title;
        public int StepCount => steps.Length;
        public string[] RewardFlags => rewardFlags;
        public Core.Progress.RewardPackage Reward => reward != null ? reward.ToPackage() : null;

        public Rewards.RewardBundle RewardBundle
        {
            get => reward;
            set => reward = value;
        }

        public QuestStepInfo GetStep(int index) =>
            index >= 0 && index < steps.Length ? steps[index].ToInfo() : default;

        public static QuestData Create(string id, string title, QuestStep[] steps, params string[] rewardFlags)
        {
            var quest = CreateInstance<QuestData>();
            quest.name = $"MIS_{id}";
            quest.id = id;
            quest.title = title;
            quest.steps = steps ?? Array.Empty<QuestStep>();
            quest.rewardFlags = rewardFlags ?? Array.Empty<string>();
            return quest;
        }
    }

    [Serializable]
    public sealed class QuestStep
    {
        [SerializeField, Tooltip("Aviso que cumple el paso, p. ej. \"hablar:anciana\" (ver StoryEvents).")]
        string eventKey;

        [SerializeField, Min(1)] int requiredCount = 1;

        [SerializeField, Tooltip("Objetivo que se muestra en el HUD.")]
        string description;

        public QuestStep(string eventKey, string description, int requiredCount = 1)
        {
            this.eventKey = eventKey;
            this.description = description;
            this.requiredCount = requiredCount;
        }

        public QuestStepInfo ToInfo() => new(eventKey, requiredCount, description);
    }
}
