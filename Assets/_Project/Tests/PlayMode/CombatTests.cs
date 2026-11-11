using System.Collections;
using System.Reflection;
using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Data.Characters;
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
            typeof(Health).GetField("invulnerableUntil", PrivateField).SetValue(playerHealth, float.NegativeInfinity);

            Assert.IsTrue(playerHealth.TryDamage(100, Vector3.zero));
            Assert.That(playerHealth.Current, Is.EqualTo(0), "La vida no debe bajar de 0.");
            Assert.IsFalse(playerHealth.TryDamage(1, Vector3.zero), "Muerto no debe recibir más daño.");
            Assert.That(deaths, Is.EqualTo(1), "Died debe dispararse una sola vez.");
        }

        [Test]
        public void Heal_ClampsToMax_ReportsSource_AndNeverRevives()
        {
            HealSource? reportedSource = null;
            var reportedAmount = 0;
            playerHealth.Healed += (amount, source) => { reportedAmount = amount; reportedSource = source; };
            playerHealth.TryDamage(2, Vector3.zero);

            Assert.That(playerHealth.Heal(10, HealSource.RestShrine), Is.EqualTo(2), "Solo recupera hasta el máximo.");
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max));
            Assert.That(reportedAmount, Is.EqualTo(2));
            Assert.That(reportedSource, Is.EqualTo(HealSource.RestShrine));
            Assert.That(playerHealth.Heal(1, HealSource.Consumable), Is.EqualTo(0), "Con la vida llena no cura nada.");

            typeof(Health).GetField("invulnerableUntil", PrivateField).SetValue(playerHealth, float.NegativeInfinity);
            playerHealth.TryDamage(100, Vector3.zero);
            Assert.That(playerHealth.Heal(5, HealSource.Ability), Is.EqualTo(0), "Heal no debe revivir.");
            Assert.IsTrue(playerHealth.IsDead);
        }

        // Al cambiar de escena el jugador nuevo toma la vida con la que salió: nunca sube (Diseno.md §9).
        [Test]
        public void CarryOver_LowersToTheCarriedHealth_ButNeverRaisesIt()
        {
            playerHealth.CarryOver(2);
            Assert.That(playerHealth.Current, Is.EqualTo(2));
            playerHealth.CarryOver(playerHealth.Max);
            Assert.That(playerHealth.Current, Is.EqualTo(2), "CarryOver no debe curar.");
            playerHealth.CarryOver(0);
            Assert.That(playerHealth.Current, Is.EqualTo(1), "Llega con al menos 1 de vida.");
        }

        [Test]
        public void ReviveFull_OnlyWorksWhenDead()
        {
            playerHealth.TryDamage(2, Vector3.zero);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ReviveFull"));
            playerHealth.ReviveFull();
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 2), "ReviveFull no debe servir para curar.");

            typeof(Health).GetField("invulnerableUntil", PrivateField).SetValue(playerHealth, float.NegativeInfinity);
            playerHealth.TryDamage(100, Vector3.zero);
            playerHealth.ReviveFull();
            Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max));
        }

        // Regla central (Diseno.md §9): nada recupera vida con el tiempo ni al acabar un combate.
        [UnityTest]
        public IEnumerator Health_DoesNotRegenerate_OverTime()
        {
            player.SetActive(false);
            player.AddComponent<PlayerDamageResponse>();
            player.SetActive(true);
            yield return null;

            playerHealth.TryDamage(2, Vector3.zero);
            var afterHit = playerHealth.Current;
            yield return new WaitForSeconds(2f);

            Assert.That(playerHealth.Current, Is.EqualTo(afterHit), "La vida no debe regenerarse sola.");
        }

        [UnityTest]
        public IEnumerator Attack_HitsTargetInFront_NotBehind_AndNeverItself()
        {
            var front = CreateDummy(new Vector3(0f, 0f, -1f), out var frontHealth);
            var behind = CreateDummy(new Vector3(0f, 0f, 1.5f), out var behindHealth);

            try
            {
                // Los colliders nuevos entran en las consultas de física con el siguiente paso de simulación;
                // un solo frame no basta si el juego va a más FPS que la física.
                yield return new WaitForFixedUpdate();

                Assert.That(attack.Strike(), Is.EqualTo(1), "Debe golpear exactamente a un objetivo.");
                Assert.That(frontHealth.Current, Is.EqualTo(frontHealth.Max - attack.Damage), "El objetivo de delante debe recibir daño.");
                Assert.That(behindHealth.Current, Is.EqualTo(behindHealth.Max), "El de detrás no debe recibir daño.");
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "El jugador no debe golpearse a sí mismo.");
            }
            finally
            {
                // Si quedaran en la escena bloquearían a los personajes de las pruebas siguientes.
                Object.Destroy(front);
                Object.Destroy(behind);
            }
        }

        // E1: tres golpes seguidos forman un combo; el tercero remata con más daño y después se reinicia.
        [UnityTest]
        public IEnumerator Attack_ChainsAThreeHitCombo_WhoseFinisherHitsHarder()
        {
            var dummy = CreateDummy(new Vector3(0f, 0f, -1f), out var dummyHealth);
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(dummyHealth, 0f);
            typeof(PlayerAttack).GetField("lungeSpeed", PrivateField).SetValue(attack, 0f); // que no se acerque
            try
            {
                yield return new WaitForFixedUpdate();
                var expected = new[] { attack.Damage, attack.Damage, Mathf.RoundToInt(attack.Damage * 1.5f), attack.Damage };
                for (var i = 0; i < expected.Length; i++)
                {
                    var before = dummyHealth.Current;
                    Assert.That(attack.Attack(), Is.EqualTo(1), $"El golpe {i + 1} debe acertar.");
                    Assert.That(attack.ComboStep, Is.EqualTo(i % 3));
                    Assert.That(before - dummyHealth.Current, Is.EqualTo(expected[i]), $"Daño del golpe {i + 1}.");
                    yield return new WaitForSeconds(0.25f);
                }
                Assert.That(Time.timeScale, Is.EqualTo(1f), "El hitstop debe devolver la escala de tiempo.");

                yield return new WaitForSeconds(0.6f);
                attack.Attack();
                Assert.That(attack.ComboStep, Is.EqualTo(0), "Pasada la ventana el combo vuelve a empezar.");
            }
            finally
            {
                Object.Destroy(dummy);
            }
        }

        // E2: un enemigo en diagonal, que el golpe recto no alcanza, se alcanza girando hacia él; uno detrás no.
        [UnityTest]
        public IEnumerator Attack_TurnsTowardANearbyEnemyInsideTheCone_ButNotBehind()
        {
            var behind = CreateDummy(new Vector3(0f, 0f, 1.2f), out var behindHealth);
            var side = CreateDummy(new Vector3(1.1f, 0f, -1.1f), out var sideHealth);
            typeof(PlayerAttack).GetField("lungeSpeed", PrivateField).SetValue(attack, 0f);
            try
            {
                yield return new WaitForFixedUpdate();
                Assert.That(attack.Strike(), Is.EqualTo(0), "Sin girar, el golpe recto no llega a la diagonal.");
                Assert.That(attack.Attack(), Is.EqualTo(1), "Con el apuntado asistido debe acertar.");
                Assert.That(sideHealth.Current, Is.LessThan(sideHealth.Max));
                Assert.That(behindHealth.Current, Is.EqualTo(behindHealth.Max), "No debe girarse hacia atrás.");
                var facing = player.GetComponent<PlayerMovement>().FacingDirection;
                Assert.That(Vector3.Angle(facing, new Vector3(1f, 0f, -1f)), Is.LessThan(5f), "Debe quedar mirando al enemigo.");
            }
            finally
            {
                Object.Destroy(behind);
                Object.Destroy(side);
            }
        }

        sealed class StaggerProbe : MonoBehaviour, IStaggerable
        {
            public float Seconds;
            public void Stagger(float seconds) => Seconds = seconds;
        }

        // E3: defender justo antes del golpe lo anula y aturde al atacante; tarde, solo reduce; pulsar sin parar no sirve.
        [UnityTest]
        public IEnumerator PerfectBlock_NegatesTheHit_StaggersTheAttacker_AndCannotBeSpammed()
        {
            var defense = player.AddComponent<PlayerDefense>();
            var attacker = CreateDummy(new Vector3(0f, 0f, -1f), out _);
            var probe = attacker.AddComponent<StaggerProbe>();
            var gauge = BreakGauge.Ensure(attacker, "Diana", 3, 1f, Affinity.Luz);
            var blocks = 0;
            defense.PerfectBlocked += () => blocks++;
            try
            {
                yield return new WaitForFixedUpdate();
                defense.Held = true;
                yield return null;
                Assert.IsTrue(defense.InPerfectWindow);
                Assert.IsFalse(playerHealth.TryDamage(10, attacker.transform.position), "El bloqueo perfecto anula el golpe.");
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max));
                Assert.That(probe.Seconds, Is.GreaterThan(0f), "Aturde a quien golpeó.");
                Assert.That(gauge.Shield, Is.EqualTo(2), "Y le quita un punto de escudo.");
                Assert.That(blocks, Is.EqualTo(1));

                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(Time.timeScale, Is.EqualTo(1f), "La pausa del bloqueo termina sola.");
                yield return new WaitForSeconds(0.25f);
                Assert.IsFalse(defense.InPerfectWindow);
                Assert.IsTrue(playerHealth.TryDamage(10, attacker.transform.position), "Tarde, la defensa solo reduce.");
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max - 5));

                defense.Held = false;
                yield return null;
                defense.Held = true;
                yield return null;
                Assert.IsFalse(defense.InPerfectWindow, "Soltar y volver a pulsar enseguida no rearma la ventana.");
            }
            finally
            {
                Object.Destroy(attacker);
            }
        }

        // E4: solo los golpes débiles quitan escudo; sin escudo queda roto (aturdido, más daño) y luego se recupera.
        [UnityTest]
        public IEnumerator BreakGauge_WeakHitsBreakTheShield_WhichStunsAndRaisesDamage_ThenRecovers()
        {
            var enemy = CreateDummy(new Vector3(0f, 0f, -1.5f), out var enemyHealth);
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(enemyHealth, 0f);
            var probe = enemy.AddComponent<StaggerProbe>();
            var gauge = BreakGauge.Ensure(enemy, "Diana", 2, 0.5f, Affinity.Luz, Affinity.Agua);
            var center = enemy.transform.position;
            try
            {
                yield return new WaitForFixedUpdate();
                attack.StrikeArea(center, 0.5f, 5, Affinity.Tierra);
                Assert.That(gauge.Shield, Is.EqualTo(2), "Un golpe que no es débil no quita escudo.");
                Assert.IsTrue(BreakReadout.Current.HasValue, "El HUD muestra al enemigo golpeado.");
                Assert.That(BreakReadout.Current.Value.Name, Is.EqualTo("Diana"));
                CollectionAssert.AreEqual(new[] { "Luz", "Agua" }, BreakReadout.Current.Value.Weaknesses);

                attack.StrikeArea(center, 0.5f, 5, Affinity.Luz);
                Assert.That(gauge.Shield, Is.EqualTo(1));
                Assert.IsFalse(gauge.IsBroken);
                attack.StrikeArea(center, 0.5f, 5, Affinity.Agua);
                Assert.IsTrue(gauge.IsBroken, "Sin escudo queda roto.");
                Assert.IsTrue(BreakReadout.Current.Value.Broken);
                Assert.That(probe.Seconds, Is.EqualTo(0.5f), "Roto, queda aturdido lo que dura la ruptura.");

                var before = enemyHealth.Current;
                attack.StrikeArea(center, 0.5f, 10, Affinity.Tierra);
                Assert.That(before - enemyHealth.Current, Is.EqualTo(15), "Roto recibe ×1,5.");

                yield return new WaitForSecondsRealtime(0.2f);
                yield return new WaitForSeconds(0.6f);
                Assert.IsFalse(gauge.IsBroken, "La ruptura termina sola.");
                Assert.That(gauge.Shield, Is.EqualTo(2), "Y el escudo vuelve entero.");
                Assert.That(enemyHealth.Vulnerability, Is.EqualTo(1f));
            }
            finally
            {
                Object.Destroy(enemy);
            }
            yield return null;
            Assert.IsFalse(BreakReadout.Current.HasValue, "Al desaparecer el enemigo, el HUD lo oculta.");
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

                yield return new WaitForSeconds(0.4f); // termina la invulnerabilidad (0.5 s) que dejó el golpe
                Assert.IsTrue(enemyHealth.TryDamage(100, player.transform.position));
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
