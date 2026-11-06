using CronicasDeMexico.Data.Characters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
    }
}
