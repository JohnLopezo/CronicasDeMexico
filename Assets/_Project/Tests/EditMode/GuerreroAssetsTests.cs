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
