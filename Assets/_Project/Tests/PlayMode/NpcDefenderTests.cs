using System.Collections;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Npc;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>El guardia de la aldea o el joven guerrero (NpcDefender) contra un enemigo, sobre un suelo de prueba.</summary>
    public sealed class NpcDefenderTests
    {
        const int EnemyLayer = 7;
        GameObject ground, defender, enemy;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 5f;

            // Inactivos hasta configurarlos, para que Awake y OnEnable vean los valores finales.
            defender = new GameObject("Defensor");
            defender.SetActive(false);
            defender.transform.position = Vector3.up * 0.1f;
            AddController(defender);
            defender.AddComponent<NpcDefender>().Configure(1 << EnemyLayer, 6f, null);

            enemy = new GameObject("Enemigo") { layer = EnemyLayer };
            enemy.SetActive(false);
            enemy.transform.position = new Vector3(4f, 0.1f, 0f);
            AddController(enemy);
            enemy.AddComponent<Health>();
        }

        // Como los personajes del juego: con la cápsula por defecto no llegarían a alcance de golpe, y con el
        // minMoveDistance por defecto se pierden los pasos pequeños a FPS altos (CreateCharacterRoot).
        static void AddController(GameObject character)
        {
            var controller = character.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.radius = 0.25f;
            controller.minMoveDistance = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(ground);
            Object.Destroy(defender);
            Object.Destroy(enemy);
        }

        [UnityTest]
        public IEnumerator Defender_ChasesAndDefeatsAnEnemy_ThenGoesBackToPatrolling()
        {
            var health = enemy.GetComponent<Health>();
            enemy.SetActive(true);
            defender.SetActive(true);
            var patrol = defender.GetComponent<NpcPatrol>();

            yield return new WaitForSeconds(0.5f);
            Assert.IsNotNull(patrol.ChasePoint, "No va a por el enemigo.");

            for (var elapsed = 0f; elapsed < 30f && !health.IsDead; elapsed += Time.deltaTime)
                yield return null;
            Assert.IsTrue(health.IsDead, $"No lo venció: vida {health.Current}, a {Vector3.Distance(defender.transform.position, enemy.transform.position):F2} m.");
            Assert.Less(Vector3.Distance(defender.transform.position, enemy.transform.position), 2.5f, "Lo golpeó desde lejos.");

            yield return new WaitForSeconds(0.4f);
            Assert.IsNull(patrol.ChasePoint, "Sigue persiguiendo a un enemigo vencido.");
        }

        [UnityTest]
        public IEnumerator Defender_DoesNotFightDuringAConversation()
        {
            var health = enemy.GetComponent<Health>();
            enemy.SetActive(true);
            var before = health.Current;
            defender.SetActive(true);
            defender.GetComponent<NpcPatrol>().Hold(Vector3.forward);

            enemy.transform.position = new Vector3(0.8f, 0.1f, 0f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(before, health.Current, "Golpeó en mitad de la conversación.");
        }

        [UnityTest]
        public IEnumerator Enemy_GoesForTheNearestTarget_AndCannotHurtADefender()
        {
            var player = new GameObject("JugadorDePrueba");
            player.transform.position = new Vector3(40f, 0.1f, 0f); // lejos: el defensor está más cerca
            var playerHealth = player.AddComponent<Health>();
            var before = playerHealth.Current;

            var chaser = enemy.AddComponent<EnemyChaser>();
            typeof(EnemyChaser).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(chaser, player.transform);
            enemy.SetActive(true);
            defender.SetActive(true);
            var startDistance = Vector3.Distance(enemy.transform.position, defender.transform.position);

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(chaser.IsChasing, "No persigue al defensor, que entra en su radio.");
            Assert.Less(Vector3.Distance(enemy.transform.position, defender.transform.position), startDistance);
            Assert.AreEqual(before, playerHealth.Current);

            Object.Destroy(player);
        }
    }
}
