using System.Collections.Generic;
using System.Text.RegularExpressions;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>Cada guerrero seleccionable tiene todas sus animaciones, en todas las direcciones.</summary>
    public class GuerreroAssetsTests
    {
        [TestCase("jaguar", "Balam")]
        [TestCase("ajolote", "Xali")]
        [TestCase("lobo", "Naira")]
        [TestCase("mariposa", "Ameyali")]
        [TestCase("aguila", "Itzel")]
        [TestCase("quetzal", "Yaret")]
        [TestCase("xolo", "Ocel")]
        public void Guardian_HasEveryAnimation(string id, string displayName)
        {
            var guardian = AssetDatabase.LoadAssetAtPath<GuardianData>($"Assets/_Project/Data/Characters/GUA_{id}.asset");
            Assert.IsNotNull(guardian, id);
            Assert.AreEqual(id, guardian.Id);
            Assert.AreEqual(displayName, guardian.DisplayName);

            foreach (var set in new[] { guardian.SpriteSet, guardian.AttackSet, guardian.RunSet, guardian.HitSet, guardian.DeathSet })
            {
                Assert.IsNotNull(set, id);
                for (var direction = 0; direction < DirectionalSpriteSet.DirectionCount; direction++)
                {
                    Assert.IsNotNull(set.GetIdle(direction), set.name);
                    StringAssert.Contains($"Guerrero_{displayName}_", set.GetOnceFrame(direction, 0f).name, set.name);
                }
            }

            Assert.AreEqual(0.29f, guardian.AttackSet.Duration, 0.001f, "El ataque dura lo mismo con uno o dos cuadros.");
        }

        [Test]
        public void OnlyThePilot_HasDefenseAndVictoryPoses()
        {
            var catalog = Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath);
            foreach (var guardian in catalog.Guardians)
            {
                var pilot = guardian.Id == "jaguar";
                Assert.AreEqual(pilot, guardian.DefenseSet != null, guardian.Id);
                Assert.AreEqual(pilot, guardian.VictorySet != null, guardian.Id);
                Assert.AreEqual(pilot, guardian.SpecialSet != null, guardian.Id);
            }
        }

        [Test]
        public void Everyone_WalksForwardAndBackward_WithFourFrames()
        {
            foreach (var guardian in Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath).Guardians)
                foreach (var direction in new[] { 0, DirectionalSpriteSet.DirectionCount - 1 })
                {
                    var frames = new HashSet<Sprite>();
                    for (var frame = 0; frame < 4; frame++)
                        frames.Add(guardian.SpriteSet.GetWalkFrame(direction, (frame + 0.5f) / guardian.SpriteSet.FramesPerSecond));
                    Assert.AreEqual(4, frames.Count, $"{guardian.Id}, dirección {direction}");
                }
        }

        [TestCase("jaguar", Affinity.Tierra)]
        [TestCase("lobo", Affinity.Viento)]
        [TestCase("aguila", Affinity.Luz)]
        public void Affinity_IsTheOneOnItsSheet(string id, Affinity affinity) =>
            Assert.AreEqual(affinity, AssetDatabase.LoadAssetAtPath<GuardianData>($"Assets/_Project/Data/Characters/GUA_{id}.asset").InitialAffinity);

        const float BudgetPoints = 150f, GrowthPoints = 18.5f;

        // Lo que vale un reparto de atributos (Docs/Sistemas/Sistema_Progresion.md).
        static float Points(CharacterStats stats) => stats.maxHealth + 5f * (stats.attack + stats.defense) + 0.5f * stats.maxEnergy;

        [Test]
        public void Guardians_ShareTheSameBudget_ButNotTheSameStats()
        {
            var seen = new HashSet<string>();
            foreach (var guardian in Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath).Guardians)
            {
                CharacterStats one = guardian.At(1), twentyone = guardian.At(21);
                Assert.AreEqual(BudgetPoints, Points(one), 0.01f, guardian.Id);
                // A 20 niveles de distancia, para que el redondeo hacia abajo de cada nivel no cuente.
                Assert.AreEqual(BudgetPoints + 20f * GrowthPoints, Points(twentyone), 0.01f, guardian.Id);
                Assert.IsTrue(seen.Add($"{one.maxHealth}/{one.attack}/{one.defense}/{one.maxEnergy}"), $"{guardian.Id} repite atributos.");
            }
        }

        [Test]
        public void HiddenOffering_GivesOnlyTheChosenGuardiansMark()
        {
            var catalog = Resources.Load<ItemCatalog>(ItemCatalog.ResourcePath);
            var progression = new Progression(Resources.Load<ProgressionConfig>(ProgressionConfig.ResourcePath), null, null, Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath));
            Assert.IsTrue(progression.SelectGuardian("xolo"));
            var inventory = new Inventory(catalog);
            var rewards = new RewardService(inventory, progression, null);

            var package = AssetDatabase.LoadAssetAtPath<RewardBundle>("Assets/_Project/Data/Rewards/Olmeca/REC_ofrenda_p2_oculta.asset").ToPackage(new System.Random(1));
            Assert.IsTrue(rewards.Grant(package));

            Assert.IsTrue(inventory.Has("marca_xolo"));
            Assert.IsFalse(inventory.Has("marca_jaguar"));
            Assert.IsTrue(inventory.Has("esencia_energia"), "Lo demás de la ofrenda se entrega igual.");
            Assert.AreEqual(2, package.Items.Count, "El mensaje del cofre describe el paquete: no debe nombrar las otras marcas.");
        }

        static Progression NewProgression() =>
            new(Resources.Load<ProgressionConfig>(ProgressionConfig.ResourcePath), null, null, Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath));

        [Test]
        public void ChosenGuardian_IsSaved_AndRestored()
        {
            var chosen = NewProgression();
            Assert.AreEqual("jaguar", chosen.Profile.GuardianId, "Sin elegir, el de la configuración.");
            Assert.IsTrue(chosen.SelectGuardian("xolo"));
            var data = new SaveData();
            chosen.Capture(data);
            Assert.AreEqual("xolo", data.guardianId);

            var loaded = NewProgression();
            loaded.Restore(data);
            Assert.AreEqual("xolo", loaded.Profile.GuardianId);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("jaguar")]
        public void OldSaves_ContinueAsBalam(string savedId)
        {
            var loaded = NewProgression();
            loaded.SelectGuardian("xolo");
            loaded.Restore(new SaveData { guardianId = savedId, level = 1 });
            Assert.AreEqual("jaguar", loaded.Profile.GuardianId);
        }

        [Test]
        public void UnknownGuardian_IsRejected_AndAnUnknownSaveFallsBack()
        {
            var progression = NewProgression();
            Assert.IsFalse(progression.SelectGuardian("no_existe"));
            Assert.IsFalse(progression.SelectGuardian(null));
            Assert.AreEqual("jaguar", progression.Profile.GuardianId);

            LogAssert.Expect(LogType.Warning, new Regex("no_existe"));
            progression.Restore(new SaveData { guardianId = "no_existe", level = 1 });
            Assert.AreEqual("jaguar", progression.Profile.GuardianId);
        }
    }
}
