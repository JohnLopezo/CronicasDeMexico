using System.Collections;
using System.Linq;
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
    /// <summary>
    /// Escena jugable de la ciudad olmeca (OlmecaCityGenerator, desde Ciudad.json). Las posiciones del flujo de arte
    /// (x, y, z) se convierten a Unity con IsometricProjection.FromPipeline.
    /// </summary>
    public class OlmecaCiudadSceneTests
    {
        const string SceneName = "Olmeca_Ciudad";

        static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            yield return null;
        }

        static IEnumerator UnloadScene()
        {
            yield return SceneManager.UnloadSceneAsync(SceneName);
        }

        static float GroundHeightAt(float x, float y)
        {
            var origin = IsometricProjection.FromPipeline(x, y, 20f);
            Assert.IsTrue(Physics.Raycast(origin, Vector3.down, out var hit, 40f), $"No hay colisión bajo ({x}, {y}).");
            return hit.point.y;
        }

        [UnityTest]
        public IEnumerator Scene_Loads_WithPlayerOnGround_AndIsometricCamera()
        {
            yield return LoadScene();
            yield return new WaitForSeconds(1f); // si no hubiera suelo, la gravedad lo habría hecho caer

            var player = Object.FindAnyObjectByType<PlayerMovement>();
            Assert.IsNotNull(player, "La escena no tiene jugador.");
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.1f), "El jugador no está sobre el suelo.");

            var camera = Camera.main;
            Assert.IsNotNull(camera, "La escena no tiene una cámara con la etiqueta MainCamera.");
            Assert.IsTrue(camera.orthographic, "La cámara debe ser ortográfica.");
            Assert.That(Quaternion.Angle(camera.transform.rotation, IsometricProjection.CameraRotation), Is.LessThan(0.01f));
            Assert.IsNotNull(camera.GetComponent<IsometricCameraFollow>());

            var billboard = player.GetComponentInChildren<SpriteBillboard>();
            Assert.AreEqual(SpriteBillboard.BillboardMode.CameraPlane, billboard.Mode,
                "El jugador debe mirar al plano de la cámara para ordenarse por profundidad con los objetos.");
            Assert.IsNotNull(Object.FindAnyObjectByType<UIDocument>(), "La escena no tiene HUD.");

            yield return UnloadScene();
        }

        // El escenario se dibuja a la resolución del arte y un panel lo amplía por un entero: el Render Scale de URP
        // (0,75 en Bajo) no debe tocar el pixel art.
        [UnityTest]
        public IEnumerator Camera_RendersAtArtResolution_AndPanelScalesItByAnInteger()
        {
            yield return LoadScene();
            yield return null;

            var camera = Camera.main;
            var screen = camera.GetComponent<PixelArtScreen>();
            Assert.IsNotNull(screen, "La cámara no tiene PixelArtScreen.");
            Assert.IsNotNull(screen.Target);
            Assert.AreSame(screen.Target, camera.targetTexture);
            Assert.AreEqual(FilterMode.Point, screen.Target.filterMode);
            Assert.AreEqual(IsometricProjection.IntegerScale(Screen.height), screen.Scale);
            Assert.GreaterOrEqual(screen.Target.height * screen.Scale, Screen.height);
            Assert.GreaterOrEqual(screen.Target.width * screen.Scale, Screen.width);
            Assert.AreEqual(screen.Target.height / (2f * IsometricProjection.PixelsPerUnit), camera.orthographicSize, 1e-4f);
            Assert.IsNotNull(screen.Presentation.rootVisualElement.Q("pixel-art-screen"), "El panel no muestra la imagen.");

            yield return UnloadScene();
        }

        [UnityTest]
        public IEnumerator Colliders_FollowCityLayout()
        {
            yield return LoadScene();

            Assert.That(GroundHeightAt(3.5f, 1.5f), Is.EqualTo(0f).Within(0.01f), "Plaza (lejos del jugador, que también es un collider)");
            Assert.That(GroundHeightAt(-5f, 11f), Is.EqualTo(1f).Within(0.01f), "Nivel 1 del Gran Montículo");
            Assert.That(GroundHeightAt(-3f, 14f), Is.EqualTo(2f).Within(0.01f), "Nivel 2");
            Assert.That(GroundHeightAt(0f, 16f), Is.EqualTo(3f).Within(0.01f), "Nivel 3 (cima)");
            Assert.That(GroundHeightAt(0f, 8.5f), Is.EqualTo(0.5f).Within(0.05f), "Mitad de la primera rampa");
            Assert.That(GroundHeightAt(0f, -11f), Is.EqualTo(1f).Within(0.01f), "Plataforma del recinto");

            var waterCell = Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
                .First(box => box.transform.parent != null && box.transform.parent.name == "Agua");
            Assert.Greater(waterCell.bounds.max.y, 1f, "El agua debe bloquear el paso.");

            // El muelle se camina: sobre su tablero no hay agua que bloquee.
            var pier = Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
                .FirstOrDefault(box => box.transform.parent != null && box.transform.parent.name == "Muelles");
            Assert.IsNotNull(pier, "Falta el muelle.");
            var pierTop = pier.bounds.center + Vector3.up * 10f;
            Assert.IsTrue(Physics.Raycast(pierTop, Vector3.down, out var pierHit, 20f));
            Assert.That(pierHit.point.y, Is.EqualTo(0.14f).Within(0.02f), "El muelle debe poder caminarse.");

            yield return UnloadScene();
        }

        // Sube la primera rampa moviendo el CharacterController como lo haría PlayerMovement.
        [UnityTest]
        public IEnumerator Player_CanClimbFirstRamp()
        {
            yield return LoadScene();

            var player = Object.FindAnyObjectByType<PlayerMovement>();
            player.enabled = false; // sin entrada: el test mueve el controlador
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = IsometricProjection.FromPipeline(0f, 6.6f, 0.05f);
            controller.enabled = true;

            // La rampa sube hacia +Y del flujo (y = 7.5 → 9.5). Se para en y ≈ 10.2, antes de la segunda rampa (10.5).
            var uphill = IsometricProjection.FromPipeline(0f, 1f, 0f).normalized;
            for (var t = 0f; t < 1.2f; t += Time.fixedDeltaTime)
            {
                controller.Move((uphill * 3f + Vector3.down * 2f) * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.transform.position.y, Is.EqualTo(1f).Within(0.15f), "No subió al primer nivel por la rampa.");

            yield return UnloadScene();
        }

        [UnityTest]
        public IEnumerator Scenery_IsOnPixelGrid_AndAltarsCanBeExamined()
        {
            yield return LoadScene();

            const float pixel = 1f / IsometricProjection.PixelsPerUnit;
            var objects = GameObject.Find("Objetos");
            Assert.IsNotNull(objects, "Falta el grupo Objetos.");
            var renderers = objects.GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(renderers.Length, 500, "La ciudad tiene muy pocos objetos.");
            foreach (var renderer in renderers)
            {
                var position = renderer.transform.position;
                Assert.That(Vector3.Distance(position, IsometricProjection.SnapToPixelGrid(position, pixel)), Is.LessThan(1e-3f),
                    $"{renderer.name} no está alineado a la rejilla de píxeles.");
                Assert.IsNotNull(renderer.sprite, renderer.name);
            }

            var inscriptions = Object.FindObjectsByType<InscriptionInteractable>(FindObjectsSortMode.None);
            Assert.That(inscriptions.Count(i => i.name.Contains("Altar_Trono")), Is.EqualTo(2), "Los dos altares deben poder examinarse.");
            Assert.That(inscriptions.Count(i => i.name.Contains("Estela")), Is.GreaterThanOrEqualTo(8));

            // Cada trozo del suelo horneado debe dibujarse con su propia textura (con un material compartido salían
            // todos con la del primero).
            var ground = GameObject.Find("--- Ciudad ---").transform.Find("Suelo").GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(ground.Length, 1);
            foreach (var renderer in ground)
                Assert.AreSame(renderer.sprite.texture, renderer.sharedMaterial.mainTexture, renderer.name);
            Assert.AreEqual(ground.Length, ground.Select(r => r.sharedMaterial).Distinct().Count());

            var cycler = Object.FindAnyObjectByType<SpriteFrameCycler>();
            Assert.IsNotNull(cycler, "El agua no está animada.");
            Assert.AreEqual(4, cycler.FrameCount);

            yield return UnloadScene();
        }
    }
}
