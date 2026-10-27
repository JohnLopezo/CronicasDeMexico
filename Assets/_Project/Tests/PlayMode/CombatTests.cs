using System.Collections;
using System.Reflection;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    public class CombatTests
    {
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

        int enemyLayer;
        GameObject ground;
        GameObject player;
        Health playerHealth;
        PlayerAttack attack;

        [SetUp]
        public void SetUp()
        {
            enemyLayer = LayerMask.NameToLayer("Enemigo");
            Assert.That(enemyLayer, Is.Not.EqualTo(-1), "Falta el layer 'Enemigo' en Tags and Layers.");

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);

            // Sin acción de movimiento: el jugador se queda quieto mirando hacia -Z (dirección inicial).
            player = CreateCharacter("JugadorPrueba", Vector3.zero, 0);
            player.SetActive(false);
            player.AddComponent<PlayerMovement>();
            playerHealth = player.AddComponent<Health>();
            attack = player.AddComponent<PlayerAttack>();
            typeof(PlayerAttack).GetField("targetLayers", PrivateField).SetValue(attack, (LayerMask)(1 << enemyLayer));
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(ground);
        }

        static GameObject CreateCharacter(string name, Vector3 position, int layer)
        {
            var character = new GameObject(name) { layer = layer };
            character.transform.position = position;
            var controller = character.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            controller.minMoveDistance = 0f;
            return character;
        }

        GameObject CreateDummy(Vector3 position, out Health health)
        {
            var dummy = CreateCharacter("DianaPrueba", position, enemyLayer);
            health = dummy.AddComponent<Health>();
            return dummy;
        }

        [Test]
        public void Health_TakesDamage_IgnoresHitsWhileInvulnerable_AndDiesOnce()
        {
            var deaths = 0;
            playerHealth.Died += () => deaths++;
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(playerHealth, 0f);

            Assert.IsTrue(playerHealth.TryDamage(2, Vector3.zero));
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 2));

            playerHealth.GrantInvulnerability(10f);
            Assert.IsFalse(playerHealth.TryDamage(1, Vector3.zero), "Invulnerable no debe recibir daño.");

            playerHealth.Restore();
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "Restore debe devolver la vida completa.");

            Assert.IsTrue(playerHealth.TryDamage(100, Vector3.zero));
            Assert.That(playerHealth.Current, Is.EqualTo(0), "La vida no debe bajar de 0.");
            Assert.IsFalse(playerHealth.TryDamage(1, Vector3.zero), "Muerto no debe recibir más daño.");
            Assert.That(deaths, Is.EqualTo(1), "Died debe dispararse una sola vez.");
        }

        [UnityTest]
        public IEnumerator Attack_HitsTargetInFront_NotBehind_AndNeverItself()
        {
            var front = CreateDummy(new Vector3(0f, 0f, -1f), out var frontHealth);
            var behind = CreateDummy(new Vector3(0f, 0f, 1.5f), out var behindHealth);
            yield return null;

            Assert.That(attack.Strike(), Is.EqualTo(1), "Debe golpear exactamente a un objetivo.");
            Assert.That(frontHealth.Current, Is.EqualTo(frontHealth.Max - 1), "El objetivo de delante debe recibir daño.");
            Assert.That(behindHealth.Current, Is.EqualTo(behindHealth.Max), "El de detrás no debe recibir daño.");
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "El jugador no debe golpearse a sí mismo.");

            Object.Destroy(front);
            Object.Destroy(behind);
        }

        [UnityTest]
        public IEnumerator Enemy_ChasesAndDamagesPlayer_IsKnockedBack_AndDisappearsOnDeath()
        {
            var enemy = CreateCharacter("EnemigoPrueba", new Vector3(0f, 0f, -4f), enemyLayer);
            enemy.SetActive(false);
            var enemyHealth = enemy.AddComponent<Health>();
            var chaser = enemy.AddComponent<EnemyChaser>();
            typeof(EnemyChaser).GetField("target", PrivateField).SetValue(chaser, player.transform);
            enemy.SetActive(true);

            try
            {
                // 4 m a 2.6 m/s: lo alcanza en menos de 2 s.
                yield return new WaitForSeconds(2.5f);
                Assert.IsTrue(chaser.IsChasing, "Debe perseguir al jugador dentro del radio de detección.");
                Assert.That(playerHealth.Current, Is.LessThan(playerHealth.Max), "Al tocar al jugador debe dañarlo.");

                var beforeHit = Vector3.Distance(enemy.transform.position, player.transform.position);
                Assert.That(attack.Strike(), Is.EqualTo(1), "El golpe debe alcanzar al enemigo pegado al jugador.");
                yield return new WaitForSeconds(0.2f);
                Assert.That(Vector3.Distance(enemy.transform.position, player.transform.position), Is.GreaterThan(beforeHit + 0.2f),
                    "El golpe debe empujar al enemigo hacia atrás.");

                enemyHealth.Restore(); // quita la invulnerabilidad que dejó el golpe anterior
                enemyHealth.TryDamage(100, player.transform.position);
                Assert.IsTrue(enemyHealth.IsDead);
                Assert.IsFalse(enemy.GetComponent<CharacterController>().enabled, "Muerto no debe tener collider.");
            }
            finally
            {
                Object.Destroy(enemy);
            }
        }

        [UnityTest]
        public IEnumerator Player_RespawnsWithFullHealth_AfterDying()
        {
            player.SetActive(false);
            var response = player.AddComponent<PlayerDamageResponse>();
            typeof(PlayerDamageResponse).GetField("respawnDelay", PrivateField).SetValue(response, 0.2f);
            player.SetActive(true);
            yield return null;

            // Se mueve el punto de partida para comprobar que vuelve al de Awake.
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(5f, 0f, 5f);
            controller.enabled = true;

            playerHealth.TryDamage(100, Vector3.zero);
            Assert.IsTrue(playerHealth.IsDead);
            Assert.IsFalse(player.GetComponent<PlayerMovement>().enabled, "Muerto no debe poder moverse.");

            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(playerHealth.IsDead, "Debe reaparecer tras el retardo.");
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max));
            Assert.That(Vector3.Distance(player.transform.position, Vector3.zero), Is.LessThan(0.2f), "Debe reaparecer en el punto de inicio.");
            Assert.IsTrue(player.GetComponent<PlayerMovement>().enabled);
            Assert.IsFalse(playerHealth.TryDamage(1, Vector3.zero), "Al reaparecer debe ser invulnerable un momento.");
        }
    }
}
