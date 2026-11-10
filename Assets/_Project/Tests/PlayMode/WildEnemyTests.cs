using System.Collections;
using System.Reflection;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Player;
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
            if (data.attackShape == EnemyAttackShape.Projectile || data.special is EnemySpecial.SlowSpit or EnemySpecial.FanThrow)
            {
                var shots = new Projectile[3];
                for (var i = 0; i < shots.Length; i++)
                {
                    var shot = new GameObject($"Proyectil_{i}");
                    shot.SetActive(false);
                    shot.transform.SetParent(enemy.transform);
                    shots[i] = shot.AddComponent<Projectile>();
                }
                typeof(WildEnemy).GetField("projectiles", PrivateField).SetValue(wild, shots);
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
        // ---------- Habilidades especiales (etapa 3) ----------

        static EnemyData WithSpecial(EnemyData data, EnemySpecial special, float range, float radius, int damage = 6)
        {
            data.moveSpeed = 0f; // quieto: así solo cuenta la habilidad
            data.special = special;
            data.specialRange = range;
            data.specialRadius = radius;
            data.specialWindup = 0.3f;
            data.specialCooldown = 10f;
            data.specialDamage = damage;
            data.slowFactor = 0.5f;
            data.slowSeconds = 2f;
            return data;
        }

        [UnityTest]
        public IEnumerator SlowSpit_HitsFromAfar_AndSlowsTheTargetForAWhile()
        {
            var movement = player.AddComponent<PlayerMovement>();
            var enemy = CreateEnemy(WithSpecial(Data(EnemyAttackShape.Circle), EnemySpecial.SlowSpit, 5f, 0.45f, damage: 4),
                new Vector3(0f, 0f, 3.5f), out _);
            try
            {
                yield return new WaitForSeconds(0.1f);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Special), "Fuera del alcance de la mordida, escupe.");
                yield return new WaitForSeconds(1.0f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 4));
                Assert.That(movement.SpeedMultiplier, Is.EqualTo(0.5f).Within(0.001f), "El escupitajo frena.");
                yield return new WaitForSeconds(2.2f);
                Assert.That(movement.SpeedMultiplier, Is.EqualTo(1f), "El freno se pasa.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator TailSweep_HitsAllAround()
        {
            var enemy = CreateEnemy(WithSpecial(Data(EnemyAttackShape.Line), EnemySpecial.TailSweep, 1.6f, 1.8f),
                new Vector3(0f, 0f, 1.2f), out _);
            try
            {
                yield return new WaitForSeconds(0.1f);
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Special));
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "Avisa antes del coletazo.");
                yield return new WaitForSeconds(0.4f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 6));
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DoubleSwipe_HitsTwice()
        {
            var enemy = CreateEnemy(WithSpecial(Data(EnemyAttackShape.Line), EnemySpecial.DoubleSwipe, 1.3f, 0.9f),
                new Vector3(0f, 0f, 0.9f), out _);
            try
            {
                yield return new WaitForSeconds(0.45f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 6), "Primer zarpazo.");
                Assert.That(enemy.Current, Is.EqualTo(WildEnemy.State.Special), "Sigue con el segundo.");
                yield return new WaitForSeconds(0.35f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 12), "Segundo zarpazo.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator FanThrow_LaunchesThreeShots()
        {
            var enemy = CreateEnemy(WithSpecial(Data(EnemyAttackShape.Circle), EnemySpecial.FanThrow, 6f, 0.45f),
                new Vector3(0f, 0f, 4f), out _);
            try
            {
                yield return new WaitForSeconds(0.4f);
                var flying = 0;
                foreach (var shot in enemy.GetComponentsInChildren<Projectile>(true))
                    if (shot.IsFlying)
                        flying++;
                Assert.That(flying, Is.EqualTo(3), "Tres frutos en abanico.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator SlowScreech_HurtsAndSlowsAround()
        {
            var movement = player.AddComponent<PlayerMovement>();
            var enemy = CreateEnemy(WithSpecial(Data(EnemyAttackShape.Line), EnemySpecial.SlowScreech, 2.8f, 3f, damage: 3),
                new Vector3(0f, 0f, 2f), out _);
            try
            {
                yield return new WaitForSeconds(0.5f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 3));
                Assert.That(movement.SpeedMultiplier, Is.EqualTo(0.5f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DrainBite_HealsTheBiter()
        {
            var data = Data(EnemyAttackShape.Circle);
            data.attackDrains = true;
            var enemy = CreateEnemy(data, new Vector3(0f, 0f, 1f), out var health);
            try
            {
                health.TryDamage(20, player.transform.position); // antes de su primer cuadro: no interrumpe nada
                yield return new WaitForSeconds(1.5f); // el retroceso lo aparta: vuelve, avisa y muerde
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 7));
                Assert.That(health.Current, Is.EqualTo(20 + 7), "Recupera lo que muerde.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Swarm_AlliesAttackAtTheSameTime()
        {
            var data = Data(EnemyAttackShape.Circle);
            data.moveSpeed = 0f;
            data.swarm = true;
            data.swarmRadius = 5f;
            var first = CreateEnemy(data, new Vector3(0f, 0f, 1f), out _);
            var second = CreateEnemy(data, new Vector3(3f, 0f, 1f), out _);
            try
            {
                yield return new WaitForSeconds(0.1f);
                Assert.That(first.Current, Is.EqualTo(WildEnemy.State.Windup));
                Assert.That(second.Current, Is.EqualTo(WildEnemy.State.Windup), "El de al lado ataca a la vez, aunque no llegue.");
            }
            finally
            {
                Object.Destroy(first.gameObject);
                Object.Destroy(second.gameObject);
            }
        }
    }
}
