using System.Collections;
using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>E7: acciones de senda con los habitantes y cocina de la curandera, en la ciudad real.</summary>
    public class PathActionAndCookingTests
    {
        const string City = "Olmeca_Ciudad";

        StoryState bootStory;
        Inventory bootInventory;
        Progression bootProgression;
        RewardService bootRewards;
        StoryState story;
        Inventory inventory;
        Progression progression;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ServiceLocator.TryGet(out bootStory);
            ServiceLocator.TryGet(out bootInventory);
            ServiceLocator.TryGet(out bootProgression);
            ServiceLocator.TryGet(out bootRewards);

            story = new StoryState();
            inventory = new Inventory(Resources.Load<ItemCatalog>(ItemCatalog.ResourcePath));
            progression = new Progression(Resources.Load<ProgressionConfig>(ProgressionConfig.ResourcePath), inventory,
                Resources.Load<BlessingCatalog>(BlessingCatalog.ResourcePath), Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath));
            ServiceLocator.Register(story);
            ServiceLocator.Register(inventory);
            ServiceLocator.Register(progression);
            ServiceLocator.Register(new RewardService(inventory, progression, story));

            yield return SceneManager.LoadSceneAsync(City, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(City));
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (bootStory != null) ServiceLocator.Register(bootStory);
            if (bootInventory != null) ServiceLocator.Register(bootInventory);
            if (bootProgression != null) ServiceLocator.Register(bootProgression);
            if (bootRewards != null) ServiceLocator.Register(bootRewards);
            var scene = SceneManager.GetSceneByName(City);
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        static DialogueInteractable Npc(string id) => Object.FindObjectsByType<DialogueInteractable>(FindObjectsSortMode.None)
            .First(dialogue => dialogue.Npc != null && dialogue.Npc.Id == id);

        [UnityTest]
        public IEnumerator Healer_TeachesTheCacaoRecipe_WhenListenedTo_OnlyOnce_AndOnlyToListeners()
        {
            var healer = Npc("curandera");
            Assert.IsTrue(progression.SelectGuardian("aguila"));
            Assert.AreEqual("Indagar", healer.PathVerb, "Itzel indaga.");
            Assert.IsTrue(progression.SelectGuardian("ajolote"));
            Assert.AreEqual("Escuchar", healer.PathVerb, "Xali escucha.");

            healer.PathInteract(healer.transform);
            var lines = 1;
            while (healer.Advance())
                lines++;
            healer.End();
            Assert.Greater(lines, 1, "Dice sus líneas de la acción, no las de siempre.");
            Assert.IsTrue(story.HasFlag("receta.bebida_cacao"), "Al terminar enseña la receta.");
            Assert.IsNull(healer.PathVerb, "Una sola vez por partida.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hearth_IsNextToTheHealer_AndCooksFoodWithItsOwnRecipes()
        {
            var hearth = Object.FindObjectsByType<ServicePoint>(FindObjectsSortMode.None).FirstOrDefault(point => point.Prompt == "Cocinar");
            Assert.IsNotNull(hearth, "Falta el fogón de la curandera.");
            Assert.Less(Vector3.Distance(hearth.transform.position, Npc("curandera").transform.position), 3f, "Junto a la curandera.");

            var economy = Resources.Load<EconomyCatalog>(EconomyCatalog.ResourcePath);
            var cooking = economy.Recipes.Where(recipe => recipe.Station == CraftStation.Cocina).ToList();
            CollectionAssert.AreEquivalent(new[] { "bebida_maiz", "pescado_hoja", "bebida_cacao" }, cooking.Select(recipe => recipe.Id));
            Assert.IsTrue(economy.Recipes.Any(recipe => recipe.Id == "amuleto_jade" && recipe.Station == CraftStation.Artesano),
                "Las del artesano siguen en su taller.");

            var corn = cooking.First(recipe => recipe.Id == "bebida_maiz");
            inventory.Add("maiz", 2);
            Assert.IsTrue(Economy.Craft(corn, inventory, story));
            Assert.AreEqual(1, inventory.Count("bebida_maiz"));
            Assert.AreEqual(0, inventory.Count("maiz"));

            var cacao = cooking.First(recipe => recipe.Id == "bebida_cacao");
            Assert.IsFalse(Economy.IsUnlocked(cacao.UnlockFlag, story), "La de cacao hay que aprenderla.");
            yield return null;
        }

        [Test]
        public void EveryPathResponse_HasLines_KnownItems_AndAnNpcInTheCity()
        {
            var catalog = Resources.Load<PathActionCatalog>(PathActionCatalog.ResourcePath);
            Assert.IsNotNull(catalog, "Falta Resources/Narrativa/AccionesSenda (Crónicas → Crear acciones de senda).");
            var items = Resources.Load<ItemCatalog>(ItemCatalog.ResourcePath);
            foreach (var response in catalog.Responses)
            {
                Assert.IsNotEmpty(response.lines, response.DoneKey);
                foreach (var item in response.items)
                    Assert.IsNotNull(items.Get(item.id), $"{response.DoneKey}: objeto desconocido {item.id}.");
            }
            foreach (PathAction action in System.Enum.GetValues(typeof(PathAction)))
                Assert.GreaterOrEqual(catalog.Responses.Count(response => response.action == action), 3, $"Pocas respuestas para {action}.");
        }

        [UnityTest]
        public IEnumerator EveryPathResponse_PointsToAnNpcOfTheCity()
        {
            var ids = Object.FindObjectsByType<DialogueInteractable>(FindObjectsSortMode.None)
                .Where(dialogue => dialogue.Npc != null).Select(dialogue => dialogue.Npc.Id).ToHashSet();
            foreach (var response in Resources.Load<PathActionCatalog>(PathActionCatalog.ResourcePath).Responses)
                Assert.IsTrue(ids.Contains(response.npcId), $"Nadie en la ciudad tiene el id {response.npcId}.");
            yield return null;
        }
    }
}
