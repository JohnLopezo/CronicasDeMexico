using System.Collections;
using System.Reflection;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// En las pruebas de PlayMode nadie esquiva saltando salvo que la prueba lo pida (EnemyJumpDodge.Roll): así los golpes
    /// de las demás pruebas entran siempre, sin fallos al azar.
    /// </summary>
    [SetUpFixture]
    public class NoRandomJumpDodges
    {
        [OneTimeSetUp]
        public void NeverDodge() => EnemyJumpDodge.Roll = () => 1f;

        [OneTimeTearDown]
        public void Restore() => EnemyJumpDodge.Roll = () => Random.value;
    }

    /// <summary>Esquiva saltando (PORTAR_A_UNITY §1), con WildEnemy y la tirada fijada.</summary>
    public class EnemyJumpDodgeTests
    {
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

        GameObject ground;
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            player = CreateCharacter("JugadorPrueba", Vector3.zero);
            player.AddComponent<Health>();
        }

        [TearDown]
        public void TearDown()
        {
            EnemyJumpDodge.Roll = () => 1f;
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

        static EnemyData Data(bool flies = false)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHealth = 40;
            data.moveSpeed = 3f;
            data.detectionRadius = 8f;
            data.loseRadius = 12f;
            data.attackShape = EnemyAttackShape.Circle;
            data.attackRange = 1.2f;
            data.attackSize = 0.9f;
            data.attackReach = 3f;
            data.attackWindup = 0.5f;
            data.attackRecover = 0.3f;
            data.attackCooldown = 10f;
            data.attackDamage = 7;
            data.flies = flies;
            data.hoverHeight = 1f;
            return data;
        }

        WildEnemy CreateEnemy(EnemyData data, Vector3 position, out Health health)
        {
            var enemy = CreateCharacter("EnemigoPrueba", position);
            enemy.SetActive(false);
            health = enemy.AddComponent<Health>();
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(health, 0f);
            var wild = enemy.AddComponent<WildEnemy>();
            wild.Data = data;
            typeof(WildEnemy).GetField("target", PrivateField).SetValue(wild, player.transform);
            if (data.flies)
            {
                var height = new GameObject("Altura").transform;
                height.SetParent(enemy.transform, false);
                typeof(WildEnemy).GetField("heightRoot", PrivateField).SetValue(wild, height);
            }
            enemy.SetActive(true);
            return wild;
        }

        Vector3 From => player.transform.position;

        [Test]
        public void OtherTests_SeeNoDodges() =>
            Assert.That(EnemyJumpDodge.Roll(), Is.GreaterThanOrEqualTo(1f), "Fuera de estas pruebas la tirada no debe esquivar nunca.");

        // Fuera de su radio de detección (quieto, para medir el empujón): esquiva, no pierde vida, pone la espera y queda
        // invulnerable 0,4 s.
        [UnityTest]
        public IEnumerator Walker_DodgesThePlayersHit_AndWaitsBeforeTheNext()
        {
            EnemyJumpDodge.Roll = () => 0f;
            var enemy = CreateEnemy(Data(), new Vector3(0f, 0f, 10f), out var health);
            try
            {
                yield return new WaitForSeconds(0.3f); // que se pose en el suelo
                var dodge = enemy.GetComponent<EnemyJumpDodge>();
                Assert.IsNotNull(dodge, "WildEnemy debe añadir EnemyJumpDodge en su Awake.");
                var y = enemy.transform.position.y;
                var z = enemy.transform.position.z;
                Assert.IsFalse(health.TryDamage(10, From, fromPlayer: true), "El golpe esquivado no cuenta (ni pide la pausa de impacto).");
                Assert.AreEqual(health.Max, health.Current, "Esquivado, no pierde vida.");
                Assert.IsTrue(dodge.IsCoolingDown, "Tras esquivar espera 3 s.");
                Assert.IsFalse(health.TryDamage(10, From, fromPlayer: true), "Invulnerable un momento tras el salto.");
                yield return new WaitForSeconds(0.15f);
                Assert.Greater(enemy.transform.position.y, y + 0.3f, "Salta hacia arriba.");
                Assert.Greater(enemy.transform.position.z, z + 0.3f, "Y hacia atrás, lejos del jugador.");
                yield return new WaitForSeconds(0.4f);
                Assert.IsTrue(health.TryDamage(10, From, fromPlayer: true), "En la espera de 3 s ya no esquiva.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        // Solo los golpes del jugador: el resto (trampas, proyectiles, defensores) entra siempre.
        [UnityTest]
        public IEnumerator HitsNotFromThePlayer_AreNeverDodged()
        {
            EnemyJumpDodge.Roll = () => 0f;
            var enemy = CreateEnemy(Data(), new Vector3(0f, 0f, 6f), out var health);
            try
            {
                yield return new WaitForSeconds(0.3f);
                Assert.IsTrue(health.TryDamage(10, From), "Un golpe que no es del jugador no se esquiva.");
            }
            finally
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        // Aturdido, con la ruptura rota o avisando su ataque no esquiva.
        [UnityTest]
        public IEnumerator NoDodge_WhenStunned_Broken_OrAttacking()
        {
            EnemyJumpDodge.Roll = () => 0f;
            var stunned = CreateEnemy(Data(), new Vector3(0f, 0f, 6f), out var stunnedHealth);
            var broken = CreateEnemy(Data(), new Vector3(6f, 0f, 6f), out var brokenHealth);
            var attacking = CreateEnemy(Data(), new Vector3(0f, 0f, 1f), out var attackingHealth);
            try
            {
                yield return new WaitForSeconds(0.2f);
                stunned.Stagger(2f);
                Assert.IsTrue(stunnedHealth.TryDamage(10, From, fromPlayer: true), "Aturdido no esquiva.");

                var gauge = BreakGauge.Ensure(broken.gameObject, "Prueba", 3, 5f, Affinity.Tierra);
                typeof(BreakGauge).GetField("brokenUntil", PrivateField).SetValue(gauge, Time.time + 5f);
                Assert.IsTrue(brokenHealth.TryDamage(10, From, fromPlayer: true), "Con la ruptura rota no esquiva.");

                Assert.AreEqual(WildEnemy.State.Windup, attacking.Current, "Pegado al jugador avisa su ataque.");
                Assert.IsTrue(attackingHealth.TryDamage(10, From, fromPlayer: true), "Avisando su ataque no esquiva.");
            }
            finally
            {
                Object.Destroy(stunned.gameObject);
                Object.Destroy(broken.gameObject);
                Object.Destroy(attacking.gameObject);
            }
        }

        // Los voladores esquivan más (35 % frente a 25 %) y suben en vez de saltar.
        [UnityTest]
        public IEnumerator Flyer_DodgesMoreOften_AndRises()
        {
            EnemyJumpDodge.Roll = () => 0.3f; // entre el 25 % y el 35 %
            var walker = CreateEnemy(Data(), new Vector3(-6f, 0f, 7f), out var walkerHealth);
            var flyer = CreateEnemy(Data(flies: true), new Vector3(6f, 0f, 7f), out var flyerHealth);
            try
            {
                yield return new WaitForSeconds(0.3f);
                Assert.IsTrue(walkerHealth.TryDamage(10, From, fromPlayer: true), "Con 0,3 el de a pie no esquiva (25 %).");
                Assert.IsFalse(flyerHealth.TryDamage(10, From, fromPlayer: true), "Con 0,3 el volador sí (35 %).");
                yield return new WaitForSeconds(0.3f);
                Assert.Greater(flyer.Height, 2f, "Al esquivar sube a su altura de defensa.");
            }
            finally
            {
                Object.Destroy(walker.gameObject);
                Object.Destroy(flyer.gameObject);
            }
        }
    }
}
