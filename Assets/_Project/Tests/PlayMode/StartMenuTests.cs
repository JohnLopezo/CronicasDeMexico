using System;
using System.Collections;
using System.IO;
using CronicasDeMexico.Core.Boot;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.UI.Hud;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Menú de inicio (StartMenuController, en Boot). En las pruebas Boot se carga encima de otra escena, así que el
    /// menú no se pide solo: se muestra con un guardado en una carpeta temporal, para no tocar la partida real.
    /// </summary>
    public class StartMenuTests
    {
        const string CityScene = "Olmeca_Ciudad";
        string folder;
        StartMenuController menu;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "CronicasPruebaInicio_" + Guid.NewGuid().ToString("N"));
            menu = Object.FindAnyObjectByType<StartMenuController>();
            Assert.IsNotNull(menu, "Boot debe tener el menú de inicio (Crónicas → Crear menú de inicio en Boot).");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            menu.Hide();
            if (ServiceLocator.TryGet(out SceneTravel travel))
                while (travel.IsTraveling)
                    yield return null;
            if (SceneManager.GetSceneByName(CityScene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(CityScene);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        (SaveService save, PlayerCheckpoint checkpoint) SavedGame(bool withSave)
        {
            var save = new SaveService(folder);
            var checkpoint = new PlayerCheckpoint();
            save.Register(checkpoint);
            if (withSave)
            {
                checkpoint.Set(CityScene, OlmecaCitySpawn, 3);
                Assert.IsTrue(save.Save());
            }
            return (save, checkpoint);
        }

        const string OlmecaCitySpawn = "desde_selva"; // OlmecaCityGenerator.FromJungleSpawnId

        Button Get(string name) => menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(name);
        VisualElement Panel(string name) => menu.GetComponent<UIDocument>().rootVisualElement.Q(name);

        [UnityTest]
        public IEnumerator StartMenu_IsNotRequested_WhenBootLoadsOverAnotherScene()
        {
            yield return null;
            Assert.IsFalse(GameBootstrapper.StartMenuRequested);
            Assert.IsFalse(menu.IsVisible);
        }

        // Sin PanelSettings el menú existe pero no se dibuja (pasó en el primer build: StartMenuSetup cargaba los assets
        // antes de abrir Boot).
        [UnityTest]
        public IEnumerator StartMenu_HasPanelSettings_AndItsStyleSheet()
        {
            yield return null;
            var document = menu.GetComponent<UIDocument>();
            Assert.IsNotNull(document.panelSettings, "El menú de inicio no tiene PanelSettings: no se dibujaría.");
            Assert.Greater(document.rootVisualElement.styleSheets.count, 0, "Falta la hoja de estilos StartMenu.uss.");
        }

        [UnityTest]
        public IEnumerator WithoutSave_ContinueIsDisabled_AndOptionsListQualityLevels()
        {
            var (save, checkpoint) = SavedGame(false);
            menu.Show(save, checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            yield return null;

            Assert.IsTrue(menu.IsVisible);
            Assert.IsFalse(Get("start-continue").enabledSelf, "Sin partida no se puede continuar.");
            Assert.IsTrue(Get("start-new").enabledSelf);
            for (var i = 0; i < QualitySettings.names.Length; i++)
                Assert.IsNotNull(Get($"start-quality-{i}"), $"Falta el nivel {QualitySettings.names[i]}.");
        }

        [UnityTest]
        public IEnumerator NewGame_WithSave_AsksConfirmation_ThenDeletesIt_AndLoadsTheCity()
        {
            var (save, checkpoint) = SavedGame(true);
            menu.Show(save, checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            Assert.IsTrue(Get("start-continue").enabledSelf);

            menu.NewGame();
            Assert.IsFalse(Panel("start-confirm").ClassListContains("hidden"), "Con partida guardada debe pedir confirmación.");
            Assert.IsTrue(save.HasSave, "Sin confirmar no se borra nada.");

            menu.ConfirmNewGame();
            Assert.IsFalse(Panel("start-guardians").ClassListContains("hidden"), "Después toca elegir Guardián.");
            Assert.IsTrue(save.HasSave, "Volver atrás desde la elección no debe haber borrado nada.");

            menu.StartWithGuardian("jaguar");
            Assert.IsFalse(save.HasSave);
            Assert.IsFalse(menu.IsVisible);
            for (var i = 0; i < 300 && !SceneManager.GetSceneByName(CityScene).isLoaded; i++)
                yield return null;
            Assert.IsTrue(SceneManager.GetSceneByName(CityScene).isLoaded);
        }

        [UnityTest]
        public IEnumerator GuardianPanel_ListsTheSeven_AndHighlightingDescribesThem()
        {
            var (save, checkpoint) = SavedGame(false);
            menu.Show(save, checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            menu.NewGame();
            yield return null;

            var catalog = Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath);
            Assert.AreEqual(7, catalog.Guardians.Length);
            foreach (var guardian in catalog.Guardians)
                Assert.IsNotNull(Get($"start-guardian-{guardian.Id}"), guardian.Id);

            menu.HighlightGuardian(catalog.Guardians[3]);
            StringAssert.Contains("Ameyali", Get("start-guardian-start").text);
            StringAssert.Contains(catalog.Guardians[3].Description, menu.GetComponent<UIDocument>().rootVisualElement.Q<Label>("start-guardian-description").text);
            Assert.IsTrue(Get("start-guardian-mariposa").ClassListContains("start-menu__button--selected"));
            Assert.IsFalse(save.HasSave);
            Assert.IsFalse(SceneManager.GetSceneByName(CityScene).isLoaded, "Marcar una tarjeta no empieza la partida.");
        }

        [UnityTest]
        public IEnumerator WithoutSave_StartsOnTheGuardianPanel_AndWithSaveOnTheMenu()
        {
            var (save, checkpoint) = SavedGame(false);
            menu.Show(save, checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            yield return null;
            Assert.IsFalse(Panel("start-guardians").ClassListContains("hidden"), "Sin partida se empieza eligiendo Guardián.");
            Assert.IsTrue(Panel("start-main").ClassListContains("hidden"));
            Assert.IsFalse(save.HasSave);

            var saved = SavedGame(true);
            menu.Show(saved.save, saved.checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            yield return null;
            Assert.IsFalse(Panel("start-main").ClassListContains("hidden"), "Con partida guardada, primero el menú, para poder continuar.");
        }

        [UnityTest]
        public IEnumerator PlayFromAGameScene_AsksForTheGuardian_AndStartsWithFullHealth()
        {
            var progression = ServiceLocator.Get<Progression>();
            yield return SceneManager.LoadSceneAsync(CityScene, LoadSceneMode.Additive);
            yield return null;
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerStats>();
            var scenes = SceneManager.sceneCount;
            try
            {
                menu.ShowGuardianPicker();
                Assert.IsTrue(menu.IsVisible);
                Assert.AreEqual(0f, Time.timeScale, "Nadie ataca mientras se elige.");
                Assert.IsTrue(Get("start-guardian-back").ClassListContains("hidden"), "Aquí no hay menú al que volver.");

                menu.StartWithGuardian("ajolote");
                Assert.IsFalse(menu.IsVisible);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.AreEqual(scenes, SceneManager.sceneCount, "La escena ya estaba cargada.");
                yield return null;

                var health = player.GetComponent<Health>();
                Assert.AreEqual(progression.Stats.maxHealth, health.Max);
                Assert.AreEqual(health.Max, health.Current, "Al empezar, con la vida llena del guerrero elegido.");
                StringAssert.StartsWith("Guerrero_Xali_", player.GetComponentInChildren<SpriteBillboard>().GetComponent<SpriteRenderer>().sprite.name);
            }
            finally
            {
                progression.SelectGuardian("jaguar");
            }
        }

        [UnityTest]
        public IEnumerator GuardianTab_ChangesTheWarrior_WithoutHealingOrRestoringEnergy()
        {
            var progression = ServiceLocator.Get<Progression>();
            var (save, checkpoint) = SavedGame(false);
            menu.Show(save, checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            var gameMenu = default(GameMenuController);
            try
            {
                menu.StartWithGuardian("ajolote");
                for (var i = 0; i < 300 && !SceneManager.GetSceneByName(CityScene).isLoaded; i++)
                    yield return null;
                yield return null;

                var player = Object.FindAnyObjectByType<PlayerStats>();
                var health = player.GetComponent<Health>();
                // Según el nivel: los servicios de Boot se comparten con las demás pruebas, que pueden haberlo subido.
                Assert.AreEqual(progression.Stats.maxHealth, health.Max, "Vida de Xali.");
                Assert.IsTrue(health.TryDamage(30, player.transform.position + Vector3.right));
                var hurt = health.Current;
                Assert.IsTrue(progression.SpendEnergy(30));
                var energy = progression.Energy;

                gameMenu = Object.FindAnyObjectByType<GameMenuController>();
                gameMenu.Open(MenuTabs.Guardian);
                var row = gameMenu.GetComponent<UIDocument>().rootVisualElement.Q("guardian-aguila");
                Assert.IsNotNull(row, "La pestaña lista a Itzel.");
                Assert.IsTrue(row.Q<Button>().enabledSelf);
                Assert.IsFalse(gameMenu.GetComponent<UIDocument>().rootVisualElement.Q("guardian-ajolote").Q<Button>().enabledSelf, "El que está en uso no se elige.");
                using (var click = NavigationSubmitEvent.GetPooled())
                {
                    click.target = row.Q<Button>();
                    row.Q<Button>().SendEvent(click);
                }
                yield return null;

                Assert.AreEqual("aguila", progression.Profile.GuardianId);
                Assert.AreEqual(progression.Stats.maxHealth, health.Max, "Vida de Itzel.");
                Assert.AreEqual(Mathf.Min(hurt, health.Max), health.Current, "Se conserva la vida; solo se recorta al nuevo máximo.");
                Assert.AreEqual(Mathf.Min(energy, progression.Stats.maxEnergy), progression.Energy, "Cambiar no recupera energía.");
                Assert.AreEqual("DSS_Guerrero_Itzel_Caminar", player.GetComponent<DirectionalSpriteAnimator>().SpriteSet.name);
                StringAssert.StartsWith("Guerrero_Itzel_", player.GetComponentInChildren<SpriteBillboard>().GetComponent<SpriteRenderer>().sprite.name);

                var afterItzel = health.Current;
                progression.SelectGuardian("ajolote", fillEnergy: false);
                Assert.AreEqual(afterItzel, health.Current, "Volver a una vida máxima mayor tampoco cura.");
            }
            finally
            {
                if (gameMenu != null)
                    gameMenu.Close();
                progression.SelectGuardian("jaguar");
            }
        }

        [UnityTest]
        public IEnumerator StartWithGuardian_PutsItsSpritesOnThePlayer()
        {
            var progression = ServiceLocator.Get<Progression>();
            var (save, checkpoint) = SavedGame(false);
            menu.Show(save, checkpoint, ServiceLocator.Get<SceneTravel>(), CityScene);
            try
            {
                menu.StartWithGuardian("xolo");
                Assert.AreEqual("xolo", progression.Profile.GuardianId);
                for (var i = 0; i < 300 && !SceneManager.GetSceneByName(CityScene).isLoaded; i++)
                    yield return null;
                yield return null;

                var player = Object.FindAnyObjectByType<PlayerStats>();
                var animator = player.GetComponent<DirectionalSpriteAnimator>();
                Assert.AreEqual("DSS_Guerrero_Ocel_Caminar", animator.SpriteSet.name);
                StringAssert.StartsWith("Guerrero_Ocel_", player.GetComponentInChildren<SpriteBillboard>().GetComponent<SpriteRenderer>().sprite.name);

                player.GetComponent<PlayerAttack>().Strike();
                Assert.AreEqual("DSS_Guerrero_Ocel_Ataque", animator.CurrentAction.name);
            }
            finally
            {
                progression.SelectGuardian("jaguar"); // los servicios de Boot se comparten con las demás pruebas
            }
        }

        [UnityTest]
        public IEnumerator Continue_WithSave_TravelsToTheCheckpointScene()
        {
            SavedGame(true);
            var (save, checkpoint) = SavedGame(false); // otra instancia que lee la partida, como al arrancar
            var travel = ServiceLocator.Get<SceneTravel>();
            menu.Show(save, checkpoint, travel, CityScene);

            menu.Continue();
            Assert.IsFalse(menu.IsVisible);
            while (travel.IsTraveling)
                yield return null;
            Assert.AreEqual(CityScene, SceneManager.GetActiveScene().name);
            Assert.IsTrue(save.HasSave, "Continuar no borra la partida.");
        }
    }
}
