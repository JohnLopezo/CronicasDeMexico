using System.Collections;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Narrative;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.Gameplay.Travel;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Pisos 1 y 2 de la mazmorra (M3): esquiva, tutorial de El Umbral, agua baja y ecos borrados del Pantano, y el Jaguar
    /// de Piedra con su arena, la bajada cerrada hasta vencerlo y la misión MP_OLM_04. Usan servicios de historia propios
    /// (ActiveWhenFlags se queda con el StoryState que encuentra al empezar) para no tocar el estado de Boot.
    /// </summary>
    public class OlmecaMazmorraCombatTests
    {
        const string Floor1 = "Olmeca_Mazmorra_P1";
        const string Floor2 = "Olmeca_Mazmorra_P2";
        const string Floor3 = "Olmeca_Mazmorra_P3";
        const string JaguarDefeatedFlag = "olm.jaguar_piedra_vencido";

        StoryState bootStory;
        QuestService bootQuests;
        StoryState story;
        QuestService quests;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.TryGet(out bootStory);
            ServiceLocator.TryGet(out bootQuests);
            story = new StoryState();
            quests = new QuestService(story, Resources.Load<QuestCatalog>(QuestCatalog.ResourcePath));
            ServiceLocator.Register(story);
            ServiceLocator.Register(quests);
        }

        [TearDown]
        public void TearDown()
        {
            if (bootStory != null)
                ServiceLocator.Register(bootStory);
            if (bootQuests != null)
                ServiceLocator.Register(bootQuests);
        }

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
            yield return null; // ActiveWhenFlags espera a StoryState en su primer Update
        }

        static void Teleport(Component character, Vector3 position)
        {
            var controller = character.GetComponent<CharacterController>();
            controller.enabled = false;
            character.transform.position = position;
            controller.enabled = true;
        }

        static IEnumerator WaitPhysics()
        {
            for (var i = 0; i < 4; i++)
                yield return new WaitForFixedUpdate();
        }

        // Desplaza al personaje con su CharacterController, como al caminar (dispara los triggers de salida).
        static IEnumerator Walk(Component character, Vector3 offset)
        {
            var controller = character.GetComponent<CharacterController>();
            const int steps = 10;
            for (var i = 0; i < steps; i++)
            {
                controller.Move(offset / steps);
                yield return new WaitForFixedUpdate();
            }
            yield return WaitPhysics();
        }

        static PlayerMovement Player() => Object.FindAnyObjectByType<PlayerMovement>();

        static Vector3 Flow(float x, float y) => IsometricProjection.FromPipeline(x, y, 0f);

        static StringEventChannel MessageChannel() =>
            Object.FindAnyObjectByType<TutorialHint>().MessageChannel;

        static bool BossBarVisible() => Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
            .Select(document => document.rootVisualElement?.Q("boss"))
            .Any(boss => boss != null && !boss.ClassListContains("hidden"));

        [UnityTest]
        public IEnumerator Dodge_MovesFast_IsInvulnerable_AndHasACooldown()
        {
            yield return Load(Floor1);
            var player = Player();
            var health = player.GetComponent<Health>();
            Teleport(player, Flow(-11.5f, 0f));
            yield return WaitPhysics();
            var start = player.transform.position;

            Assert.IsTrue(player.TryDodge(Flow(1f, 0f) - Flow(0f, 0f)));
            Assert.IsTrue(player.IsDodging);
            Assert.IsFalse(health.TryDamage(1, start), "Durante la esquiva no se recibe daño.");
            Assert.IsFalse(player.TryDodge(Vector3.zero), "La esquiva tiene un tiempo de espera.");

            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(player.IsDodging);
            Assert.Greater(Vector3.Distance(start, player.transform.position), 1.4f, "La esquiva debe recorrer más que el paso.");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.TryDodge(Vector3.zero), "Pasada la espera se puede volver a esquivar.");
        }

        // La pista se muestra al entrar en su zona una sola vez en la partida y se quita al salir.
        [UnityTest]
        public IEnumerator TutorialHint_ShowsOnce_AndHidesWhenLeaving()
        {
            yield return Load(Floor1);
            var player = Player();
            var hint = Object.FindObjectsByType<TutorialHint>(FindObjectsSortMode.None).Single(h => h.HintId == "atacar");
            string shown = null;
            var channel = MessageChannel();
            void Listen(string text) => shown = text;
            channel.Subscribe(Listen);
            try
            {
                Teleport(player, new Vector3(hint.transform.position.x, 0f, hint.transform.position.z));
                yield return WaitPhysics();
                Assert.AreEqual(hint.Text, shown);
                Assert.IsTrue(story.HasFlag(hint.Flag));

                yield return Walk(player, Flow(-2f, 0f) - Flow(0f, 0f)); // de vuelta al pasillo
                Assert.IsNull(shown, "Al salir de la zona la pista se quita.");

                Teleport(player, new Vector3(hint.transform.position.x, 0f, hint.transform.position.z));
                yield return WaitPhysics();
                Assert.IsNull(shown, "La pista solo se muestra una vez.");
            }
            finally
            {
                channel.Unsubscribe(Listen);
            }
        }

        [UnityTest]
        public IEnumerator ShallowWater_SlowsThePlayer_UntilLeavingIt()
        {
            yield return Load(Floor2);
            foreach (var echo in Object.FindObjectsByType<ErasedEcho>(FindObjectsSortMode.None))
                echo.gameObject.SetActive(false); // uno aparece junto al charco y estorbaría el paso
            var player = Player();
            Assert.AreEqual(1f, player.SpeedMultiplier);
            Teleport(player, Flow(-3.5f, 4f)); // charco junto a la entrada de la sala inundada
            yield return WaitPhysics();
            Assert.Less(player.SpeedMultiplier, 1f, "El agua baja debe frenar.");

            yield return Walk(player, Flow(2f, 0f) - Flow(0f, 0f)); // a la losa seca de al lado
            Assert.AreEqual(1f, player.SpeedMultiplier, "Fuera del agua se recupera la velocidad.");

            // También si sale de golpe, sin pasar por el borde (reaparecer tras caer, por ejemplo).
            Teleport(player, Flow(-3.5f, 4f));
            yield return WaitPhysics();
            Assert.Less(player.SpeedMultiplier, 1f);
            Teleport(player, Flow(-12f, 1f));
            yield return WaitPhysics();
            Assert.AreEqual(1f, player.SpeedMultiplier, "Al salir de golpe del agua también se recupera la velocidad.");
        }

        // El eco avisa y dispara; al recibir un golpe se desvanece y reaparece en otro sitio.
        [UnityTest]
        public IEnumerator ErasedEcho_Shoots_AndFadesToAnotherPlaceWhenHit()
        {
            yield return Load(Floor2);
            var player = Player();
            var health = player.GetComponent<Health>();
            var echo = Object.FindObjectsByType<ErasedEcho>(FindObjectsSortMode.None).OrderBy(e => e.name).First();
            var other = Object.FindObjectsByType<ErasedEcho>(FindObjectsSortMode.None).Single(e => e != echo);
            other.gameObject.SetActive(false); // que dispare solo uno

            var start = echo.transform.position;
            Teleport(player, start + (Flow(0f, 0f) - Flow(0f, 3f)));
            var before = health.Current;
            yield return new WaitForSeconds(2.5f);
            Assert.Less(health.Current, before, "El fragmento del eco debe alcanzar al jugador quieto.");

            Assert.IsTrue(echo.GetComponent<Health>().TryDamage(1, player.transform.position));
            Assert.IsTrue(echo.IsFaded, "Al recibir un golpe se desvanece.");
            Assert.IsFalse(echo.GetComponent<Health>().TryDamage(1, player.transform.position), "Desvanecido no se le puede dañar.");
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(echo.IsFaded);
            Assert.Greater(Vector3.Distance(start, echo.transform.position), 1f, "Debe reaparecer en otro de sus puntos.");
        }

        // Arena del Jaguar de Piedra y MP_OLM_04: despierta con barra en el HUD, avisa el salto con una marca donde
        // estaba el jugador y cae allí; vencido, avisa a la misión, queda liberado y la bajada se abre.
        [UnityTest]
        public IEnumerator StoneJaguar_TelegraphsItsLeap_AndOpensTheWayDownWhenDefeated()
        {
            var quest = Resources.Load<QuestCatalog>(QuestCatalog.ResourcePath).Find("MP_OLM_04");
            Assert.IsNotNull(quest, "MP_OLM_04 no está en el catálogo de misiones.");
            quests.Start(quest);
            story.Report(StoryEvents.Reach("mazmorra_piso1"));

            yield return Load(Floor2);
            yield return WaitPhysics();
            Assert.AreEqual(2, quests.GetStep("MP_OLM_04"), "Llegar al Pantano cumple el segundo paso.");

            var jaguar = Object.FindAnyObjectByType<StoneJaguar>();
            Assert.IsNotNull(jaguar, "Falta el Jaguar de Piedra.");
            Assert.IsFalse(Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Any(p => p.TargetScene == Floor3),
                "La bajada está tapada hasta vencer al Jaguar.");
            Assert.IsFalse(BossBarVisible());

            // Lejos del zarpazo y a tiro de salto, aunque se acerque acechando antes del primer ataque.
            var player = Player();
            var health = player.GetComponent<Health>();
            Teleport(player, jaguar.transform.position + (Flow(-5.5f, 0f) - Flow(0f, 0f)));
            yield return WaitPhysics();
            var playerSpot = player.transform.position;
            jaguar.Engage();
            yield return null;
            Assert.IsTrue(BossBarVisible(), "Al despertar muestra su barra.");

            var guard = 0f;
            while (!jaguar.Marker.IsShowing)
            {
                guard += Time.deltaTime;
                Assert.Less(guard, 3f, "El Jaguar no avisó ningún ataque.");
                yield return null;
            }
            Assert.IsTrue(jaguar.Marker.Contains(playerSpot), "El salto se avisa donde está el jugador.");
            var before = health.Current;
            yield return new WaitForSeconds(1.6f); // aviso del salto (0,8 s) y vuelo (0,5 s)
            Assert.Less(Vector3.Distance(jaguar.transform.position, playerSpot), 1f, "Debe caer donde avisó.");
            Assert.Less(health.Current, before, "Quedarse en la marca hace daño.");

            var jaguarHealth = jaguar.GetComponent<Health>();
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(jaguarHealth.TryDamage(jaguarHealth.Max, player.transform.position));
            yield return null;
            Assert.IsTrue(story.HasFlag(JaguarDefeatedFlag));
            Assert.IsFalse(jaguar.gameObject.activeInHierarchy, "Vencido, el Jaguar no vuelve.");
            Assert.IsFalse(BossBarVisible(), "Vencido, la barra se oculta.");
            Assert.AreEqual(3, quests.GetStep("MP_OLM_04"), "Vencer al Jaguar cumple el tercer paso.");
            Assert.IsTrue(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Any(r => r.sprite != null && r.sprite.name == "Jaguar_Piedra_Liberado_01"), "Queda la escultura liberada.");
            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).SingleOrDefault(p => p.TargetScene == Floor3);
            Assert.IsNotNull(portal, "Vencido el Jaguar, la bajada se abre.");
        }

        // Tras vencerlo, volver al piso (p. ej. tras una derrota) no lo trae de vuelta.
        [UnityTest]
        public IEnumerator StoneJaguar_StaysDefeated_WhenTheFloorReloads()
        {
            story.SetFlag(JaguarDefeatedFlag);
            yield return Load(Floor2);
            Assert.IsNull(Object.FindAnyObjectByType<StoneJaguar>(), "El Jaguar vencido no reaparece.");
            Assert.IsTrue(Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Any(p => p.TargetScene == Floor3));
        }
    }
}
