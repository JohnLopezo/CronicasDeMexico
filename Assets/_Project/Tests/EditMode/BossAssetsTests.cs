using CronicasDeMexico.Data.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    /// <summary>Los jefes de la arena de pruebas tienen sus datos, sus habilidades de la lámina y sus sprites.</summary>
    public class BossAssetsTests
    {
        [TestCase("SerpienteEmplumada", 4, false)]
        [TestCase("SenorLluvia", 4, false)]
        [TestCase("MuroCraneos", 3, true)] // la cuarta, Resurrección, es pasiva (resurrectFraction)
        [TestCase("PerroFuego", 4, false)]
        [TestCase("SenorMuertos", 4, false)]
        [TestCase("GuardianOfrenda", 4, false)]
        public void Boss_HasItsAbilitiesAndSprites(string key, int abilities, bool resurrects)
        {
            var data = AssetDatabase.LoadAssetAtPath<BossData>($"Assets/_Project/Data/Enemies/BD_{key}.asset");
            Assert.IsNotNull(data, key);
            StringAssert.StartsWith("Eco ", data.displayName, "Son ecos ficticios, no dioses documentados (GDD §3).");
            Assert.That(data.abilities.Length, Is.EqualTo(abilities));
            Assert.AreEqual(resurrects, data.resurrectFraction > 0f);
            foreach (var ability in data.abilities)
            {
                Assert.That(ability.range, Is.GreaterThan(0f), ability.name);
                if (ability.kind == BossAbilityKind.Summon)
                    Assert.IsNotNull(data.summon, $"{key}: {ability.name} necesita a quién invocar.");
                else if (ability.kind != BossAbilityKind.Shield)
                    Assert.That(ability.windup, Is.GreaterThan(0f), $"{ability.name}: todo golpe se avisa.");
            }
            foreach (var set in new[] { data.walk, data.attack, data.special })
                StringAssert.StartsWith($"Jefe_{key}_", set.GetOnceFrame(0, 0f).name);
        }

        [Test]
        public void BossSprites_AreImportedWithTheFeetOnThePivot()
        {
            var guids = AssetDatabase.FindAssets("Jefe_ t:Sprite", new[] { "Assets/_Project/Art/Environment/Olmeca/Sprites/Jefes" });
            Assert.That(guids.Length, Is.EqualTo(6 * 4));
            foreach (var guid in guids)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreEqual(48f, sprite.pixelsPerUnit, sprite.name);
                Assert.AreEqual(128f, sprite.rect.height, sprite.name);
                Assert.AreEqual(2f, sprite.pivot.y, 0.01f, sprite.name);
            }
        }
    }
}
