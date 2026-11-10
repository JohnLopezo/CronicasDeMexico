using System.Collections;
using System.Reflection;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>El comportamiento común de los enemigos de la lámina (WildEnemy), con datos de prueba.</summary>
    public class WildEnemyTests
    {
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

        GameObject ground;
        GameObject player;
        Health playerHealth;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            player = CreateCharacter("JugadorPrueba", Vector3.zero);
            playerHealth = player.AddComponent<Health>();
            SetInvulnerability(playerHealth, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(ground);
        }

        static GameObject CreateCharacter(string name, Vector3 position)
        {
            var character = new GameObject(name);
            character.transform.position = position;
            var controller = character.AddComponent<CharacterController>();
            controller.height = 0.9f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.45f, 0f);
            controller.minMoveDistance = 0f;
            return character;
        }

        static void SetInvulnerability(Health health, float seconds) =>
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(health, seconds);

        static EnemyData Data(EnemyAttackShape shape, EnemyDefense defense = EnemyDefense.None, EnemyMovement movement = EnemyMovement.Chase)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHealth = 40;
            data.moveSpeed = 3f;
            data.detectionRadius = 8f;
            data.loseRadius = 12f;
            data.movement = movement;
            data.attackShape = shape;
            data.attackRange = shape == EnemyAttackShape.Projectile ? 7f : 1.2f;
            data.attackSize = 0.9f;
            data.attackReach = shape == EnemyAttackShape.Projectile ? 8f : 3f;
            data.attackWindup = 0.5f;
            data.attackRecover = 0.3f;
            data.attackCooldown = 10f; // un solo ataque por prueba
            data.attackDamage = 7;
            data.defense = defense;
            data.defenseFactor = 0.5f;
            data.defenseSeconds = 1f;
            data.defenseCooldown = 0f;
            data.respawnDelay = 0f;
            return data;
        }

        WildEnemy CreateEnemy(EnemyData data, Vector3 position, out Health health)
        {
            var enemy = CreateCharacter("EnemigoPrueba", position);
            enemy.SetActive(false);
            health = enemy.AddComponent<Health>();
            SetInvulnerability(health, 0f);
            var wild = enemy.AddComponent<WildEnemy>();
            wild.Data = data;
            typeof(WildEnemy).GetField("target", PrivateField).SetValue(wild, player.transform);
            if (data.attackShape == EnemyAttackShape.Projectile)
            {
                var shot = new GameObject("Proyectil");
                shot.SetActive(false);
                shot.transform.SetParent(enemy.transform);
                typeof(WildEnemy).GetField("projectiles", PrivateField).SetValue(wild, new[] { shot.AddComponent<Projectile>() });
            }
            if (data.flies)
            {
                var height = new GameObject("Altura").transform;
                height.SetParent(enemy.transform, false);
                typeof(WildEnemy).GetField("heightRoot", PrivateField).SetValue(wild, height);
            }
            enemy.SetActive(true);
            return wild;
        }

        [UnityTest]
        public IEnumerator CircleAttack_IsTelegraphed_BeforeItHurts()
        {
            var enemy = CreateEnemy(Data(EnemyAttackShape.Circle), new Vector3(0f, 0f, 1f), out var health);
            try
            {
                Assert.That(health.Max, Is.EqualTo(40), "La vida sale de EnemyData.");
                yield return new WaitForSeconds(0.2f);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Windup), "Pegado al jugador, debe avisar antes de golpear.");
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "Durante el aviso no debe dañar.");
                yield return new WaitForSeconds(0.6f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 7), "Al terminar el aviso, el círculo golpea.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator HitDuringTheWindup_CancelsTheAttack()
        {
            var enemy = CreateEnemy(Data(EnemyAttackShape.Circle), new Vector3(0f, 0f, 1f), out var health);
            try
            {
                yield return new WaitForSeconds(0.2f);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Windup));
                health.TryDamage(5, player.transform.position);
                yield return new WaitForSeconds(0.6f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "Un golpe durante el aviso lo interrumpe.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator LineAttack_ChargesThroughTheTarget()
        {
            var data = Data(EnemyAttackShape.Line);
            data.attackRange = 3f;
            var enemy = CreateEnemy(data, new Vector3(0f, 0f, 2.5f), out _);
            try
            {
                yield return new WaitForSeconds(0.25f);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Windup), "A media distancia avisa la embestida.");
                yield return new WaitForSeconds(0.8f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 7), "La embestida debe alcanzarlo una sola vez.");
                Assert.That(enemy.transform.position.z, Is.LessThan(1f), "Debe haber recorrido la franja hacia el jugador.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator KeepDistance_BacksAway_AndHitsWithAProjectile()
        {
            var data = Data(EnemyAttackShape.Projectile, movement: EnemyMovement.KeepDistance);
            data.preferredDistance = 4f;
            data.attackWindup = 0.3f;
            var enemy = CreateEnemy(data, new Vector3(0f, 0f, 1.5f), out _);
            try
            {
                yield return new WaitForSeconds(0.1f);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Chase), "Con el jugador encima, primero se aparta.");
                yield return new WaitForSeconds(2.5f);
                Assert.That(playerHealth.Current, Is.LessThan(playerHealth.Max), "La piedra debe alcanzar al jugador.");
                Assert.That(enemy.transform.position.z, Is.GreaterThan(2.4f), "Debe apartarse hasta su distancia.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator GuardAfterHit_HalvesTheNextHits()
        {
            var enemy = CreateEnemy(Data(EnemyAttackShape.Circle, EnemyDefense.GuardAfterHit), new Vector3(0f, 0f, 5f), out var health);
            try
            {
                yield return null;
                health.TryDamage(10, player.transform.position);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Guard));
                health.TryDamage(10, player.transform.position);
                Assert.That(health.Current, Is.EqualTo(40 - 10 - 5), "Cubierto recibe la mitad.");
                yield return new WaitForSeconds(1.2f);
                health.TryDamage(10, player.transform.position);
                Assert.That(health.Current, Is.EqualTo(15), "Al terminar la defensa, el golpe siguiente vuelve a hacer el daño completo.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator FrontArmor_HalvesOnlyHitsFromTheFront()
        {
            var enemy = CreateEnemy(Data(EnemyAttackShape.Circle, EnemyDefense.FrontArmor), new Vector3(0f, 0f, 3f), out var health);
            try
            {
                yield return new WaitForSeconds(0.3f); // se gira hacia el jugador al perseguirlo
                health.TryDamage(10, player.transform.position);
                Assert.That(health.Current, Is.EqualTo(35), "De frente, las escamas paran la mitad.");

                player.transform.position = enemy.transform.position + enemy.FacingDirection * -2f; // a su espalda
                enemy.enabled = false; // que no se gire
                typeof(WildEnemy).GetMethod("UpdateFrontArmor", PrivateField).Invoke(enemy, null);
                health.TryDamage(10, player.transform.position);
                Assert.That(health.Current, Is.EqualTo(25), "Por la espalda, daño completo.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Phase_IgnoresDamage_AndReappearsNearTheTarget()
        {
            var enemy = CreateEnemy(Data(EnemyAttackShape.Line, EnemyDefense.Phase), new Vector3(0f, 0f, 6f), out var health);
            try
            {
                yield return null;
                health.TryDamage(10, player.transform.position);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Phased));
                Assert.IsFalse(health.TryDamage(10, player.transform.position), "Desvanecido no recibe daño.");
                yield return new WaitForSeconds(1.2f);
                Assert.That(enemy.Current, Is.Not.EqualTo(WildEnemy.State.Phased));
                Assert.That(Vector3.Distance(enemy.transform.position, player.transform.position), Is.LessThan(3f),
                    "Reaparece junto al jugador.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Flyer_HoversAboveTheGround_AndFliesHighWhenHit()
        {
            var data = Data(EnemyAttackShape.Line, EnemyDefense.FlyHigh);
            data.flies = true;
            data.hoverHeight = 1f;
            var enemy = CreateEnemy(data, new Vector3(0f, 0f, 7f), out var health);
            try
            {
                yield return new WaitForSeconds(0.1f);
                Assert.That(enemy.Height, Is.InRange(0.85f, 1.15f), "Vuela a su altura.");
                health.TryDamage(10, player.transform.position);
                yield return new WaitForSeconds(0.2f);
                Assert.That(enemy.Height, Is.GreaterThan(2f), "Al recibir un golpe sube fuera de alcance.");
                Assert.IsFalse(health.TryDamage(10, player.transform.position), "Arriba no recibe daño.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Defeated_StaysDown_WithoutRespawn()
        {
            var enemy = CreateEnemy(Data(EnemyAttackShape.Circle), new Vector3(0f, 0f, 6f), out var health);
            try
            {
                yield return null;
                health.TryDamage(100, player.transform.position);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Dead));
                Assert.IsFalse(enemy.GetComponent<CharacterController>().enabled, "Vencido no tiene collider.");
                yield return new WaitForSeconds(1f);
                Assert.IsTrue(health.IsDead, "Sin tiempo de reaparición queda vencido.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }
    }
}
