using System;
using System.Collections;
using System.IO;
using System.Linq;
using CronicasDeMexico.Core.Boot;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Narrative;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.Gameplay.Travel;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Base de la mazmorra olmeca (M2, OlmecaDungeonGenerator desde Mazmorra_P&lt;n&gt;.json): pisos y escaleras, santuario de
    /// Memoria (punto de control y guardado), derrota que devuelve al santuario, santuario de Vida una vez por incursión
    /// y continuar la partida desde Boot. Las pruebas que guardan usan servicios propios con una carpeta temporal, para no
    /// tocar la partida real ni el estado de Boot.
    /// </summary>
    public class OlmecaMazmorraSceneTests
    {
        const string JungleScene = "Olmeca_Selva";
        static string Floor(int n) => $"Olmeca_Mazmorra_P{n}";

        // Servicios de Boot que una prueba sustituye y deja como estaban.
        StoryState bootStory;
        PlayerCheckpoint bootCheckpoint;
        SaveService bootSave;
        string folder;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.TryGet(out bootStory);
            ServiceLocator.TryGet(out bootCheckpoint);
            ServiceLocator.TryGet(out bootSave);
            folder = Path.Combine(Path.GetTempPath(), "CronicasPruebaMazmorra_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (bootStory != null)
                ServiceLocator.Register(bootStory);
            if (bootCheckpoint != null)
                ServiceLocator.Register(bootCheckpoint);
            if (bootSave != null)
                ServiceLocator.Register(bootSave);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        // Aunque una prueba falle a medias, no deja escenas de región cargadas para las siguientes.
        [UnityTearDown]
        public IEnumerator UnloadRegions()
        {
            if (ServiceLocator.TryGet(out SceneTravel travel))
                while (travel.IsTraveling)
                    yield return null;
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("Olmeca_"))
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        (StoryState story, PlayerCheckpoint checkpoint, SaveService save) OwnServices()
        {
            var story = new StoryState();
            var checkpoint = new PlayerCheckpoint();
            var save = new SaveService(folder);
            save.Register(story);
            save.Register(checkpoint);
            ServiceLocator.Register(story);
            ServiceLocator.Register(checkpoint);
            ServiceLocator.Register(save);
            return (story, checkpoint, save);
        }

        static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            yield return null;
            yield return null; // ActiveWhenFlags espera a StoryState en su primer Update
        }

        static IEnumerator UnloadIfLoaded(string scene)
        {
            if (SceneManager.GetSceneByName(scene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
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

        static IEnumerator WaitTravel(SceneTravel travel)
        {
            var guard = 0f;
            while (travel.IsTraveling)
            {
                guard += Time.unscaledDeltaTime;
                Assert.Less(guard, 10f, "El viaje no termina.");
                yield return null;
            }
            yield return null;
        }

        static PlayerMovement Player() => Object.FindAnyObjectByType<PlayerMovement>();

        static SpriteRenderer SpriteNamed(string sprite) =>
            Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(r => r.sprite != null && r.sprite.name == sprite);

        static ScenePortal Portal(string targetScene) =>
            Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Single(p => p.TargetScene == targetScene);

        static SpawnPoint Spawn(string id) =>
            Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s => s.SpawnId == id);

        // Pisa el portal hacia targetScene y espera a llegar.
        static IEnumerator TakePortal(string targetScene, SceneTravel travel)
        {
            var portal = Portal(targetScene);
            Teleport(Player(), new Vector3(portal.transform.position.x, 0f, portal.transform.position.z));
            yield return WaitPhysics();
            Assert.IsTrue(travel.IsTraveling, $"El portal hacia {targetScene} no inició el viaje.");
            yield return WaitTravel(travel);
            Assert.AreEqual(targetScene, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Floors_Load_WithStairsShrinesEnemiesAndTheVoidBlocked()
        {
            var expectedEnemies = new[] { 2, 3, 0 };
            for (var n = 1; n <= 3; n++)
            {
                yield return Load(Floor(n));
                yield return new WaitForSeconds(0.5f); // si no hubiera suelo, la gravedad los habría hecho caer

                var player = Player();
                Assert.IsNotNull(player, $"El piso {n} no tiene jugador.");
                Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.1f), $"Piso {n}: el jugador no está sobre el suelo.");
                Assert.Less(Vector3.Distance(player.transform.position, Spawn("desde_arriba").transform.position), 0.5f,
                    "Al pulsar Play el jugador empieza al pie de la escalera de subida.");
                Assert.IsNotNull(Camera.main?.GetComponent<IsometricCameraFollow>(), "Falta la cámara isométrica.");
                Assert.IsNotNull(Object.FindAnyObjectByType<DefeatReturn>(), $"Piso {n}: falta DefeatReturn.");

                Assert.AreEqual(n == 1 ? JungleScene : Floor(n - 1), Portal(n == 1 ? JungleScene : Floor(n - 1)).TargetScene);
                if (n < 3)
                {
                    Assert.AreEqual("desde_arriba", Portal(Floor(n + 1)).TargetSpawnId);
                    Assert.IsNotNull(Spawn("desde_abajo"));
                }

                var enemies = Object.FindObjectsByType<EnemyChaser>(FindObjectsSortMode.None);
                Assert.AreEqual(expectedEnemies[n - 1], enemies.Length, $"Piso {n}: número de espíritus.");
                foreach (var enemy in enemies)
                    Assert.That(enemy.transform.position.y, Is.EqualTo(0f).Within(0.1f), $"{enemy.name} no está sobre el suelo.");

                Assert.AreEqual(n != 2, Object.FindAnyObjectByType<MemoryShrine>() != null, $"Piso {n}: santuario de Memoria.");
                Assert.AreEqual(n == 2, Object.FindAnyObjectByType<LifeShrine>() != null, $"Piso {n}: santuario de Vida.");

                // El vacío que rodea las salas no se atraviesa: hay una caja a la altura de un muro. (0, 8) del flujo
                // queda detrás de la sala central en los tres pisos.
                Assert.IsTrue(Physics.Raycast(IsometricProjection.FromPipeline(0f, 8f, 10f), Vector3.down, out var hit, 20f));
                Assert.Greater(hit.point.y, 1f, $"Piso {n}: el vacío debe bloquear el paso.");

                yield return UnloadIfLoaded(Floor(n));
            }
        }

        // Selva → piso 1 → 2 → 3 y de vuelta por las escaleras, con la vida que se tenía (Diseno.md §9).
        [UnityTest]
        public IEnumerator Travel_FromTheJungleDownToFloor3AndBack_KeepsThePlayersHealth()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SceneTravel travel), "Boot no registró SceneTravel.");
            var (story, _, _) = OwnServices();
            story.SetFlag(StoryEvents.QuestCompletedFlag("MP_OLM_03")); // el guerrero ya reconoció la entrada

            yield return Load(JungleScene);
            var health = Player().GetComponent<Health>();
            Assert.IsTrue(health.TryDamage(2, health.transform.position + Vector3.right));
            var expected = health.Current;

            yield return TakePortal(Floor(1), travel);
            Assert.IsFalse(SceneManager.GetSceneByName(JungleScene).isLoaded, "La selva debe descargarse.");
            yield return TakePortal(Floor(2), travel);
            yield return TakePortal(Floor(3), travel);
            Assert.Less(Vector3.Distance(Player().transform.position, Spawn("desde_arriba").transform.position), 0.5f);
            Assert.AreEqual(expected, Player().GetComponent<Health>().Current, "La vida no debe recuperarse al cambiar de piso.");

            yield return TakePortal(Floor(2), travel);
            Assert.Less(Vector3.Distance(Player().transform.position, Spawn("desde_abajo").transform.position), 0.5f);
            yield return TakePortal(Floor(1), travel);
            yield return TakePortal(JungleScene, travel);
            Assert.Less(Vector3.Distance(Player().transform.position, Spawn("desde_mazmorra").transform.position), 0.5f);
            Assert.AreEqual(expected, Player().GetComponent<Health>().Current);

            yield return UnloadIfLoaded(JungleScene);
        }

        // Descansar fija el punto de control y guarda; al caer, el jugador vuelve al santuario con la vida completa, los
        // espíritus normales del piso vuelven y la historia se conserva.
        [UnityTest]
        public IEnumerator MemoryShrine_SavesTheCheckpoint_AndDefeatReturnsThereWithFullHealth()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SceneTravel travel));
            var (story, checkpoint, save) = OwnServices();
            yield return Load(Floor(1));

            var player = Player();
            var health = player.GetComponent<Health>();
            Assert.IsTrue(health.TryDamage(1, player.transform.position + Vector3.right));
            var shrine = Object.FindAnyObjectByType<MemoryShrine>();
            var awake = SpriteNamed("Santuario_Memoria_02");
            Assert.IsFalse(awake.gameObject.activeInHierarchy, "El jade empieza apagado.");

            shrine.Interact(player.transform);
            yield return null;
            Assert.IsTrue(checkpoint.HasCheckpoint);
            Assert.AreEqual(Floor(1), checkpoint.Scene);
            Assert.AreEqual("memoria_p1", checkpoint.SpawnId);
            Assert.AreEqual(health.Current, checkpoint.Health);
            Assert.IsTrue(File.Exists(save.MainPath), "El santuario no guardó la partida.");
            Assert.IsTrue(story.HasFlag(DungeonFlags.MemoryShrineAwake("memoria_p1")));
            Assert.IsTrue(awake.gameObject.activeInHierarchy, "Tras descansar el jade brilla.");
            Assert.IsFalse(SpriteNamed("Santuario_Memoria_01").gameObject.activeInHierarchy);

            story.SetFlag("prueba.progreso");
            var enemy = Object.FindObjectsByType<EnemyChaser>(FindObjectsSortMode.None).First();
            Assert.IsTrue(enemy.GetComponent<Health>().TryDamage(100, player.transform.position));
            yield return new WaitForSeconds(1.1f); // fin de la invulnerabilidad del jugador (1 s) tras el primer golpe
            Assert.IsTrue(enemy.GetComponent<Health>().IsDead, "El espíritu no reaparece mientras se está en el piso.");

            Assert.IsTrue(health.TryDamage(100, player.transform.position + Vector3.right));
            Assert.IsTrue(health.IsDead);
            yield return new WaitForSeconds(1.7f); // DefeatReturn espera 1,5 s antes de viajar
            Assert.IsTrue(travel.IsTraveling, "La derrota no inició el regreso al santuario.");
            yield return WaitTravel(travel);

            player = Player();
            Assert.AreEqual(Floor(1), player.gameObject.scene.name);
            Assert.Less(Vector3.Distance(player.transform.position, Spawn("memoria_p1").transform.position), 0.5f,
                "Tras la derrota se aparece delante del santuario.");
            health = player.GetComponent<Health>();
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(health.Max, health.Current, "Se vuelve con la vida completa (penalización 0).");
            Assert.AreEqual(2, Object.FindObjectsByType<EnemyChaser>(FindObjectsSortMode.None)
                .Count(e => !e.GetComponent<Health>().IsDead), "Los espíritus normales vuelven al recargar el piso.");
            Assert.IsTrue(story.HasFlag("prueba.progreso"), "La derrota no debe borrar el progreso de la historia.");

            yield return UnloadIfLoaded(Floor(1));
        }

        // Sin punto de control se reaparece en el sitio, como fuera de la mazmorra.
        [UnityTest]
        public IEnumerator Defeat_WithoutCheckpoint_RespawnsInPlace()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SceneTravel travel));
            OwnServices();
            yield return Load(Floor(2));
            var player = Player();
            var health = player.GetComponent<Health>();
            Assert.IsTrue(health.TryDamage(100, player.transform.position + Vector3.right));
            yield return new WaitForSeconds(1.8f);
            Assert.IsFalse(travel.IsTraveling);
            Assert.IsFalse(health.IsDead, "Sin santuario de Memoria debe reaparecer en el sitio.");
            Assert.AreEqual(Floor(2), player.gameObject.scene.name);
            yield return UnloadIfLoaded(Floor(2));
        }

        // Cura la mitad una vez por incursión; con la vida llena no se gasta; al volver a la selva se reenciende.
        [UnityTest]
        public IEnumerator LifeShrine_HealsHalfOncePerIncursion_AndRelightsInTheJungle()
        {
            var (story, _, _) = OwnServices();
            yield return Load(Floor(2));
            var player = Player();
            var health = player.GetComponent<Health>();
            var shrine = Object.FindAnyObjectByType<LifeShrine>();
            var usedFlag = DungeonFlags.LifeShrineUsed("vida_p2");

            shrine.Interact(player.transform);
            Assert.IsFalse(story.HasFlag(usedFlag), "Con la vida llena el fuego no se gasta.");

            Assert.IsTrue(health.TryDamage(health.Max - 1, player.transform.position + Vector3.right));
            Assert.AreEqual(1, health.Current);
            shrine.Interact(player.transform);
            Assert.AreEqual(1 + Mathf.CeilToInt(health.Max * 0.5f), health.Current, "Debe curar la mitad, redondeada hacia arriba.");
            Assert.IsTrue(story.HasFlag(usedFlag));
            yield return null;
            Assert.IsTrue(SpriteNamed("Santuario_Vida_02").gameObject.activeInHierarchy, "Tras usarlo el fuego se ve apagado.");

            var before = health.Current;
            yield return new WaitForSeconds(1.1f); // fin de la invulnerabilidad del jugador (1 s) tras el golpe
            Assert.IsTrue(health.TryDamage(1, player.transform.position + Vector3.right));
            shrine.Interact(player.transform);
            Assert.AreEqual(before - 1, health.Current, "Solo cura una vez por incursión.");

            yield return UnloadIfLoaded(Floor(2));
            yield return Load(JungleScene); // salir de la mazmorra termina la incursión
            Assert.IsFalse(story.HasFlag(usedFlag), "En la selva el santuario de Vida debe reencenderse.");
            yield return UnloadIfLoaded(JungleScene);
        }

        // Boot continúa la partida en su santuario de Memoria, con la vida que se tenía al descansar.
        [UnityTest]
        public IEnumerator ContinueSavedGame_TravelsToTheCheckpoint_WithTheSavedHealth()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out SceneTravel travel));
            var saved = new SaveService(folder);
            var checkpoint = new PlayerCheckpoint();
            saved.Register(checkpoint);
            Assert.IsFalse(GameBootstrapper.ContinueSavedGame(saved, checkpoint, travel), "Sin partida no hay nada que continuar.");
            checkpoint.Set(Floor(3), "memoria_p3", 2);
            Assert.IsTrue(saved.Save());

            var loaded = new PlayerCheckpoint();
            var load = new SaveService(folder);
            load.Register(loaded);
            Assert.IsTrue(GameBootstrapper.ContinueSavedGame(load, loaded, travel));
            yield return WaitTravel(travel);

            Assert.AreEqual(Floor(3), SceneManager.GetActiveScene().name);
            var player = Player();
            Assert.Less(Vector3.Distance(player.transform.position, Spawn("memoria_p3").transform.position), 0.5f);
            Assert.AreEqual(2, player.GetComponent<Health>().Current);

            yield return UnloadIfLoaded(Floor(3));
        }
    }
}
