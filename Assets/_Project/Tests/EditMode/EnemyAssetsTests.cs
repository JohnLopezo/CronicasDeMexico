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
        [TestCase("Serpiente", "serpiente", false, EnemyDefense.GuardAfterHit, EnemySpecial.SlowSpit)]
        [TestCase("Cocodrilo", "cocodrilo", false, EnemyDefense.FrontArmor, EnemySpecial.TailSweep)]
        [TestCase("EspirituJaguar", "espiritu_jaguar", false, EnemyDefense.Phase, EnemySpecial.DoubleSwipe)]
        [TestCase("Mono", "mono", false, EnemyDefense.LeapBack, EnemySpecial.FanThrow)]
        [TestCase("Avispa", "avispa", true, EnemyDefense.Dodge, EnemySpecial.None)]
        [TestCase("Guacamaya", "guacamaya", true, EnemyDefense.FlyHigh, EnemySpecial.SlowScreech)]
        [TestCase("Murcielago", "murcielago", true, EnemyDefense.Dodge, EnemySpecial.SlowScreech)]
        public void Enemy_HasDataAndEveryAnimation(string name, string id, bool flies, EnemyDefense defense, EnemySpecial special)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/_Project/Data/Enemies/ED_{name}.asset");
            Assert.IsNotNull(data, name);
            Assert.AreEqual(id, data.id);
            Assert.AreEqual(flies, data.flies, "Vuela quien tiene la pose Volar en la lámina.");
            Assert.AreEqual(defense, data.defense);
            Assert.AreEqual(special, data.special);
            Assert.AreEqual(name == "Avispa", data.swarm, "Solo las avispas atacan en enjambre.");
            Assert.AreEqual(name == "Murcielago", data.attackDrains, "Solo el murciélago se cura al morder.");
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

        // Cada animación con todos sus cuadros, de su enemigo y en el orden de la lámina, igual en las cinco direcciones:
        // si falta un cuadro o se cuela otro enemigo, la animación se ve incompleta (revisado a petición del autor).
        [TestCase("Serpiente", false)]
        [TestCase("Cocodrilo", false)]
        [TestCase("EspirituJaguar", false)]
        [TestCase("Mono", false)]
        [TestCase("Avispa", true)]
        [TestCase("Guacamaya", true)]
        [TestCase("Murcielago", true)]
        public void EnemyAnimations_HaveEveryFrame_InEveryDirection(string name, bool flies)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/_Project/Data/Enemies/ED_{name}.asset");
            var expected = new (DirectionalSpriteSet set, string[] poses)[]
            {
                (data.walk, flies ? new[] { "Reposo", "Volar" } : new[] { "Reposo", "Caminar" }),
                (data.run, flies ? new[] { "Ataque" } : new[] { "Caminar", "Correr" }),
                (data.attack, new[] { "Ataque" }),
                (data.guard, new[] { "Defensa" }),
                (data.hit, new[] { "Dano" }),
                (data.death, new[] { "Dano", "Muerte" }),
            };
            foreach (var (set, poses) in expected)
            {
                var first = set.GetOnceFrame(0, 0f);
                for (var direction = 0; direction < DirectionalSpriteSet.DirectionCount; direction++)
                {
                    StringAssert.StartsWith($"Enemigo_{name}_Reposo", set.GetIdle(direction).name, $"{set.name}: reposo");
                    for (var frame = 0; frame < poses.Length; frame++)
                    {
                        var sprite = set.GetOnceFrame(direction, (frame + 0.5f) / set.FramesPerSecond);
                        Assert.IsNotNull(sprite, $"{set.name}, dirección {direction}, cuadro {frame + 1}: falta.");
                        Assert.AreEqual($"Enemigo_{name}_{poses[frame]}1_03", sprite.name, $"{set.name}, dirección {direction}, cuadro {frame + 1}.");
                    }
                }
                Assert.AreEqual(poses.Length / set.FramesPerSecond, set.Duration, 0.001f, $"{set.name}: ni cuadros de más ni de menos.");
                Assert.IsNotNull(first);
            }
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
