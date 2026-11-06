using System.Collections;
using System.Linq;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Etapas A y B del plan de personajes: el jugador es el jaguar (sprites del flujo, sin escalar, animaciones de
    /// zarpazo, galope, golpe y muerte) y el espíritu corrompido es el jaguar corrupto, con su salto avisado por una
    /// franja, que un golpe interrumpe, y su caída visible al morir.
    /// </summary>
    public class OlmecaJaguarTests
    {
        const string Floor1 = "Olmeca_Mazmorra_P1";

        [UnityTearDown]
        public IEnumerator UnloadRegions()
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("Olmeca_"))
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            yield return null;
            yield return null;
        }

        static void Teleport(Component character, Vector3 position)
        {
            var controller = character.GetComponent<CharacterController>();
            controller.enabled = false;
            character.transform.position = position;
            controller.enabled = true;
        }

        static string ActionName(DirectionalSpriteAnimator animator) =>
            animator.CurrentAction != null ? animator.CurrentAction.name : null;

        static EnemyChaser Spirit(string name) =>
            Object.FindObjectsByType<EnemyChaser>(FindObjectsSortMode.None).Single(e => e.name == name);

        [UnityTest]
        public IEnumerator Player_IsBalam_AndAnimatesItsActions()
        {
            yield return Load(Floor1);
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var animator = player.GetComponent<DirectionalSpriteAnimator>();
            var spriteRenderer = player.GetComponentInChildren<SpriteBillboard>().GetComponent<SpriteRenderer>();

            Assert.AreEqual(Vector3.one, player.transform.localScale, "Los sprites de los guerreros no se escalan.");
            Assert.AreEqual(IsometricProjection.PixelsPerUnit, spriteRenderer.sprite.pixelsPerUnit, "A la resolución del escenario.");
            StringAssert.StartsWith("Guerrero_Balam_", spriteRenderer.sprite.name);
            Assert.AreEqual(1.1f, player.GetComponent<CharacterController>().height, 0.01f, "Cápsula de figura humana.");
            yield return new WaitForSeconds(0.4f);
            StringAssert.StartsWith("Guerrero_Balam_Reposo", spriteRenderer.sprite.name);

            player.GetComponent<PlayerAttack>().Strike();
            Assert.AreEqual("DSS_Guerrero_Balam_Ataque", ActionName(animator));
            yield return null;
            StringAssert.StartsWith("Guerrero_Balam_Ataque", spriteRenderer.sprite.name);
            yield return new WaitForSeconds(0.4f);
            Assert.IsNull(animator.CurrentAction, "El ataque termina solo.");

            Assert.IsTrue(player.TryDodge(Vector3.left)); // hacia la cámara: la escalera de subida, con su portal, queda al fondo
            yield return null;
            Assert.AreEqual("DSS_Guerrero_Balam_Correr", ActionName(animator), "La esquiva usa la carrera.");
            yield return new WaitForSeconds(0.6f);
            Assert.IsNull(animator.CurrentAction);

            var health = player.GetComponent<Health>();
            Assert.IsTrue(health.TryDamage(10, player.transform.position + Vector3.right));
            yield return null;
            Assert.AreEqual("DSS_Guerrero_Balam_Dano", ActionName(animator));

            yield return new WaitForSeconds(1.1f); // invulnerable tras el golpe
            Assert.IsTrue(health.TryDamage(100000, player.transform.position + Vector3.right));
            yield return null;
            Assert.AreEqual("DSS_Guerrero_Balam_Muerte", ActionName(animator), "Cae con su hoja de muerte.");
        }

        [UnityTest]
        public IEnumerator Defense_RootsThePlayer_HalvesTheDamage_AndShowsItsPose()
        {
            yield return Load(Floor1);
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var defense = player.GetComponent<PlayerDefense>();
            Assert.IsNotNull(defense, "El prefab del jugador debe poder defender.");
            var animator = player.GetComponent<DirectionalSpriteAnimator>();
            var health = player.GetComponent<Health>();
            var source = player.transform.position + Vector3.right;
            health.Defense = 0;

            defense.Held = true;
            yield return null;
            yield return null;
            Assert.IsTrue(defense.IsDefending);
            Assert.AreEqual("DSS_Guerrero_Balam_Defensa", ActionName(animator));
            Assert.IsTrue(player.Rooted, "Quieto mientras defiende.");
            Assert.IsFalse(player.TryDodge(Vector3.left), "Defendiendo no esquiva.");

            var before = health.Current;
            Assert.IsTrue(health.TryDamage(11, source));
            Assert.AreEqual(before - 6, health.Current, "Mitad de 11, redondeada hacia arriba.");

            defense.Held = false;
            yield return null;
            yield return null;
            Assert.IsFalse(defense.IsDefending);
            Assert.IsFalse(player.Rooted);
            Assert.AreNotEqual("DSS_Guerrero_Balam_Defensa", ActionName(animator));
            yield return new WaitForSeconds(1.1f); // invulnerable tras el golpe
            before = health.Current;
            Assert.IsTrue(health.TryDamage(4, source));
            Assert.AreEqual(before - 4, health.Current, "Sin defender, daño completo.");
        }

        [UnityTest]
        public IEnumerator CorruptedJaguar_TelegraphsItsLeap_HitsOnTheLine_AndCanBeInterrupted()
        {
            yield return Load(Floor1);
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var playerHealth = player.GetComponent<Health>();
            var spirit = Spirit("EspirituCorrompido_1");
            var animator = spirit.GetComponent<DirectionalSpriteAnimator>();
            Assert.IsTrue(spirit.Lunges, "El jaguar corrupto salta.");
            Assert.IsNotNull(spirit.LungeMarker);
            StringAssert.StartsWith("JaguarCorrupto_", spirit.GetComponentInChildren<SpriteRenderer>().sprite.name);

            // A 2,4 m, dentro del alcance del salto: se agazapa y avisa con la franja.
            Teleport(player, IsometricProjection.FromPipeline(1.4f, 0.5f, 0f));
            var guard = 0f;
            while (spirit.Lunge != EnemyChaser.LungePhase.Windup)
            {
                guard += Time.deltaTime;
                Assert.Less(guard, 2f, "El jaguar corrupto no avisó el salto.");
                yield return null;
            }
            yield return null;
            Assert.IsTrue(spirit.LungeMarker.IsShowing, "La franja avisa la trayectoria.");
            Assert.AreEqual(TelegraphMarker.MarkerShape.Line, spirit.LungeMarker.Shape);
            Assert.IsTrue(spirit.LungeMarker.Contains(player.transform.position), "La franja apunta al jugador.");
            Assert.AreEqual("DSS_JaguarCorrupto_Acecho", ActionName(animator));

            var before = playerHealth.Current;
            guard = 0f;
            while (spirit.Lunge != EnemyChaser.LungePhase.Recover)
            {
                guard += Time.deltaTime;
                Assert.Less(guard, 2f, "El salto no terminó.");
                yield return null;
            }
            Assert.Less(playerHealth.Current, before, "Quedarse en la franja hace daño.");

            // Tras la recuperación (0,7 s) y el enfriamiento (2,5 s) vuelve a avisar si el jugador está a media distancia;
            // un golpe durante el aviso lo interrumpe.
            yield return new WaitForSeconds(3.4f);
            Teleport(spirit, IsometricProjection.FromPipeline(-1f, 0.5f, 0f));
            Teleport(player, IsometricProjection.FromPipeline(1.4f, 0.5f, 0f));
            guard = 0f;
            while (spirit.Lunge != EnemyChaser.LungePhase.Windup)
            {
                guard += Time.deltaTime;
                Assert.Less(guard, 4f, "No volvió a avisar el salto.");
                yield return null;
            }
            Assert.IsTrue(spirit.GetComponent<Health>().TryDamage(5, player.transform.position));
            Assert.AreEqual(EnemyChaser.LungePhase.None, spirit.Lunge, "Un golpe durante el aviso cancela el salto.");
            yield return null;
            Assert.IsFalse(spirit.LungeMarker.IsShowing);
        }

        [UnityTest]
        public IEnumerator CorruptedJaguar_ShowsItsFall_ThenDisappears()
        {
            yield return Load(Floor1);
            var spirit = Spirit("EspirituCorrompido_2");
            var animator = spirit.GetComponent<DirectionalSpriteAnimator>();
            var spriteRenderer = spirit.GetComponentInChildren<SpriteBillboard>().GetComponent<SpriteRenderer>();

            Assert.IsTrue(spirit.GetComponent<Health>().TryDamage(1000, spirit.transform.position + Vector3.right));
            yield return null;
            Assert.AreEqual("DSS_JaguarCorrupto_Muerte", ActionName(animator));
            Assert.IsTrue(spriteRenderer.enabled, "Se ve caer.");
            Assert.IsFalse(spirit.GetComponent<CharacterController>().enabled, "Vencido no estorba.");
            yield return new WaitForSeconds(1.4f);
            Assert.IsFalse(spriteRenderer.enabled, "Después desaparece.");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(spirit.GetComponent<Health>().IsDead, "En la mazmorra no reaparece.");
        }
    }
}
