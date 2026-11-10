using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    /// <summary>Cada enemigo de la lámina tiene sus datos, sus sprites importados al escenario y todas sus animaciones.</summary>
    public class EnemyAssetsTests
    {
        [TestCase("Serpiente", "serpiente", false, EnemyDefense.GuardAfterHit)]
        [TestCase("Cocodrilo", "cocodrilo", false, EnemyDefense.FrontArmor)]
        [TestCase("EspirituJaguar", "espiritu_jaguar", false, EnemyDefense.Phase)]
        [TestCase("Mono", "mono", false, EnemyDefense.LeapBack)]
        [TestCase("Avispa", "avispa", true, EnemyDefense.Dodge)]
        [TestCase("Guacamaya", "guacamaya", true, EnemyDefense.FlyHigh)]
        [TestCase("Murcielago", "murcielago", true, EnemyDefense.Dodge)]
        public void Enemy_HasDataAndEveryAnimation(string name, string id, bool flies, EnemyDefense defense)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/_Project/Data/Enemies/ED_{name}.asset");
            Assert.IsNotNull(data, name);
            Assert.AreEqual(id, data.id);
            Assert.AreEqual(flies, data.flies, "Vuela quien tiene la pose Volar en la lámina.");
            Assert.AreEqual(defense, data.defense);
            Assert.That(data.attackRange, Is.GreaterThan(0f));
            if (data.movement == EnemyMovement.KeepDistance)
                Assert.That(data.attackRange, Is.GreaterThanOrEqualTo(data.preferredDistance), "Debe alcanzar desde su distancia.");

            foreach (var set in new[] { data.walk, data.run, data.attack, data.guard, data.hit, data.death })
            {
                Assert.IsNotNull(set, name);
                for (var direction = 0; direction < DirectionalSpriteSet.DirectionCount; direction++)
                    StringAssert.StartsWith($"Enemigo_{name}_", set.GetOnceFrame(direction, 0f).name, set.name);
            }
            StringAssert.Contains("_Muerte", data.death.GetOnceFrame(0, data.death.Duration).name, "La caída termina tumbado.");
        }

        [Test]
        public void EnemySprites_AreImportedAtTheSceneScale_WithTheFeetOnThePivot()
        {
            var guids = AssetDatabase.FindAssets("Enemigo_ t:Sprite", new[] { "Assets/_Project/Art/Environment/Olmeca/Sprites/Enemigos" });
            Assert.That(guids.Length, Is.EqualTo(4 * 7 + 6 * 3), "7 poses de los terrestres y 6 de los voladores.");
            foreach (var guid in guids)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreEqual(48f, sprite.pixelsPerUnit, sprite.name);
                Assert.AreEqual(64f, sprite.rect.height, sprite.name);
                Assert.AreEqual(2f, sprite.pivot.y, 0.01f, $"{sprite.name}: los pies a 2 px del borde.");
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, sprite.name);
            }
        }
    }
}
