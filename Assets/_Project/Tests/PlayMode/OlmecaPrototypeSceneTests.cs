using System.Collections;
using CronicasDeMexico.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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

            yield return SceneManager.UnloadSceneAsync(SceneName);
        }
    }
}
