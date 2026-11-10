using System.Collections;
using System.Linq;
using System.Reflection;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>Los jefes de la arena de pruebas (TrialBoss): cada forma de habilidad, las fases, la resurrección y la arena.</summary>
    public class TrialBossTests
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
            player = Character("JugadorPrueba", Vector3.zero);
            playerHealth = player.AddComponent<Health>();
            playerHealth.SetMaxHealth(200); // aguanta todas las habilidades de una prueba
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(playerHealth, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(ground);
        }

        [UnityTearDown]
        public IEnumerator UnloadArena()
        {
            var scene = SceneManager.GetSceneByName("Arena_Pruebas_Jefes");
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        static GameObject Character(string name, Vector3 position)
        {
            var character = new GameObject(name);
            character.transform.position = position;
            var controller = character.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            controller.minMoveDistance = 0f;
            return character;
        }

        static BossAbility Ability(BossAbilityKind kind, int damage = 10, int count = 1, float factor = 1f) => new()
        {
            name = kind.ToString(), kind = kind, range = 20f, radius = 1.2f, length = 6f, count = count, spread = 60f,
            windup = 0.3f, cooldown = 100f, damage = damage, factor = factor, seconds = 1f,
        };

        TrialBoss Boss(Vector3 position, params BossAbility[] abilities) => Boss(position, 0f, abilities);

        TrialBoss Boss(Vector3 position, float resurrect, params BossAbility[] abilities)
        {
            var data = ScriptableObject.CreateInstance<BossData>();
            data.maxHealth = 100;
            data.moveSpeed = 0f; // quieto: solo cuentan las habilidades
            data.engageRadius = 0f;
            data.resurrectFraction = resurrect;
            data.abilities = abilities;
            var root = Character("JefePrueba", position);
            root.SetActive(false);
            var health = root.AddComponent<Health>();
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(health, 0f);
            var boss = root.AddComponent<TrialBoss>();
            boss.Data = data;
            typeof(TrialBoss).GetField("target", PrivateField).SetValue(boss, player.transform);
            typeof(TrialBoss).GetField("projectiles", PrivateField).SetValue(boss, Enumerable.Range(0, 10).Select(i =>
            {
                var shot = new GameObject($"Proyectil_{i}");
                shot.SetActive(false);
                shot.transform.SetParent(root.transform);
                return shot.AddComponent<Projectile>();
            }).ToArray());
            typeof(TrialBoss).GetField("minions", PrivateField).SetValue(boss, Enumerable.Range(0, 2).Select(i =>
            {
                var minion = new GameObject($"Invocado_{i}");
                minion.SetActive(false);
                return minion;
            }).ToArray());
            root.SetActive(true);
            return boss;
        }

        int Lost => playerHealth.Max - playerHealth.Current;

        [UnityTest]
        public IEnumerator Swipe_IsTelegraphed_AndRepeats()
        {
            var boss = Boss(new Vector3(0f, 0f, 1f), Ability(BossAbilityKind.Swipe, count: 2));
            yield return null;
            Assert.IsTrue(boss.Force(0));
            yield return new WaitForSeconds(0.15f);
            Assert.That(Lost, Is.EqualTo(0), "Durante el aviso no daña.");
            yield return new WaitForSeconds(0.6f);
            Assert.That(Lost, Is.EqualTo(20), "Dos zarpazos seguidos.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Beam_HitsOnlyAlongItsLine()
        {
            var boss = Boss(new Vector3(0f, 0f, 4f), Ability(BossAbilityKind.Beam));
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(0.1f);
            player.transform.position = new Vector3(3f, 0f, 0f); // se aparta de la franja durante el aviso
            yield return new WaitForSeconds(0.4f);
            Assert.That(Lost, Is.EqualTo(0), "Fuera de la franja no lo alcanza.");
            yield return new WaitForSeconds(0.7f);
            player.transform.position = Vector3.zero;
            boss.GetType().GetField("cooldowns", PrivateField).SetValue(boss, new float[1]);
            boss.Force(0);
            yield return new WaitForSeconds(0.45f);
            Assert.That(Lost, Is.EqualTo(10), "Dentro de la franja, sí.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Charge_RunsThroughTheTarget()
        {
            var boss = Boss(new Vector3(0f, 0f, 4f), Ability(BossAbilityKind.Charge));
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(1.2f);
            Assert.That(Lost, Is.EqualTo(10));
            Assert.That(boss.transform.position.z, Is.LessThan(2f), "Debe haber embestido hacia el jugador.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Leap_LandsWhereTheTargetWas_AndCannotBeHitInTheAir()
        {
            var boss = Boss(new Vector3(0f, 0f, 6f), Ability(BossAbilityKind.Leap));
            var health = boss.GetComponent<Health>();
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(0.4f); // ya en el aire
            Assert.IsFalse(health.TryDamage(10, player.transform.position), "En el aire no recibe daño.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(Lost, Is.EqualTo(10), "Cae encima del jugador.");
            Assert.That(Vector3.Distance(boss.transform.position, Vector3.zero), Is.LessThan(0.6f));
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Rain_FallsOnTheTarget()
        {
            var boss = Boss(new Vector3(0f, 0f, 6f), Ability(BossAbilityKind.Rain, count: 4));
            typeof(TrialBoss).GetField("circles", PrivateField).SetValue(boss, new TelegraphMarker[6]);
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(0.5f);
            Assert.That(Lost, Is.EqualTo(10), "La primera gota cae donde está el jugador; solo cuenta una.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Nova_HurtsAround_AndSlows()
        {
            var movement = player.AddComponent<PlayerMovement>();
            var boss = Boss(new Vector3(0f, 0f, 1f), Ability(BossAbilityKind.Nova, factor: 0.5f));
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(0.45f);
            Assert.That(Lost, Is.EqualTo(10));
            Assert.That(movement.SpeedMultiplier, Is.EqualTo(0.5f).Within(0.001f), "La onda frena.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Volley_LaunchesEveryShot()
        {
            var boss = Boss(new Vector3(0f, 0f, 5f), Ability(BossAbilityKind.Volley, count: 7));
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(0.4f);
            Assert.That(boss.GetComponentsInChildren<Projectile>(true).Count(p => p.IsFlying), Is.EqualTo(7));
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Summon_BringsOutItsMinions()
        {
            var boss = Boss(new Vector3(0f, 0f, 5f), Ability(BossAbilityKind.Summon, count: 2));
            var minions = (GameObject[])typeof(TrialBoss).GetField("minions", PrivateField).GetValue(boss);
            yield return null;
            boss.Force(0);
            yield return new WaitForSeconds(0.4f);
            Assert.That(minions.Count(m => m.activeSelf), Is.EqualTo(2));
            Object.Destroy(boss.gameObject);
            foreach (var minion in minions)
                Object.Destroy(minion);
        }

        [UnityTest]
        public IEnumerator Shield_ReducesTheDamage_WhileItLasts()
        {
            var boss = Boss(new Vector3(0f, 0f, 5f), Ability(BossAbilityKind.Shield, factor: 0.3f));
            var health = boss.GetComponent<Health>();
            yield return null;
            boss.Force(0);
            yield return null;
            health.TryDamage(10, player.transform.position);
            Assert.That(health.Current, Is.EqualTo(97), "Protegido recibe el 30 %.");
            yield return new WaitForSeconds(1.2f);
            health.TryDamage(10, player.transform.position);
            Assert.That(health.Current, Is.EqualTo(87), "Al terminar, el daño completo.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Phases_ChangeWithHealth_WithAMomentOfInvulnerability()
        {
            var boss = Boss(new Vector3(0f, 0f, 5f), Ability(BossAbilityKind.Swipe));
            var health = boss.GetComponent<Health>();
            yield return null;
            Assert.That(boss.Phase, Is.EqualTo(1));
            health.TryDamage(45, player.transform.position);
            Assert.That(boss.Phase, Is.EqualTo(2), "Al 55 %, fase 2.");
            Assert.IsFalse(health.TryDamage(5, player.transform.position), "Al cambiar de fase, un momento invulnerable.");
            yield return new WaitForSeconds(1.1f);
            health.TryDamage(30, player.transform.position);
            Assert.That(boss.Phase, Is.EqualTo(3), "Al 25 %, fase 3.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Resurrect_ComesBackOnce()
        {
            var boss = Boss(new Vector3(0f, 0f, 5f), 0.4f, Ability(BossAbilityKind.Swipe));
            var health = boss.GetComponent<Health>();
            yield return null;
            health.TryDamage(100, player.transform.position);
            Assert.IsFalse(boss.IsDefeated, "La primera vez se levanta.");
            yield return new WaitForSeconds(1.7f);
            Assert.That(health.Current, Is.EqualTo(40), "Con el 40 % de la vida.");
            yield return new WaitForSeconds(1.1f); // termina su invulnerabilidad
            health.TryDamage(100, player.transform.position);
            Assert.IsTrue(boss.IsDefeated, "La segunda, cae.");
            Object.Destroy(boss.gameObject);
        }

        [UnityTest]
        public IEnumerator Arena_HasSixBosses_AndAPadSummonsEach()
        {
            Object.Destroy(player); // la arena trae su propio jugador
            Object.Destroy(ground);
            yield return SceneManager.LoadSceneAsync("Arena_Pruebas_Jefes", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Arena_Pruebas_Jefes");
            SceneManager.SetActiveScene(scene);
            yield return null;
            var trial = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BossTrial>(true)).Single();
            var pads = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BossTrialPad>(true)).ToArray();
            Assert.That(trial.Count, Is.EqualTo(6));
            Assert.That(pads.Length, Is.EqualTo(6));
            for (var i = 0; i < trial.Count; i++)
            {
                var boss = trial.Boss(i);
                Assert.IsFalse(boss.gameObject.activeSelf, $"{boss.name}: espera a su placa.");
                Assert.That(boss.Data.abilities.Length, Is.InRange(3, 4), boss.name);
                Assert.IsNotNull(boss.Data.walk, boss.name);
            }

            var center = trial.transform.parent.Find("Centro_Arena").position;
            for (var i = 0; i < trial.Count; i++)
            {
                trial.Summon(i);
                yield return null;
                Assert.IsTrue(trial.Boss(i).gameObject.activeSelf);
                Assert.That(Vector3.Distance(trial.Boss(i).transform.position, center), Is.LessThan(0.5f), "Aparece en el centro.");
                Assert.That(Enumerable.Range(0, trial.Count).Count(j => trial.Boss(j).gameObject.activeSelf), Is.EqualTo(1), "Uno a la vez.");
            }

            // Despierta al acercarse el jugador.
            var arenaPlayer = GameObject.FindWithTag("Player");
            var controller = arenaPlayer.GetComponent<CharacterController>();
            controller.enabled = false;
            arenaPlayer.transform.position = center + Vector3.right * 3f;
            controller.enabled = true;
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(trial.Active.IsEngaged);
        }
    }
}
