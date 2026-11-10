using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Gameplay.Dungeon;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Sistema de recompensas (M5): inventario, reliquias, experiencia con protección contra el farmeo, energía sin
    /// regeneración, ranuras de Bendición, entregas únicas, fabricación y comercio, nota de mazmorra, guardado v2 y
    /// el contenido olmeca generado (Resources/Recompensas).
    /// </summary>
    public class RewardSystemTests
    {
        readonly List<Object> created = new();
        ItemCatalog catalog;
        BlessingCatalog blessings;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            catalog = Track(ScriptableObject.CreateInstance<ItemCatalog>());
            catalog.SetItems(new[]
            {
                Item("jade", ItemCategory.Material, 30),
                Item("fibras", ItemCategory.Material, 60),
                Item("semilla_preparada", ItemCategory.Consumible, 5),
                Item("amuleto_jade", ItemCategory.Reliquia, 1, new CharacterStats(10, 0, 2, 0)),
                Item("talisman", ItemCategory.Reliquia, 1, new CharacterStats(0, 3, 0, 0)),
                Item("collar", ItemCategory.Reliquia, 1, new CharacterStats(0, 0, 4, 0)),
                Item("marca_aguila", ItemCategory.Reliquia, 1, new CharacterStats(0, 9, 0, 0), guardianOnly: "aguila"),
                Item("figurilla", ItemCategory.Coleccionable, 1, record: "figurilla", sellable: false),
                Item("fragmento", ItemCategory.Narrativo, 1),
                Item(Inventory.Currency, ItemCategory.Moneda, 9999),
            });
            blessings = Track(ScriptableObject.CreateInstance<BlessingCatalog>());
            blessings.SetBlessings(new[]
            {
                Blessing("tierra", BlessingSlot.Principal, default),
                Blessing("fuerza", BlessingSlot.Secundaria, new CharacterStats(10, 0, 4, 0)),
                Blessing("instinto", BlessingSlot.Secundaria, new CharacterStats(0, 4, 0, 0)),
            });
        }

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

        ItemData Item(string id, ItemCategory category, int maxStack, CharacterStats bonus = default, string guardianOnly = null,
            string record = null, bool sellable = true)
        {
            var item = Track(ScriptableObject.CreateInstance<ItemData>());
            item.Configure(id, id, id, category, Rarity.Comun, Origin.Ficticio, "F", "uso", maxStack, 5,
                sellable && category != ItemCategory.Narrativo, bonus, default, guardianOnly, record);
            return item;
        }

        BlessingData Blessing(string id, BlessingSlot slot, CharacterStats bonus)
        {
            var blessing = Track(ScriptableObject.CreateInstance<BlessingData>());
            blessing.Configure(id, id, id, slot, bonus, slot == BlessingSlot.Principal ? 30 : 0, 4f, "", "", "", "olmeca");
            return blessing;
        }

        (Inventory inventory, Progression progression, RewardService rewards, StoryState story) Services()
        {
            var inventory = new Inventory(catalog);
            var progression = new Progression(DefaultProgressionConfig.Instance, inventory, blessings);
            var story = new StoryState();
            return (inventory, progression, new RewardService(inventory, progression, story), story);
        }

        // ---------- Inventario ----------

        [Test]
        public void Inventory_AddRespectsMaxStack_AndRemoveIsAllOrNothing()
        {
            var inventory = new Inventory(catalog);

            Assert.AreEqual(4, inventory.Add("semilla_preparada", 4));
            Assert.AreEqual(1, inventory.Add("semilla_preparada", 3), "el tope de los consumibles es 5");
            Assert.AreEqual(5, inventory.Count("semilla_preparada"));
            Assert.IsFalse(inventory.Remove("semilla_preparada", 6));
            Assert.AreEqual(5, inventory.Count("semilla_preparada"));
            Assert.IsTrue(inventory.Remove("semilla_preparada", 5));
            CollectionAssert.DoesNotContain(inventory.Items, "semilla_preparada");
        }

        [Test]
        public void Inventory_RelicsUseTwoSlots_AndOnlyRelicsCanBeEquipped()
        {
            var inventory = new Inventory(catalog);
            inventory.Add("amuleto_jade", 1);
            inventory.Add("talisman", 1);
            inventory.Add("collar", 1);
            inventory.Add("jade", 1);

            Assert.IsFalse(inventory.EquipRelic("jade"), "un material no se equipa");
            Assert.IsTrue(inventory.EquipRelic("amuleto_jade"));
            Assert.IsTrue(inventory.EquipRelic("talisman"));
            Assert.IsFalse(inventory.EquipRelic("collar"), "solo hay dos ranuras");
            Assert.IsTrue(inventory.UnequipRelic("talisman"));
            Assert.IsTrue(inventory.EquipRelic("collar"));
        }

        // ---------- Progresión ----------

        [Test]
        public void Progression_StartsAtScaleTimesTen()
        {
            var (_, progression, _, _) = Services();

            var stats = progression.Stats;
            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(50, stats.maxHealth);
            Assert.AreEqual(10, stats.attack);
            Assert.AreEqual(stats.maxEnergy, progression.Energy, "empieza con la energía llena");
        }

        [Test]
        public void Progression_LevelsUp_AndStatsGrow()
        {
            var (_, progression, _, _) = Services();
            var before = progression.Stats;

            progression.AddExperience(DefaultProgressionConfig.Instance.ExperienceToNext(1));

            Assert.AreEqual(2, progression.Level);
            Assert.Greater(progression.Stats.maxHealth, before.maxHealth);
            Assert.Greater(progression.Stats.attack, before.attack);
        }

        [Test]
        public void Progression_OverLevelPenalty_ReducesButNeverBelowFloor()
        {
            var (_, progression, _, _) = Services();
            progression.AddExperience(10000); // varios niveles
            Assert.Greater(progression.Level, 4);

            var nearZone = progression.AddExperience(100, zoneLevel: progression.Level - 1);
            var farBelow = progression.AddExperience(100, zoneLevel: 1);

            Assert.AreEqual(75, nearZone, "un nivel por encima: -25 %");
            Assert.AreEqual(10, farBelow, "muy por encima: el mínimo configurable (10 %)");
        }

        [Test]
        public void Progression_RepeatedEnemy_GivesLessEachTime_ThenNothing()
        {
            var (_, progression, _, _) = Services();

            var gains = Enumerable.Range(0, 4).Select(_ => progression.AddExperience(40, 0, "Piso1/Espiritu_1")).ToArray();

            CollectionAssert.AreEqual(new[] { 40, 20, 10, 0 }, gains, "factor 0,5 y corte bajo 0,2");
            Assert.AreEqual(40, progression.AddExperience(40, 0, "Piso1/Espiritu_2"), "otro enemigo cuenta aparte");
        }

        [Test]
        public void Energy_DoesNotRegenerate_AndOnlyRestoresUpToMax()
        {
            var (_, progression, _, _) = Services();
            var max = progression.Stats.maxEnergy;

            Assert.IsTrue(progression.SpendEnergy(30));
            Assert.IsFalse(progression.SpendEnergy(max), "no se gasta más de lo que hay");
            Assert.AreEqual(max - 30, progression.Energy);
            Assert.AreEqual(30, progression.RestoreEnergy(100));
            Assert.AreEqual(max, progression.Energy);
        }

        [Test]
        public void Blessings_PrimaryIsReserved_SecondaryCanChange()
        {
            var (_, progression, _, _) = Services();

            progression.UnlockBlessing("tierra");
            progression.UnlockBlessing("fuerza");
            progression.UnlockBlessing("instinto");

            Assert.AreEqual("tierra", progression.PrimaryBlessing);
            Assert.AreEqual("fuerza", progression.SecondaryBlessing, "la primera secundaria se equipa sola");
            Assert.IsFalse(progression.EquipSecondary("tierra"), "la principal no va a la ranura secundaria");
            Assert.IsTrue(progression.EquipSecondary("instinto"));
            Assert.AreEqual("tierra", progression.PrimaryBlessing, "la principal no se pierde");
            Assert.AreEqual(14, progression.Stats.attack, "ataque 10 + instinto 4");
        }

        [Test]
        public void Stats_IncludeRelics_AndRespectGuardianOnly()
        {
            var (inventory, progression, _, _) = Services();
            inventory.Add("amuleto_jade", 1);
            inventory.Add("marca_aguila", 1);
            inventory.EquipRelic("amuleto_jade");
            inventory.EquipRelic("marca_aguila");

            var stats = progression.Stats;
            Assert.AreEqual(60, stats.maxHealth);
            Assert.AreEqual(2, stats.defense);
            Assert.AreEqual(10, stats.attack, "la marca del águila no sirve al jaguar");
        }

        // ---------- Entregas ----------

        [Test]
        public void Grant_WithOnceKey_IsGivenOnlyOnce_AndSetsRecordFlags()
        {
            var (inventory, progression, rewards, story) = Services();
            var package = new RewardPackage { Experience = 20 }.Add("figurilla", 1).Add("jade", 2);
            package.Flags.Add("receta.amuleto_jade");

            Assert.IsTrue(rewards.Grant(package, "ofrenda.p1_especial"));
            Assert.IsFalse(rewards.Grant(package, "ofrenda.p1_especial"));

            Assert.AreEqual(2, inventory.Count("jade"));
            Assert.AreEqual(20, progression.Experience);
            Assert.IsTrue(story.HasFlag(RewardService.RecordPrefix + "figurilla"), "el coleccionable abre su entrada del Museo");
            Assert.IsTrue(story.HasFlag("receta.amuleto_jade"));
        }

        [Test]
        public void Grant_Blessing_NotifiesAndUnlocks()
        {
            var (_, progression, rewards, _) = Services();
            var notices = new List<RewardNotice>();
            rewards.Notified += notices.Add;

            rewards.Grant(new RewardPackage { Blessing = "tierra" }, "jefe.olmeca");

            Assert.AreEqual("tierra", progression.PrimaryBlessing);
            Assert.IsTrue(notices.Any(n => n.Kind == RewardNoticeKind.Blessing && n.Id == "tierra"));
        }

        [Test]
        public void QuestReward_IsGrantedWhenQuestCompletes()
        {
            var inventory = new Inventory(catalog);
            var progression = new Progression(DefaultProgressionConfig.Instance, inventory, blessings);
            var story = new StoryState();
            var quests = new QuestService(story);
            var rewards = new RewardService(inventory, progression, story, quests);
            var bundle = Track(ScriptableObject.CreateInstance<RewardBundle>());
            bundle.Configure(40, new[] { new RewardBundle.Stack(catalog.Get("jade"), 3) });
            var quest = Track(QuestData.Create("MP_PRUEBA", "Prueba",
                new[] { new QuestStep(StoryEvents.Talk("anciana"), "Habla con la anciana") }));
            quest.RewardBundle = bundle;

            quests.Start(quest);
            story.Report(StoryEvents.Talk("anciana"));

            Assert.AreEqual(3, inventory.Count("jade"));
            Assert.AreEqual(40, progression.Experience);
            Assert.IsTrue(rewards.WasGranted("mision.MP_PRUEBA"));
        }

        [Test]
        public void LootTable_GuaranteedAlways_AndChanceRolls()
        {
            var table = Track(ScriptableObject.CreateInstance<LootTable>());
            table.Configure(15, new[] { new LootTable.Entry(catalog.Get("jade"), 1f, 1, 1) },
                new[] { new LootTable.Entry(catalog.Get("fibras"), 0.5f, 1, 2) });

            var random = new System.Random(7);
            var withFibers = 0;
            for (var i = 0; i < 200; i++)
            {
                var package = table.Roll(random);
                Assert.AreEqual(15, package.Experience);
                Assert.IsTrue(package.Items.Any(s => s.id == "jade" && s.count == 1));
                if (package.Items.Any(s => s.id == "fibras"))
                    withFibers++;
            }
            Assert.That(withFibers, Is.InRange(70, 130), "≈50 %");
        }

        // ---------- Fabricación y comercio ----------

        [Test]
        public void Craft_NeedsUnlockAndAllMaterials_AndConsumesThem()
        {
            var inventory = new Inventory(catalog);
            var story = new StoryState();
            var recipe = Track(ScriptableObject.CreateInstance<RecipeData>());
            recipe.Configure("amuleto_jade", catalog.Get("amuleto_jade"), 1,
                new[] { new RewardBundle.Stack(catalog.Get("jade"), 2), new RewardBundle.Stack(catalog.Get("fibras"), 2) },
                "receta.amuleto_jade");
            inventory.Add("jade", 2);
            inventory.Add("fibras", 1);

            Assert.IsFalse(Economy.CanCraft(recipe, inventory, story), "receta bloqueada");
            story.SetFlag("receta.amuleto_jade");
            Assert.IsFalse(Economy.CanCraft(recipe, inventory, story), "falta una fibra");
            Assert.AreEqual(1, Economy.Missing(recipe, inventory).Single().count);
            inventory.Add("fibras", 1);
            Assert.IsTrue(Economy.Craft(recipe, inventory, story));
            Assert.AreEqual(1, inventory.Count("amuleto_jade"));
            Assert.AreEqual(0, inventory.Count("jade"));
            Assert.AreEqual(0, inventory.Count("fibras"));
        }

        [Test]
        public void Trade_BuyWithEcosOrBarter_AndUniqueItemsCannotBeSold()
        {
            var inventory = new Inventory(catalog);
            var offer = Track(ScriptableObject.CreateInstance<TradeOfferData>());
            offer.Configure("trueque_jade", catalog.Get("jade"), 1, catalog.Get("fibras"), 3);
            inventory.Add("fibras", 3);

            Assert.IsTrue(Economy.Buy(offer, inventory, null));
            Assert.AreEqual(1, inventory.Count("jade"));
            Assert.AreEqual(0, inventory.Count("fibras"));

            inventory.Add("fragmento", 1);
            inventory.Add("figurilla", 1);
            inventory.Add("amuleto_jade", 1);
            inventory.EquipRelic("amuleto_jade");
            Assert.IsFalse(Economy.CanSell("fragmento", inventory), "narrativo");
            Assert.IsFalse(Economy.CanSell("figurilla", inventory), "coleccionable no vendible");
            Assert.IsFalse(Economy.CanSell("amuleto_jade", inventory), "equipado");
            Assert.IsTrue(Economy.Sell("jade", inventory));
            Assert.AreEqual(5, inventory.Count(Inventory.Currency));
        }

        // ---------- Mazmorra ----------

        [TestCase(3, 3, false, "S")]
        [TestCase(3, 3, true, "A")]
        [TestCase(2, 3, false, "A")]
        [TestCase(1, 3, true, "B")]
        [TestCase(0, 3, true, "C")]
        public void DungeonGrade(int found, int total, bool fallen, string expected) =>
            Assert.AreEqual(expected, DungeonCompletion.Grade(found, total, fallen));

        // ---------- Guardado ----------

        [Test]
        public void Save_RoundTrip_KeepsInventoryProgressAndGrantedRewards()
        {
            var folder = Path.Combine(Path.GetTempPath(), "CronicasPruebaRecompensas_" + Guid.NewGuid().ToString("N"));
            try
            {
                var (inventory, progression, rewards, story) = Services();
                var save = new SaveService(folder);
                save.Register(story);
                save.Register(inventory);
                save.Register(progression);
                save.Register(rewards);
                inventory.Add("amuleto_jade", 1);
                inventory.EquipRelic("amuleto_jade");
                rewards.Grant(new RewardPackage { Experience = 150, Blessing = "tierra" }.Add("jade", 4), "jefe.olmeca");
                progression.SpendEnergy(30);
                progression.AddExperience(40, 0, "P1/Eco");
                Assert.IsTrue(save.Save());

                var (inventory2, progression2, rewards2, story2) = Services();
                var load = new SaveService(folder);
                load.Register(story2);
                load.Register(inventory2);
                load.Register(progression2);
                load.Register(rewards2);
                Assert.IsTrue(load.Load());

                Assert.AreEqual(4, inventory2.Count("jade"));
                Assert.IsTrue(inventory2.IsEquipped("amuleto_jade"));
                Assert.AreEqual(progression.Level, progression2.Level);
                Assert.AreEqual(progression.Experience, progression2.Experience);
                Assert.AreEqual(progression.Energy, progression2.Energy);
                Assert.AreEqual("tierra", progression2.PrimaryBlessing);
                Assert.AreEqual(1, progression2.DefeatCount("P1/Eco"));
                Assert.IsTrue(rewards2.WasGranted("jefe.olmeca"));
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [Test]
        public void Save_FromVersion1_MigratesToLevelOneWithFullEnergy()
        {
            var folder = Path.Combine(Path.GetTempPath(), "CronicasPruebaMigracion_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(folder);
                var probe = new SaveService(folder);
                File.WriteAllText(probe.MainPath,
                    "{\"version\":1,\"scene\":\"Olmeca_Ciudad\",\"flags\":[\"olm.juramento\"],\"quests\":[]}");
                var (inventory, progression, _, story) = Services();
                probe.Register(story);
                probe.Register(inventory);
                probe.Register(progression);

                Assert.IsTrue(probe.Load());
                Assert.IsTrue(story.HasFlag("olm.juramento"));
                Assert.AreEqual(1, progression.Level);
                Assert.AreEqual(progression.Stats.maxEnergy, progression.Energy);
                Assert.AreEqual(0, inventory.Items.Count);
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        // ---------- Contenido olmeca (generado por OlmecaRewardAssets) ----------

        [Test]
        public void OlmecaContent_IsCompleteAndConsistent()
        {
            var items = Resources.Load<ItemCatalog>(ItemCatalog.ResourcePath);
            var blessingCatalog = Resources.Load<BlessingCatalog>(BlessingCatalog.ResourcePath);
            var economy = Resources.Load<EconomyCatalog>(EconomyCatalog.ResourcePath);
            var museum = Resources.Load<MuseumCatalog>(MuseumCatalog.ResourcePath);
            var config = Resources.Load<ProgressionConfig>(ProgressionConfig.ResourcePath);
            Assert.IsNotNull(items, "genera las escenas olmecas para crear el contenido");
            Assert.IsNotNull(blessingCatalog);
            Assert.IsNotNull(economy);
            Assert.IsNotNull(museum);
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.Guardian);

            foreach (var item in items.Items)
            {
                Assert.IsNotNull(item.Icon, $"{item.Id} sin icono");
                Assert.IsNotEmpty(item.OriginNote, $"{item.Id} sin nota de origen (D/I/F)");
            }
            var ecos = items.Get(Inventory.Currency);
            Assert.AreEqual(Origin.Ficticio, ecos.Origin);
            StringAssert.Contains("NO representan una moneda histórica", ecos.OriginNote);
            Assert.IsFalse(items.Get("fragmento_primera_memoria").Sellable);

            var earth = blessingCatalog.Get("tierra");
            Assert.AreEqual(BlessingSlot.Principal, earth.Slot);
            Assert.AreEqual(3, blessingCatalog.SecondaryChoices("olmeca").Count(), "elección de una entre tres");

            foreach (var recipe in economy.Recipes)
            {
                Assert.IsNotNull(items.Get(recipe.ResultId), recipe.Id);
                foreach (var ingredient in recipe.Ingredients)
                    Assert.IsNotNull(items.Get(ingredient.id), $"{recipe.Id}: {ingredient.id}");
            }
            foreach (var offer in economy.Offers)
            {
                Assert.IsNotNull(items.Get(offer.ItemId), offer.Id);
                Assert.IsNotNull(items.Get(offer.PriceId), offer.Id);
            }
            Assert.IsTrue(economy.Offers.Any(o => o.PriceId != Inventory.Currency), "hay trueque de materiales");
            Assert.AreEqual("olmeca", museum.Cultures.Single().Id);
        }
    }
}
