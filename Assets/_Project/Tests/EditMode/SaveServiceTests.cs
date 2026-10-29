using System;
using System.IO;
using System.Text.RegularExpressions;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Data.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Guardado de la partida (fase narrativa 2, etapa N3): ida y vuelta, copia de respaldo y versiones.
    /// </summary>
    public class SaveServiceTests
    {
        string folder;
        QuestData quest;
        QuestCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "CronicasPruebaGuardado_" + Guid.NewGuid().ToString("N"));
            quest = QuestData.Create("MP_TEST", "Prueba", new[]
            {
                new QuestStep(StoryEvents.Talk("artesano"), "Habla con el artesano"),
                new QuestStep(StoryEvents.Defeat("espiritu_jaguar"), "Vence a los espíritus", 2),
            }, "recompensa.MP_TEST");
            catalog = QuestCatalog.Create(quest);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Object.DestroyImmediate(quest);
            Object.DestroyImmediate(catalog);
        }

        (SaveService save, StoryState story, QuestService quests) NewGame()
        {
            var story = new StoryState();
            var quests = new QuestService(story, catalog);
            var save = new SaveService(folder);
            save.Register(story);
            save.Register(quests);
            return (save, story, quests);
        }

        [Test]
        public void SaveAndLoad_RestoresFlagsAndQuestProgress_WithoutRegrantingRewards()
        {
            var (save, story, quests) = NewGame();
            story.SetFlag("anciana.pidio_ayuda");
            quests.Start(quest);
            story.Report(StoryEvents.Talk("artesano"));
            story.Report(StoryEvents.Defeat("espiritu_jaguar"));
            Assert.IsTrue(save.Save());

            var (load, loadedStory, loadedQuests) = NewGame();
            var completed = 0;
            loadedQuests.Completed += _ => completed++;
            Assert.IsTrue(load.Load());

            Assert.IsTrue(loadedStory.HasFlag("anciana.pidio_ayuda"));
            Assert.IsTrue(loadedStory.HasFlag(StoryEvents.QuestActiveFlag("MP_TEST")));
            Assert.AreEqual(QuestStatus.Active, loadedQuests.GetStatus("MP_TEST"));
            Assert.AreEqual(1, loadedQuests.GetStep("MP_TEST"));
            Assert.IsTrue(loadedQuests.TryGetCurrentObjective(out _, out var description));
            Assert.AreEqual("Vence a los espíritus (1/2)", description, "Se conserva la cuenta del paso.");

            loadedStory.Report(StoryEvents.Defeat("espiritu_jaguar"));
            Assert.AreEqual(QuestStatus.Completed, loadedQuests.GetStatus("MP_TEST"));
            Assert.AreEqual(1, completed);

            // Guardar y cargar una misión completada no vuelve a dar la recompensa.
            Assert.IsTrue(load.Save());
            var (again, _, againQuests) = NewGame();
            againQuests.Completed += _ => completed++;
            Assert.IsTrue(again.Load());
            Assert.AreEqual(QuestStatus.Completed, againQuests.GetStatus("MP_TEST"));
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void Load_WithoutSave_ReturnsFalseAndKeepsState()
        {
            var (save, story, _) = NewGame();
            story.SetFlag("algo");

            Assert.IsFalse(save.HasSave);
            Assert.IsFalse(save.Load());
            Assert.IsTrue(story.HasFlag("algo"));
        }

        [Test]
        public void Load_UsesBackup_WhenMainFileIsCorrupt()
        {
            var (save, story, _) = NewGame();
            story.SetFlag("primera");
            save.Save();
            story.SetFlag("segunda");
            save.Save(); // la primera partida pasa a ser la copia de respaldo

            Assert.IsTrue(File.Exists(save.BackupPath));
            File.WriteAllText(save.MainPath, "{ esto no es json");

            var (load, loadedStory, _) = NewGame();
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Guardado\]"));
            Assert.IsTrue(load.Load());
            Assert.IsTrue(loadedStory.HasFlag("primera"));
            Assert.IsFalse(loadedStory.HasFlag("segunda"), "La copia es la partida anterior.");
        }

        [Test]
        public void Load_RejectsSaveFromNewerVersion()
        {
            var (save, story, _) = NewGame();
            story.SetFlag("algo");
            save.Save();
            var json = File.ReadAllText(save.MainPath).Replace(
                $"\"version\": {SaveService.CurrentVersion}", $"\"version\": {SaveService.CurrentVersion + 1}");
            File.WriteAllText(save.MainPath, json);

            var (load, loadedStory, _) = NewGame();
            LogAssert.Expect(LogType.Warning, new Regex("versión más nueva"));
            Assert.IsFalse(load.Load());
            Assert.IsFalse(loadedStory.HasFlag("algo"));
        }

        [Test]
        public void Load_IgnoresQuestMissingFromCatalog()
        {
            var (save, story, quests) = NewGame();
            var unknown = QuestData.Create("MS_BORRADA", "Ya no existe",
                new[] { new QuestStep(StoryEvents.Talk("nadie"), "…") });
            quests.Start(unknown);
            quests.Start(quest);
            save.Save();
            Object.DestroyImmediate(unknown);

            var (load, _, loadedQuests) = NewGame();
            LogAssert.Expect(LogType.Warning, new Regex("MS_BORRADA"));
            Assert.IsTrue(load.Load());
            Assert.AreEqual(QuestStatus.NotStarted, loadedQuests.GetStatus("MS_BORRADA"));
            Assert.AreEqual(QuestStatus.Active, loadedQuests.GetStatus("MP_TEST"));
        }
    }
}
