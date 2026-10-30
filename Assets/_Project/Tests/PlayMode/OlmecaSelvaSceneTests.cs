using System.Collections;
using System.Linq;
using System.Reflection;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.Gameplay.Narrative;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.Gameplay.Travel;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Selva al oeste de la ciudad olmeca (OlmecaJungleGenerator, desde Selva.json) y viaje entre ambas escenas
    /// (MP_OLM_03, M1). Las posiciones del flujo de arte (x, y, z) se convierten con IsometricProjection.FromPipeline.
    /// </summary>
    public class OlmecaSelvaSceneTests
    {
        const string JungleScene = "Olmeca_Selva";
        const string CityScene = "Olmeca_Ciudad";
        static readonly Vector2 EntrancePoint = new(-14.5f, -2.3f); // Entrada_Estructura_01 en planificar_selva

        static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(JungleScene, LoadSceneMode.Additive);
            yield return null;
        }

        static IEnumerator UnloadScene()
        {
            yield return SceneManager.UnloadSceneAsync(JungleScene);
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

        static DialogueInteractable FindWarrior() => Object.FindObjectsByType<DialogueInteractable>(FindObjectsSortMode.None)
            .SingleOrDefault(d => ((NpcData)typeof(DialogueInteractable)
                .GetField("npc", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d))?.Id == "guerrero_joven");

        static void TalkUntilTheEnd(DialogueInteractable dialogue, Transform player)
        {
            dialogue.Interact(player);
            var guard = 0;
            while (dialogue.Advance())
                Assert.Less(++guard, 20, "La conversación no termina.");
            dialogue.End();
        }

        [UnityTest]
        public IEnumerator Scene_Loads_WithSpiritsInTheClearings_AndTheEntranceBlocking()
        {
            yield return LoadScene();
            yield return new WaitForSeconds(1f); // si no hubiera suelo, la gravedad los habría hecho caer

            var player = Object.FindAnyObjectByType<PlayerMovement>();
            Assert.IsNotNull(player, "La escena no tiene jugador.");
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.1f), "El jugador no está sobre el suelo.");
            Assert.IsNotNull(Camera.main?.GetComponent<IsometricCameraFollow>(), "Falta la cámara isométrica.");

            var spirits = Object.FindObjectsByType<EnemyChaser>(FindObjectsSortMode.None);
            Assert.AreEqual(3, spirits.Length, "Debe haber un espíritu corrompido por claro.");
            foreach (var spirit in spirits)
            {
                Assert.That(spirit.transform.position.y, Is.EqualTo(0f).Within(0.1f), $"{spirit.name} no está sobre el suelo.");
                Assert.IsNotNull(spirit.GetComponent<CorruptionPulse>(), $"{spirit.name} no muestra la corrupción.");
                Assert.AreEqual(LayerMask.NameToLayer("Enemigo"), spirit.gameObject.layer);
            }
            CollectionAssert.AllItemsAreUnique(spirits.Select(s => s.GetComponent<ReportOnDefeat>().FlagOnDefeat));

            var fragments = Object.FindObjectsByType<MemoryFragmentInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(3, fragments.Length);
            Assert.IsTrue(fragments.All(f => !f.gameObject.activeInHierarchy), "Los fragmentos aparecen al vencer a su espíritu.");
            Assert.IsNull(FindWarrior(), "Sin MP_OLM_03 el guerrero sigue en la ciudad.");

            // Sin MP_OLM_03 completada, la abertura de la entrada no lleva a la mazmorra.
            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Single();
            Assert.AreEqual(CityScene, portal.TargetScene);
            var spawn = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s => s.SpawnId == "desde_ciudad");
            Assert.Less(Vector3.Distance(player.transform.position, spawn.transform.position), 0.5f,
                "Al pulsar Play en la selva el jugador empieza donde llega desde la ciudad.");

            var origin = IsometricProjection.FromPipeline(EntrancePoint.x, EntrancePoint.y, 20f);
            Assert.IsTrue(Physics.Raycast(origin, Vector3.down, out var hit, 40f));
            Assert.Greater(hit.point.y, 1f, "El montículo de la entrada debe bloquear el paso.");

            yield return UnloadScene();
        }

        // MP_OLM_03 de principio a fin, con servicios propios registrados antes de cargar (ActiveWhenFlags se queda con
        // el StoryState que encuentra al empezar) para no dejar la misión completada en los de Boot.
        [UnityTest]
        public IEnumerator JungleQuest_SpiritsLeaveFragments_AndTheWarriorRecognisesThePlace()
        {
            ServiceLocator.TryGet(out StoryState bootStory);
            ServiceLocator.TryGet(out QuestService bootQuests);
            var catalog = Resources.Load<QuestCatalog>(QuestCatalog.ResourcePath);
            var story = new StoryState();
            var quests = new QuestService(story, catalog);
            ServiceLocator.Register(story);
            ServiceLocator.Register(quests);
            try
            {
                var quest = catalog.Find("MP_OLM_03");
                Assert.IsNotNull(quest, "MP_OLM_03 no está en el catálogo de misiones.");
                story.SetFlag("olm.guerrero_en_selva"); // se lo dio el guerrero en la ciudad
                quests.Start(quest);

                yield return LoadScene();
                yield return null;
                var player = Object.FindAnyObjectByType<PlayerMovement>();
                var warrior = FindWarrior();
                Assert.IsNotNull(warrior, "El guerrero debe esperar en la entrada.");

                // Antes de los fragmentos el guerrero no cierra la misión.
                TalkUntilTheEnd(warrior, player.transform);
                Assert.AreEqual(0, quests.GetStep("MP_OLM_03"));

                var zone = Object.FindObjectsByType<ReportOnEnter>(FindObjectsSortMode.None).Single(z => z.ZoneId == "selva");
                Teleport(player, new Vector3(zone.transform.position.x, 0f, zone.transform.position.z));
                yield return WaitPhysics();
                Assert.AreEqual(1, quests.GetStep("MP_OLM_03"), "Entrar en la selva no cumplió el primer paso.");

                var fragments = Object.FindObjectsByType<MemoryFragmentInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var spirit in Object.FindObjectsByType<EnemyChaser>(FindObjectsSortMode.None))
                {
                    var clearing = spirit.transform.parent;
                    var fragment = fragments.Single(f => f.transform.IsChildOf(clearing));
                    Assert.IsFalse(fragment.gameObject.activeInHierarchy);

                    Assert.IsTrue(spirit.GetComponent<Health>().TryDamage(100, player.transform.position));
                    yield return null;
                    Assert.IsFalse(spirit.gameObject.activeSelf, $"{spirit.name} no debe volver tras caer.");
                    Assert.IsTrue(fragment.gameObject.activeInHierarchy, $"{spirit.name} no dejó su fragmento.");

                    fragment.Interact(player.transform);
                    fragment.End();
                    yield return null;
                    Assert.IsFalse(fragment.gameObject.activeInHierarchy, "El fragmento recogido debe desaparecer.");
                }

                Assert.AreEqual(2, quests.GetStep("MP_OLM_03"), "Los tres fragmentos no cumplieron el segundo paso.");
                TalkUntilTheEnd(warrior, player.transform);
                Assert.AreEqual(QuestStatus.Completed, quests.GetStatus("MP_OLM_03"));
                Assert.IsTrue(story.HasFlag("olm.estructura_encontrada"));
            }
            finally
            {
                if (bootStory != null)
                    ServiceLocator.Register(bootStory);
                if (bootQuests != null)
                    ServiceLocator.Register(bootQuests);
            }

            yield return UnloadScene();
        }

        // Ciudad → selva → ciudad por los portales del sendero del oeste: cada escena sustituye a la otra, el jugador
        // aparece en el punto de llegada y conserva la vida que tenía (Diseno.md §9).
        [UnityTest]
        public IEnumerator Travel_CityToJungleAndBack_KeepsThePlayersHealth()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SceneTravel travel), "Boot no registró SceneTravel.");
            yield return SceneManager.LoadSceneAsync(CityScene, LoadSceneMode.Additive);
            yield return null;

            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var health = player.GetComponent<Health>();
            Assert.IsTrue(health.TryDamage(2, player.transform.position + Vector3.right));
            var expected = health.Current;

            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Single();
            Assert.AreEqual(JungleScene, portal.TargetScene);
            Teleport(player, portal.transform.position - Vector3.up * portal.transform.position.y);
            yield return WaitPhysics();
            Assert.IsTrue(travel.IsTraveling, "El portal de la ciudad no inició el viaje.");
            while (travel.IsTraveling)
                yield return null;

            Assert.IsFalse(SceneManager.GetSceneByName(CityScene).isLoaded, "La ciudad debe descargarse.");
            Assert.AreEqual(JungleScene, SceneManager.GetActiveScene().name);
            player = Object.FindAnyObjectByType<PlayerMovement>();
            var arrival = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s => s.SpawnId == "desde_ciudad");
            Assert.Less(Vector3.Distance(player.transform.position, arrival.transform.position), 0.5f);
            Assert.AreEqual(expected, player.GetComponent<Health>().Current, "La vida no debe recuperarse al cambiar de escena.");
            Assert.IsTrue(player.GetComponent<PlayerMovement>().enabled);

            portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Single();
            Teleport(player, portal.transform.position - Vector3.up * portal.transform.position.y);
            yield return WaitPhysics();
            while (travel.IsTraveling)
                yield return null;

            Assert.IsFalse(SceneManager.GetSceneByName(JungleScene).isLoaded, "La selva debe descargarse.");
            Assert.AreEqual(CityScene, SceneManager.GetActiveScene().name);
            player = Object.FindAnyObjectByType<PlayerMovement>();
            arrival = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s => s.SpawnId == "desde_selva");
            Assert.Less(Vector3.Distance(player.transform.position, arrival.transform.position), 0.5f);
            Assert.AreEqual(expected, player.GetComponent<Health>().Current);

            yield return SceneManager.UnloadSceneAsync(CityScene);
        }
    }
}
