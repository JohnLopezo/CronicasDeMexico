using System.Collections;
using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Piso 3 de la mazmorra (M4): la losa sellada de la antesala, la arena y el Guardián de la Piedra Viva (fases,
    /// ataques avisados, liberación) y la misión MP_OLM_05. Usan servicios de historia propios para no tocar Boot.
    /// </summary>
    public class OlmecaGuardianTests
    {
        const string Floor3 = "Olmeca_Mazmorra_P3";
        const string SealOpenFlag = "olm.sello_abierto";
        const string FreedFlag = "olm.guardian_liberado";
        static readonly Vector2 ArenaCenter = new(5f, 13f); // "guardian" en PISOS[3] de generar_olmeca.py

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

        static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(Floor3, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(Floor3));
            yield return null;
            yield return null; // ActiveWhenFlags y StartQuestOnLoad esperan a los servicios en su primer Update
        }

        static void Teleport(Component character, Vector3 position)
        {
            var controller = character.GetComponent<CharacterController>();
            controller.enabled = false;
            character.transform.position = position;
            controller.enabled = true;
        }

        static Vector3 Flow(float x, float y) => IsometricProjection.FromPipeline(x, y, 0f);

        static PlayerMovement Player() => Object.FindAnyObjectByType<PlayerMovement>();

        // El Guardián despierto y el jugador a <paramref name="distance"/> m de él, hacia la entrada de la arena (-Y).
        IEnumerator FaceGuardian(float distance)
        {
            story.SetFlag(SealOpenFlag);
            yield return Load();
            var guardian = Object.FindAnyObjectByType<StoneGuardian>();
            Assert.IsNotNull(guardian, "Falta el Guardián de la Piedra Viva.");
            Teleport(Player(), Flow(ArenaCenter.x, ArenaCenter.y - distance));
            guardian.Engage();
            yield return new WaitForFixedUpdate();
        }

        static IEnumerator WaitWhileBusy(StoneGuardian guardian, float limit = 6f)
        {
            var elapsed = 0f;
            while (guardian.IsBusy)
            {
                elapsed += Time.deltaTime;
                Assert.Less(elapsed, limit, "El ataque no termina.");
                yield return null;
            }
        }

        // La losa cierra el paso; al examinarla y cerrar el mensaje se abre, y cuenta para MP_OLM_05, que empieza sola.
        [UnityTest]
        public IEnumerator Seal_BlocksTheArena_UntilPushed()
        {
            yield return Load();
            Assert.AreEqual(QuestStatus.Active, quests.GetStatus("MP_OLM_05"), "MP_OLM_05 empieza al llegar a la antesala.");
            var seal = Object.FindAnyObjectByType<SealedPassage>();
            Assert.IsNotNull(seal, "Falta la losa sellada.");

            // Un rayo a la altura del pecho por el pasillo choca con el bloqueo.
            var from = Flow(5f, 3f) + Vector3.up;
            var towards = (Flow(5f, 7f) - Flow(5f, 3f)).normalized;
            Assert.IsTrue(Physics.Raycast(from, towards, 4f, 1, QueryTriggerInteraction.Ignore), "La losa debe cerrar el paso.");

            seal.Interact(Player().transform);
            seal.End();
            yield return null;
            Assert.IsTrue(story.HasFlag(SealOpenFlag));
            Assert.IsFalse(seal.gameObject.activeInHierarchy, "Abierta, la losa desaparece.");
            Assert.IsFalse(Physics.Raycast(from, towards, 4f, 1, QueryTriggerInteraction.Ignore), "Abierta, el paso queda libre.");
            Assert.AreEqual(1, quests.GetStep("MP_OLM_05"), "Abrir la losa cumple el primer paso.");
        }

        // Al 60 % entra en la fase 2 (sube el agua y queda invulnerable un momento); al 25 %, en la 3.
        [UnityTest]
        public IEnumerator Guardian_ChangesPhase_AndFloodsTheArena()
        {
            yield return FaceGuardian(5.5f);
            var guardian = Object.FindAnyObjectByType<StoneGuardian>();
            var health = guardian.GetComponent<Health>();
            Assert.AreEqual(1, guardian.Phase);
            Assert.IsFalse(FloodActive(), "El agua no sube hasta la fase 2.");

            Assert.IsTrue(health.TryDamage(Mathf.CeilToInt(health.Max * 0.4f), Player().transform.position));
            Assert.AreEqual(2, guardian.Phase);
            Assert.IsTrue(FloodActive(), "En la fase 2 sube el agua.");
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(health.TryDamage(1, Player().transform.position), "Al cambiar de fase queda invulnerable un momento.");

            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(health.TryDamage(health.Current - Mathf.FloorToInt(health.Max * 0.25f), Player().transform.position));
            Assert.AreEqual(3, guardian.Phase);
        }

        static bool FloodActive() => Object.FindObjectsByType<SlowZone>(FindObjectsSortMode.None).Length > 0;

        // La embestida se avisa con una franja hacia el jugador y lo alcanza si no se aparta.
        [UnityTest]
        public IEnumerator Charge_IsTelegraphedAsALine_AndHitsAlongIt()
        {
            yield return FaceGuardian(4f);
            var guardian = Object.FindAnyObjectByType<StoneGuardian>();
            yield return WaitWhileBusy(guardian);
            var player = Player();
            var health = player.GetComponent<Health>();
            Teleport(player, Flow(ArenaCenter.x, ArenaCenter.y - 4f));
            yield return new WaitForFixedUpdate();
            var before = health.Current;

            Assert.IsTrue(guardian.ForceAttack(GuardianAttack.Charge));
            yield return null;
            Assert.IsTrue(guardian.LineMarker.IsShowing, "La embestida se avisa con una franja.");
            Assert.IsTrue(guardian.LineMarker.Contains(player.transform.position), "La franja apunta al jugador.");
            Assert.IsFalse(guardian.LineMarker.Contains(Flow(ArenaCenter.x + 3f, ArenaCenter.y - 2f)), "Fuera de la franja no alcanza.");
            yield return WaitWhileBusy(guardian);
            Assert.Less(health.Current, before, "Quedarse en la franja hace daño.");
        }

        // Las ondas son dos anillos: el primero alcanza a quien está a media distancia; el centro queda a salvo.
        [UnityTest]
        public IEnumerator Waves_AreRings_WithASafeCenter()
        {
            yield return FaceGuardian(2.6f);
            var guardian = Object.FindAnyObjectByType<StoneGuardian>();
            yield return WaitWhileBusy(guardian);
            var player = Player();
            var health = player.GetComponent<Health>();
            Teleport(player, Flow(ArenaCenter.x, ArenaCenter.y - 2.6f));
            yield return new WaitForFixedUpdate();
            var before = health.Current;

            Assert.IsTrue(guardian.ForceAttack(GuardianAttack.Waves));
            yield return null;
            Assert.IsTrue(guardian.RingMarker.IsShowing);
            Assert.IsTrue(guardian.RingMarker.Contains(player.transform.position), "A 2,6 m el primer anillo alcanza.");
            Assert.IsFalse(guardian.RingMarker.Contains(guardian.transform.position + Vector3.right * 0.5f), "El centro del anillo queda a salvo.");
            yield return WaitWhileBusy(guardian);
            Assert.Less(health.Current, before);
        }

        // La Furia marca zonas sobre el jugador en dos tandas.
        [UnityTest]
        public IEnumerator Fury_MarksZonesOnThePlayer_InTwoWaves()
        {
            yield return FaceGuardian(4f);
            var guardian = Object.FindAnyObjectByType<StoneGuardian>();
            yield return WaitWhileBusy(guardian);
            var player = Player();
            var health = player.GetComponent<Health>();
            var before = health.Current;

            Assert.IsTrue(guardian.ForceAttack(GuardianAttack.Fury));
            yield return null;
            Assert.AreEqual(3, guardian.FuryMarkers.Count(m => m.IsShowing), "Cada tanda marca tres zonas.");
            Assert.IsTrue(guardian.FuryMarkers.Any(m => m.IsShowing && m.Contains(player.transform.position)), "Una cae sobre el jugador.");
            yield return WaitWhileBusy(guardian);
            Assert.Less(health.Current, before);
        }

        // Vencido: completa MP_OLM_05, queda liberado y no vuelve al cargar el piso.
        [UnityTest]
        public IEnumerator Guardian_WhenDefeated_IsFreed_AndStaysFreed()
        {
            quests.Start(Resources.Load<QuestCatalog>(QuestCatalog.ResourcePath).Find("MP_OLM_05"));
            story.Report(StoryEvents.Examine("sello_guardian"));
            yield return FaceGuardian(5.5f);
            var guardian = Object.FindAnyObjectByType<StoneGuardian>();
            Assert.IsTrue(guardian.GetComponent<Health>().TryDamage(1000, Player().transform.position));
            yield return null;

            Assert.IsTrue(story.HasFlag(FreedFlag));
            Assert.AreEqual(QuestStatus.Completed, quests.GetStatus("MP_OLM_05"));
            Assert.IsNull(Object.FindAnyObjectByType<StoneGuardian>(), "Vencido, el Guardián se retira.");
            Assert.IsTrue(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Any(r => r.sprite != null && r.sprite.name == "Guardian_Piedra_Liberado_01"), "Queda la figura liberada.");

            yield return SceneManager.UnloadSceneAsync(Floor3);
            yield return Load();
            Assert.IsNull(Object.FindAnyObjectByType<StoneGuardian>(), "El Guardián liberado no reaparece.");
        }
    }
}
