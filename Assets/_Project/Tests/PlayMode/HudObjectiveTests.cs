using System.Collections;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CronicasDeMexico.Tests
{
    public class HudObjectiveTests
    {
        const string SceneName = "Olmeca_Ciudad";

        [UnityTest]
        public IEnumerator Hud_ShowsActiveQuestObjective_AndHidesItWhenCompleted()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            yield return null;
            yield return null; // StartQuestOnLoad empieza el despertar en el primer Update: esta misión va después

            Assert.IsTrue(ServiceLocator.TryGet(out StoryState story), "Boot no registró StoryState.");
            Assert.IsTrue(ServiceLocator.TryGet(out QuestService quests), "Boot no registró QuestService.");

            var quest = QuestData.Create("MS_PRUEBA_HUD", "Piedra verde",
                new[] { new QuestStep(StoryEvents.Talk("artesano_prueba"), "Habla con el artesano de jade") });
            try
            {
                Assert.IsTrue(quests.Start(quest));
                yield return null;

                var objective = FindObjective();
                Assert.IsNotNull(objective, "El HUD no tiene el objetivo.");
                Assert.IsFalse(objective.ClassListContains("hidden"), "El objetivo no se muestra.");
                Assert.AreEqual("Piedra verde", objective.Q<Label>("objective-title").text);
                Assert.AreEqual("Habla con el artesano de jade", objective.Q<Label>("objective-text").text);

                story.Report(StoryEvents.Talk("artesano_prueba"));
                yield return null;
                // Al completarla vuelve el objetivo anterior (en la ciudad, el despertar) o se oculta si no hay ninguno.
                Assert.IsTrue(objective.ClassListContains("hidden") || objective.Q<Label>("objective-title").text != "Piedra verde",
                    "El objetivo de una misión completada sigue en pantalla.");
            }
            finally
            {
                Object.Destroy(quest);
            }

            yield return SceneManager.UnloadSceneAsync(SceneName);
        }

        static VisualElement FindObjective()
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var objective = document.rootVisualElement?.Q("objective");
                if (objective != null)
                    return objective;
            }
            return null;
        }
    }
}
