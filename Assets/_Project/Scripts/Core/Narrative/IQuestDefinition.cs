namespace CronicasDeMexico.Core.Narrative
{
    /// <summary>
    /// Definición de una misión (la implementa QuestData, en Data). QuestService solo necesita esto, así que vive
    /// en Core junto a los servicios que registra GameBootstrapper.
    /// </summary>
    public interface IQuestDefinition
    {
        /// <summary>Identificador estable, p. ej. "MP_OLM_02". Es lo que se guarda.</summary>
        string Id { get; }

        string Title { get; }
        int StepCount { get; }
        QuestStepInfo GetStep(int index);

        /// <summary>Banderas que se activan una sola vez al completarla (recompensas, cambios en el mundo).</summary>
        string[] RewardFlags { get; }

        /// <summary>Experiencia y objetos que da al completarse (los entrega RewardService), o null.</summary>
        Progress.RewardPackage Reward { get; }
    }

    /// <summary>Un paso se cumple al recibir <see cref="RequiredCount"/> veces el aviso <see cref="EventKey"/>.</summary>
    public readonly struct QuestStepInfo
    {
        public readonly string EventKey;
        public readonly int RequiredCount;
        public readonly string Description;

        public QuestStepInfo(string eventKey, int requiredCount, string description)
        {
            EventKey = eventKey;
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
            Description = description;
        }
    }

    /// <summary>Busca definiciones por id, para recuperar misiones de una partida guardada.</summary>
    public interface IQuestCatalog
    {
        IQuestDefinition Find(string questId);
    }
}
