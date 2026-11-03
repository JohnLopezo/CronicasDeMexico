using System;
using System.Collections;
using System.IO;
using CronicasDeMexico.Core.Boot;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.UI.Hud;
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
            Assert.IsFalse(save.HasSave);
            Assert.IsFalse(menu.IsVisible);
            for (var i = 0; i < 300 && !SceneManager.GetSceneByName(CityScene).isLoaded; i++)
                yield return null;
            Assert.IsTrue(SceneManager.GetSceneByName(CityScene).isLoaded);
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
