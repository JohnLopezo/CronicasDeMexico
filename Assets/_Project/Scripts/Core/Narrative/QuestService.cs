using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Save;
using UnityEngine;

namespace CronicasDeMexico.Core.Narrative
{
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
    }

    /// <summary>
    /// Lleva las misiones: las empieza, las hace avanzar con los avisos de StoryState y las completa. Mantiene
    /// en StoryState las banderas "mision.ID.activa" y "mision.ID.completada" para que los diálogos puedan
    /// preguntar por ellas. Las recompensas (RewardFlags y el evento Completed) se dan una sola vez: al pasar a
    /// completada, nunca al cargar una partida.
    /// </summary>
    public sealed class QuestService : ISaveParticipant
    {
        sealed class Progress
        {
            public IQuestDefinition Definition;
            public QuestStatus Status;
            public int Step;
            public int Count;
        }

        readonly StoryState story;
        readonly IQuestCatalog catalog;
        readonly Dictionary<string, Progress> quests = new();
        // Orden de inicio: el objetivo que se muestra es el de la última misión activa.
        readonly List<Progress> order = new();

        /// <summary>Algo cambió (inicio, avance, final o carga): la UI vuelve a leer el objetivo.</summary>
        public event Action Changed;

        /// <summary>Se completó una misión durante el juego (no se publica al cargar).</summary>
        public event Action<IQuestDefinition> Completed;

        public QuestService(StoryState story, IQuestCatalog catalog = null)
        {
            this.story = story ?? throw new ArgumentNullException(nameof(story));
            this.catalog = catalog;
            story.EventReported += OnEventReported;
        }

        public QuestStatus GetStatus(string questId) =>
            questId != null && quests.TryGetValue(questId, out var progress) ? progress.Status : QuestStatus.NotStarted;

        /// <summary>Paso actual de una misión activa (0 si no lo está).</summary>
        public int GetStep(string questId) =>
            questId != null && quests.TryGetValue(questId, out var progress) ? progress.Step : 0;

        /// <summary>Empieza la misión. Devuelve false si ya estaba activa o completada.</summary>
        public bool Start(IQuestDefinition quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.Id) || quests.ContainsKey(quest.Id))
                return false;

            var progress = new Progress { Definition = quest, Status = QuestStatus.Active };
            quests.Add(quest.Id, progress);
            order.Add(progress);
            story.SetFlag(StoryEvents.QuestActiveFlag(quest.Id));

            // Una misión sin pasos se completa al empezar.
            if (quest.StepCount == 0)
                Complete(progress);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Título y descripción del paso actual de la última misión activa; false si no hay ninguna.</summary>
        public bool TryGetCurrentObjective(out string title, out string description)
        {
            for (var i = order.Count - 1; i >= 0; i--)
            {
                var progress = order[i];
                if (progress.Status != QuestStatus.Active || progress.Definition == null)
                    continue;

                title = progress.Definition.Title;
                var step = progress.Definition.GetStep(progress.Step);
                description = step.RequiredCount > 1
                    ? $"{step.Description} ({progress.Count}/{step.RequiredCount})"
                    : step.Description;
                return true;
            }

            title = description = null;
            return false;
        }

        void OnEventReported(string happening)
        {
            var changed = false;
            // Copia por si completar una misión activa banderas que alguien usa para empezar otra.
            foreach (var progress in order.ToArray())
            {
                if (progress.Status != QuestStatus.Active || progress.Definition == null)
                    continue;

                var step = progress.Definition.GetStep(progress.Step);
                if (step.EventKey != happening)
                    continue;

                changed = true;
                if (++progress.Count < step.RequiredCount)
                    continue;

                progress.Count = 0;
                if (++progress.Step >= progress.Definition.StepCount)
                    Complete(progress);
            }

            if (changed)
                Changed?.Invoke();
        }

        void Complete(Progress progress)
        {
            var quest = progress.Definition;
            progress.Status = QuestStatus.Completed;
            progress.Step = quest.StepCount;
            progress.Count = 0;
            story.ClearFlag(StoryEvents.QuestActiveFlag(quest.Id));
            story.SetFlag(StoryEvents.QuestCompletedFlag(quest.Id));
            if (quest.RewardFlags != null)
                foreach (var flag in quest.RewardFlags)
                    story.SetFlag(flag);
            Completed?.Invoke(quest);
        }

        public void Capture(SaveData data)
        {
            data.quests.Clear();
            foreach (var progress in order)
                data.quests.Add(new QuestRecord
                {
                    id = progress.Definition.Id,
                    completed = progress.Status == QuestStatus.Completed,
                    step = progress.Step,
                    count = progress.Count,
                });
        }

        // Las banderas de misión ya vienen en la partida (StoryState se restaura antes); aquí solo el progreso.
        public void Restore(SaveData data)
        {
            quests.Clear();
            order.Clear();
            foreach (var record in data.quests)
            {
                if (string.IsNullOrEmpty(record.id) || quests.ContainsKey(record.id))
                    continue;

                var definition = catalog?.Find(record.id);
                if (definition == null)
                {
                    Debug.LogWarning($"[Misiones] La partida tiene la misión '{record.id}', que no está en el catálogo; se ignora.");
                    continue;
                }

                var progress = new Progress
                {
                    Definition = definition,
                    Status = record.completed ? QuestStatus.Completed : QuestStatus.Active,
                    Step = record.completed ? definition.StepCount : Math.Clamp(record.step, 0, Math.Max(0, definition.StepCount - 1)),
                    Count = record.completed ? 0 : Math.Max(0, record.count),
                };
                quests.Add(record.id, progress);
                order.Add(progress);
            }

            Changed?.Invoke();
        }
    }
}
