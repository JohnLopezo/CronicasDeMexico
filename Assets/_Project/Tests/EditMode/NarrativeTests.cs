using System.Collections.Generic;
using System.Reflection;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Banderas de la historia, diálogos por ramas y misiones (fase narrativa 2, etapas N1 y N2).
    /// </summary>
    public class NarrativeTests
    {
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

        readonly List<Object> created = new();

        [SetUp]
        public void SetUp() => ServiceLocator.Clear();

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            foreach (var item in created)
                if (item != null)
                    Object.DestroyImmediate(item);
            created.Clear();
        }

        T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }

        QuestData TalkQuest(string id, params string[] npcs)
        {
            var steps = new QuestStep[npcs.Length];
            for (var i = 0; i < npcs.Length; i++)
                steps[i] = new QuestStep(StoryEvents.Talk(npcs[i]), $"Habla con {npcs[i]}");
            return Track(QuestData.Create(id, $"Misión {id}", steps, $"recompensa.{id}"));
        }

        // --- StoryState ---

        [Test]
        public void StoryState_SetAndClearFlag_NotifiesOnlyOnRealChanges()
        {
            var story = new StoryState();
            var changes = new List<string>();
            story.FlagChanged += changes.Add;

            story.SetFlag("a");
            story.SetFlag("a");
            story.ClearFlag("b");
            story.ClearFlag("a");

            Assert.IsFalse(story.HasFlag("a"));
            CollectionAssert.AreEqual(new[] { "a", "a" }, changes);
        }

        // Al salir de la mazmorra se borran las banderas de la incursión (santuarios de Vida) y nada más.
        [Test]
        public void StoryState_ClearFlagsWithPrefix_RemovesOnlyThoseFlags()
        {
            var story = new StoryState();
            story.SetFlag("incursion.santuario_vida.vida_p2");
            story.SetFlag("incursion.otra");
            story.SetFlag("santuario_memoria.memoria_p1");
            var cleared = new List<string>();
            story.FlagChanged += cleared.Add;

            story.ClearFlagsWithPrefix("incursion.");

            CollectionAssert.AreEquivalent(new[] { "incursion.santuario_vida.vida_p2", "incursion.otra" }, cleared);
            CollectionAssert.AreEqual(new[] { "santuario_memoria.memoria_p1" }, story.Flags);
        }

        // --- DialogueData ---

        [Test]
        public void Dialogue_SelectsFirstBranchWhoseConditionsHold()
        {
            var afterQuest = new DialogueBranch(new[] { new DialogueLine("Gracias.") },
                requiredFlags: new[] { "mision.X.completada" });
            var notMet = new DialogueBranch(new[] { new DialogueLine("¿Ya volviste?") },
                requiredFlags: new[] { "mision.X.activa" }, forbiddenFlags: new[] { "prisa" });
            var fallback = new DialogueBranch(new[] { new DialogueLine("Hola.") });
            var dialogue = Track(DialogueData.Create("DLG_Prueba", afterQuest, notMet, fallback));

            var flags = new HashSet<string> { "mision.X.activa", "prisa" };
            Assert.AreSame(fallback, dialogue.Select(flags.Contains), "Una bandera prohibida debe descartar la rama.");

            flags.Remove("prisa");
            Assert.AreSame(notMet, dialogue.Select(flags.Contains));

            flags.Add("mision.X.completada");
            Assert.AreSame(afterQuest, dialogue.Select(flags.Contains), "Gana la primera rama disponible.");
        }

        [Test]
        public void Dialogue_BranchWithoutLines_IsNeverSelected()
        {
            var empty = new DialogueBranch(new DialogueLine[0]);
            Assert.IsNull(Track(DialogueData.Create("DLG_Vacio", empty)).Select(_ => true));
        }

        // --- QuestService ---

        [Test]
        public void Quest_AdvancesStepByStep_AndCompletesWithRewardsOnce()
        {
            var story = new StoryState();
            var service = new QuestService(story);
            var quest = TalkQuest("MP_TEST", "artesano", "anciana");
            var completed = 0;
            service.Completed += _ => completed++;

            story.Report(StoryEvents.Talk("artesano")); // antes de empezar: no cuenta
            Assert.IsTrue(service.Start(quest));
            Assert.IsFalse(service.Start(quest), "No se puede empezar dos veces.");
            Assert.IsTrue(story.HasFlag(StoryEvents.QuestActiveFlag("MP_TEST")));
            Assert.AreEqual(0, service.GetStep("MP_TEST"));

            story.Report(StoryEvents.Talk("anciana")); // fuera de orden: no cuenta
            Assert.AreEqual(0, service.GetStep("MP_TEST"));

            story.Report(StoryEvents.Talk("artesano"));
            Assert.AreEqual(1, service.GetStep("MP_TEST"));
            Assert.IsTrue(service.TryGetCurrentObjective(out var title, out var description));
            Assert.AreEqual("Misión MP_TEST", title);
            Assert.AreEqual("Habla con anciana", description);

            story.Report(StoryEvents.Talk("anciana"));
            story.Report(StoryEvents.Talk("anciana"));

            Assert.AreEqual(QuestStatus.Completed, service.GetStatus("MP_TEST"));
            Assert.AreEqual(1, completed, "La recompensa se da una sola vez.");
            Assert.IsFalse(story.HasFlag(StoryEvents.QuestActiveFlag("MP_TEST")));
            Assert.IsTrue(story.HasFlag(StoryEvents.QuestCompletedFlag("MP_TEST")));
            Assert.IsTrue(story.HasFlag("recompensa.MP_TEST"));
            Assert.IsFalse(service.TryGetCurrentObjective(out _, out _));
        }

        [Test]
        public void Quest_StepWithCount_ShowsProgress()
        {
            var story = new StoryState();
            var service = new QuestService(story);
            var quest = Track(QuestData.Create("MS_TEST", "Cacería",
                new[] { new QuestStep(StoryEvents.Defeat("espiritu_jaguar"), "Vence a los espíritus jaguar", 3) }));
            service.Start(quest);

            story.Report(StoryEvents.Defeat("espiritu_jaguar"));
            Assert.IsTrue(service.TryGetCurrentObjective(out _, out var description));
            Assert.AreEqual("Vence a los espíritus jaguar (1/3)", description);

            story.Report(StoryEvents.Defeat("espiritu_jaguar"));
            story.Report(StoryEvents.Defeat("espiritu_jaguar"));
            Assert.AreEqual(QuestStatus.Completed, service.GetStatus("MS_TEST"));
        }

        [Test]
        public void Objective_IsFromLastStartedActiveQuest()
        {
            var story = new StoryState();
            var service = new QuestService(story);
            service.Start(TalkQuest("A", "artesano"));
            service.Start(TalkQuest("B", "anciana"));

            service.TryGetCurrentObjective(out var title, out _);
            Assert.AreEqual("Misión B", title);

            story.Report(StoryEvents.Talk("anciana"));
            service.TryGetCurrentObjective(out title, out _);
            Assert.AreEqual("Misión A", title, "Al completar B vuelve a mostrarse A.");
        }

        // --- DialogueInteractable con datos ---

        [Test]
        public void DialogueInteractable_OnFinish_SetsFlagsStartsQuestAndReportsTalk()
        {
            var story = new StoryState();
            var quests = new QuestService(story);
            ServiceLocator.Register(story);
            ServiceLocator.Register(quests);

            var npc = Track(NpcData.Create("anciana", "La anciana", "anciana"));
            var quest = TalkQuest("MP_TEST", "anciana", "artesano");
            var first = new DialogueBranch(
                new[] { new DialogueLine("Ya no recuerdo el final del relato."), new DialogueLine("¿Me ayudarás?") },
                forbiddenFlags: new[] { "anciana.pidio_ayuda" },
                flagsOnFinish: new[] { "anciana.pidio_ayuda" },
                questToStart: quest);
            var later = new DialogueBranch(new[] { new DialogueLine("Gracias por escucharme.") });
            var dialogue = Track(DialogueData.Create("DLG_Anciana", first, later));

            var channel = Track(ScriptableObject.CreateInstance<StringEventChannel>());
            var messages = new List<string>();
            channel.Subscribe(messages.Add);

            var go = Track(new GameObject("Anciana"));
            var interactable = go.AddComponent<DialogueInteractable>();
            Set(interactable, "npc", npc);
            Set(interactable, "dialogue", dialogue);
            Set(interactable, "messageChannel", channel);

            // Se aleja a mitad: no cuenta.
            interactable.Interact(go.transform);
            interactable.End();
            Assert.IsFalse(story.HasFlag("anciana.pidio_ayuda"));
            Assert.AreEqual(QuestStatus.NotStarted, quests.GetStatus("MP_TEST"));

            interactable.Interact(go.transform);
            Assert.IsTrue(interactable.Advance());
            Assert.IsFalse(interactable.Advance());
            interactable.End();

            Assert.AreEqual("<b>La anciana</b>\nYa no recuerdo el final del relato.", messages[0]);
            Assert.IsTrue(story.HasFlag("anciana.pidio_ayuda"));
            Assert.AreEqual(QuestStatus.Active, quests.GetStatus("MP_TEST"));
            // El aviso "hablar:anciana" llega después de empezar la misión, así que cuenta para su primer paso.
            Assert.AreEqual(1, quests.GetStep("MP_TEST"));

            messages.Clear();
            interactable.Interact(go.transform);
            Assert.AreEqual("<b>La anciana</b>\nGracias por escucharme.", messages[0], "Con la bandera, otra rama.");
        }

        [Test]
        public void DialogueInteractable_WithoutData_KeepsFixedLines()
        {
            var channel = Track(ScriptableObject.CreateInstance<StringEventChannel>());
            var messages = new List<string>();
            channel.Subscribe(messages.Add);

            var go = Track(new GameObject("Artesano"));
            var interactable = go.AddComponent<DialogueInteractable>();
            Set(interactable, "speakerName", "Artesano de jade");
            Set(interactable, "lines", new[] { "Uno.", "Dos." });
            Set(interactable, "messageChannel", channel);

            interactable.Interact(go.transform);
            Assert.IsTrue(interactable.Advance());
            Assert.IsFalse(interactable.Advance());
            CollectionAssert.AreEqual(new[] { "<b>Artesano de jade</b>\nUno.", "<b>Artesano de jade</b>\nDos." }, messages);
        }

        static void Set(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, PrivateField);
            Assert.IsNotNull(info, $"No existe el campo {field}.");
            info.SetValue(target, value);
        }
    }
}
