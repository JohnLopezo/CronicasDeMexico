using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Narrative
{
    /// <summary>
    /// Empieza una misión al cargar la escena si aún no se empezó (p. ej. el despertar al comenzar la partida).
    /// Espera a QuestService: al pulsar Play desde una escena de juego, Boot se carga después.
    /// </summary>
    public sealed class StartQuestOnLoad : MonoBehaviour
    {
        [SerializeField] QuestData quest;

        public QuestData Quest
        {
            get => quest;
            set => quest = value;
        }

        void Update()
        {
            if (!ServiceLocator.TryGet(out QuestService quests))
                return;

            if (quest != null && quests.GetStatus(quest.Id) == QuestStatus.NotStarted)
                quests.Start(quest);
            enabled = false;
        }
    }
}
