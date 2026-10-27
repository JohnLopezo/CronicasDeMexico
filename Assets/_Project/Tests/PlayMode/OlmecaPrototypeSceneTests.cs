using System.Collections;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CronicasDeMexico.Tests
{
    public class OlmecaPrototypeSceneTests
    {
        const string SceneName = "Olmeca_Prototipo";

        [UnityTest]
        public IEnumerator Scene_Loads_WithPlayerOnGround_AndMainCamera()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);

            // Tiempo suficiente para que la gravedad lo hubiera hecho caer si no hubiera suelo.
            yield return new WaitForSeconds(1f);

            var player = Object.FindAnyObjectByType<PlayerMovement>();
            Assert.IsNotNull(player, "La escena no tiene jugador.");
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.1f), "El jugador no está sobre el suelo.");
            Assert.IsNotNull(Camera.main, "La escena no tiene una cámara con la etiqueta MainCamera.");
            Assert.IsNotNull(Camera.main.GetComponent<CameraQualityApplier>(), "La cámara no tiene CameraQualityApplier.");
            Assert.IsNotNull(player.GetComponent<DirectionalSpriteAnimator>(), "El jugador no tiene DirectionalSpriteAnimator.");

            yield return SceneManager.UnloadSceneAsync(SceneName);
        }

        [UnityTest]
        public IEnumerator Hud_ShowsPrompt_WhenPlayerStandsNextToColossalHead()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            yield return null;

            var inscription = Object.FindAnyObjectByType<InscriptionInteractable>();
            Assert.IsNotNull(inscription, "La cabeza colosal no tiene inscripción.");
            var document = Object.FindAnyObjectByType<UIDocument>();
            Assert.IsNotNull(document, "La escena no tiene HUD.");
            var prompt = document.rootVisualElement.Q<Label>("prompt");
            Assert.IsNotNull(prompt, "El HUD no tiene la etiqueta 'prompt'.");
            Assert.IsTrue(prompt.ClassListContains("hidden"), "Lejos de la cabeza no debe verse el aviso.");

            // El CharacterController se desactiva para poder teletransportar al jugador.
            var player = Object.FindAnyObjectByType<PlayerInteractor>();
            Assert.IsNotNull(player, "El jugador no tiene PlayerInteractor.");
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = inscription.transform.position + new Vector3(0f, -1f, -2.5f);
            controller.enabled = true;

            yield return new WaitForSeconds(0.3f);

            Assert.That(player.Current, Is.SameAs(inscription), "El jugador no detectó la inscripción.");
            Assert.IsFalse(prompt.ClassListContains("hidden"), "El HUD no muestra el aviso junto a la cabeza.");
            StringAssert.Contains("Examinar", prompt.text);

            yield return SceneManager.UnloadSceneAsync(SceneName);
        }
    }
}
