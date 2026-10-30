using System.Collections;
using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.Gameplay.Rewards;
using CronicasDeMexico.UI.Hud;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Recompensas en la mazmorra olmeca generada (M5): ofrendas por piso, botín de enemigos con protección contra el
    /// farmeo, golpe de tierra que rompe el basalto agrietado y el cierre de la mazmorra (Bendición de la Tierra,
    /// ofrenda final, nota y elección de Bendición secundaria). Usa servicios propios y deja los de Boot como estaban.
    /// </summary>
    public class OlmecaRecompensasTests
    {
        static string Floor(int n) => $"Olmeca_Mazmorra_P{n}";

        StoryState bootStory;
        QuestService bootQuests;
        Inventory bootInventory;
        Progression bootProgression;
        RewardService bootRewards;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.TryGet(out bootStory);
            ServiceLocator.TryGet(out bootQuests);
            ServiceLocator.TryGet(out bootInventory);
            ServiceLocator.TryGet(out bootProgression);
            ServiceLocator.TryGet(out bootRewards);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (bootStory != null) ServiceLocator.Register(bootStory);
            if (bootQuests != null) ServiceLocator.Register(bootQuests);
            if (bootInventory != null) ServiceLocator.Register(bootInventory);
            if (bootProgression != null) ServiceLocator.Register(bootProgression);
            if (bootRewards != null) ServiceLocator.Register(bootRewards);
        }

        [UnityTearDown]
        public IEnumerator UnloadRegions()
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("Olmeca_"))
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        sealed class Own
        {
            public StoryState Story;
            public Inventory Inventory;
            public Progression Progression;
            public RewardService Rewards;
        }

        static Own OwnServices()
        {
            var story = new StoryState();
            var quests = new QuestService(story, Resources.Load<QuestCatalog>(QuestCatalog.ResourcePath));
            var inventory = new Inventory(Resources.Load<ItemCatalog>(ItemCatalog.ResourcePath));
            var progression = new Progression(Resources.Load<ProgressionConfig>(ProgressionConfig.ResourcePath), inventory,
                Resources.Load<BlessingCatalog>(BlessingCatalog.ResourcePath));
            var rewards = new RewardService(inventory, progression, story, quests) { Random = new System.Random(5) };
            ServiceLocator.Register(story);
            ServiceLocator.Register(quests);
            ServiceLocator.Register(inventory);
            ServiceLocator.Register(progression);
            ServiceLocator.Register(rewards);
            return new Own { Story = story, Inventory = inventory, Progression = progression, Rewards = rewards };
        }

        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            yield return null;
            yield return null; // ActiveWhenFlags espera a StoryState en su primer Update
        }

        static void Teleport(Component character, Vector3 position)
        {
            var controller = character.GetComponent<CharacterController>();
            controller.enabled = false;
            character.transform.position = position;
            controller.enabled = true;
        }

        static RewardChest[] Chests() =>
            Object.FindObjectsByType<RewardChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        static RewardChest Chest(string id) => Chests().Single(c => c.ChestId == id);

        static PlayerMovement Player() => Object.FindAnyObjectByType<PlayerMovement>();

        [UnityTest]
        public IEnumerator Floors_HaveTheirOfferingsCrackedBasaltAndEnemyLoot()
        {
            OwnServices();
            var expected = new[]
            {
                new[] { "p1_comun", "p1_especial", "p1_oculta" },
                new[] { "p2_especial", "p2_oculta", "p2_guardian" },
                new[] { "p3_oculta", "p3_final" },
            };
            for (var n = 1; n <= 3; n++)
            {
                yield return Load(Floor(n));
                CollectionAssert.AreEquivalent(expected[n - 1], Chests().Select(c => c.ChestId), $"Piso {n}: ofrendas.");
                Assert.IsNotNull(Object.FindAnyObjectByType<BreakableBasalt>(), $"Piso {n}: falta el basalto agrietado.");
                Assert.IsNotNull(Object.FindAnyObjectByType<RewardOnEnter>(), $"Piso {n}: falta la recompensa por descubrir.");
                foreach (var enemy in Object.FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                             .Where(h => h.GetComponent<PlayerMovement>() == null && h.GetComponent<StoneGuardian>() == null))
                    Assert.IsNotNull(enemy.GetComponent<EnemyReward>()?.Table, $"{enemy.name} sin botín.");
                Assert.IsNotNull(Player().GetComponent<PlayerAbility>(), "El jugador no tiene el golpe de tierra.");
                Assert.IsNotNull(Player().GetComponent<PlayerStats>());
                yield return SceneManager.UnloadSceneAsync(Floor(n));
            }
        }

        [UnityTest]
        public IEnumerator Offering_GivesItsRewardOnce_AndShowsOpen()
        {
            var own = OwnServices();
            yield return Load(Floor(1));
            var chest = Chest("p1_comun");
            var player = Player().transform;

            chest.Interact(player);
            chest.End();
            yield return null;
            var seeds = own.Inventory.Count("semilla_preparada");
            Assert.GreaterOrEqual(seeds, 1, "la ofrenda común da al menos una semilla preparada");
            Assert.IsTrue(own.Story.HasFlag(chest.Flag));
            var group = chest.transform.parent;
            Assert.IsFalse(group.Find("Cerrada").gameObject.activeSelf);
            Assert.IsTrue(group.Find("Abierta").gameObject.activeSelf);

            var before = own.Inventory.Items.Sum(own.Inventory.Count);
            chest.Interact(player);
            chest.End();
            Assert.AreEqual(before, own.Inventory.Items.Sum(own.Inventory.Count), "una ofrenda abierta no vuelve a dar");
        }

        [UnityTest]
        public IEnumerator EnemyLoot_GivesExperience_AndLessWhenRepeated()
        {
            var own = OwnServices();
            yield return Load(Floor(1));
            var spirit = Object.FindObjectsByType<EnemyReward>(FindObjectsSortMode.None).OrderBy(e => e.name).First();
            var health = spirit.GetComponent<Health>();

            var before = own.Progression.Experience;
            health.TryDamage(1000, spirit.transform.position);
            Assert.AreEqual(15, own.Progression.Experience - before, "espíritu corrompido: 15 de experiencia en su zona");
            Assert.AreEqual(1, own.Progression.DefeatCount(spirit.RepeatKey));

            // Al reaparecer (otra visita), el mismo espíritu da la mitad.
            var again = own.Progression.AddExperience(15, spirit.ZoneLevel, spirit.RepeatKey);
            Assert.That(again, Is.InRange(7, 8));
        }

        [UnityTest]
        public IEnumerator EarthStrike_NeedsTheBlessingAndEnergy_AndBreaksTheCrackedBasalt()
        {
            var own = OwnServices();
            yield return Load(Floor(1));
            var player = Player();
            var ability = player.GetComponent<PlayerAbility>();
            var basalt = Object.FindAnyObjectByType<BreakableBasalt>();
            Teleport(player, IsometricProjection.FromPipeline(5.0f, 3.5f, 0f));
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(ability.TryUse(), "sin la Bendición de la Tierra no hay golpe de tierra");
            own.Progression.UnlockBlessing("tierra");
            var energy = own.Progression.Energy;

            Assert.IsTrue(ability.TryUse());
            Assert.AreEqual(energy - 30, own.Progression.Energy, "cuesta 30 de energía");
            Assert.IsTrue(own.Story.HasFlag(basalt.Flag), "el basalto agrietado se rompe");
            yield return null;
            Assert.IsFalse(basalt.gameObject.activeSelf);
            Assert.IsFalse(ability.TryUse(), "tiene enfriamiento");
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(energy - 30, own.Progression.Energy, "la energía no se regenera sola");
        }

        [UnityTest]
        public IEnumerator FreedGuardian_GivesTheEarthBlessing_AndTheFinalOfferingClosesTheDungeon()
        {
            var own = OwnServices();
            own.Story.SetFlag("olm.guardian_liberado");
            own.Story.SetFlag(RewardChest.FlagPrefix + "p1_especial");
            own.Story.SetFlag(RewardChest.FlagPrefix + "p2_especial");
            yield return Load(Floor(3));
            yield return null;

            Assert.AreEqual("tierra", own.Progression.PrimaryBlessing, "Bendición de la Tierra obligatoria");
            Assert.AreEqual(1, own.Inventory.Count("fragmento_primera_memoria"));
            Assert.IsTrue(own.Story.HasFlag(StoryEvents.QuestActiveFlag("MP_OLM_06")), "empieza el regreso al asentamiento");

            var chest = Chest("p3_final");
            Assert.IsTrue(chest.gameObject.activeInHierarchy, "la ofrenda final aparece al liberar al Guardián");
            chest.Interact(Player().transform);
            chest.End();
            yield return null;

            Assert.AreEqual(1, own.Inventory.Count("corazon_piedra_viva"));
            var result = own.Rewards.LastResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.SecretsFound);
            Assert.AreEqual("A", result.Grade, "dos de tres secretos y sin caer");
            Assert.IsTrue(GameMenuController.IsOpen, "se abre la elección de Bendición");
            var menu = Object.FindAnyObjectByType<GameMenuController>();
            Assert.AreEqual(MenuTabs.BlessingChoice, menu.CurrentTab);
            Assert.AreEqual(0f, Time.timeScale, "el menú pausa el juego");
            menu.Close();
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
