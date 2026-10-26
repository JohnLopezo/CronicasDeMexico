using System.Collections;
using System.Reflection;
using CronicasDeMexico.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    public class PlayerMovementTests
    {
        const float WallFrontZ = 2.5f;

        InputSettings previousSettings;
        InputSettings testSettings;
        Keyboard keyboard;
        InputActionAsset actions;
        InputAction moveAction;
        GameObject ground;
        GameObject wall;
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            // En modo batch la aplicación no tiene foco y el Input System descartaría el teclado.
            previousSettings = InputSystem.settings;
            testSettings = ScriptableObject.CreateInstance<InputSettings>();
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;

            keyboard = InputSystem.AddDevice<Keyboard>();

            // InputActionReference exige que la acción pertenezca a un InputActionAsset.
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            moveAction = actions.AddActionMap("Player").AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1.5f, WallFrontZ + 0.5f);
            wall.transform.localScale = new Vector3(6f, 3f, 1f);

            // Se crea inactivo para asignar la acción antes de que corra OnEnable.
            player = new GameObject("JugadorPrueba");
            player.SetActive(false);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            controller.minMoveDistance = 0f; // como en el prefab: a muchos FPS el paso por frame es diminuto
            var movement = player.AddComponent<PlayerMovement>();
            typeof(PlayerMovement)
                .GetField("moveAction", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(movement, InputActionReference.Create(moveAction));
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(wall);
            Object.Destroy(ground);
            Object.Destroy(actions);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = previousSettings != null ? previousSettings : ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(testSettings);
        }

        [UnityTest]
        public IEnumerator Player_MovesForward_AndStopsAtWall()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));

            // A 4 m/s recorrería unos 8 m en 2 s; el muro está a 2.5 m.
            yield return new WaitForSeconds(2f);

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());

            var position = player.transform.position;
            Assert.That(position.z, Is.GreaterThan(1.5f), $"El jugador no avanzó: {position}");
            Assert.That(position.z, Is.LessThan(WallFrontZ), "El jugador atravesó el muro.");
            Assert.That(position.y, Is.EqualTo(0f).Within(0.1f), "El jugador no quedó sobre el suelo.");
        }

        [UnityTest]
        public IEnumerator Player_DiagonalSpeed_EqualsStraightSpeed()
        {
            // Se aleja del muro: diagonal hacia atrás-derecha (S + D).
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.D));
            yield return new WaitForSeconds(1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());

            var planar = Vector3.ProjectOnPlane(player.GetComponent<PlayerMovement>().PlanarVelocity, Vector3.up);
            Assert.That(planar.magnitude, Is.EqualTo(4f).Within(0.05f));
            Assert.That(planar.x, Is.GreaterThan(0f));
            Assert.That(planar.z, Is.LessThan(0f));
        }

        [UnityTest]
        public IEnumerator SpriteFacing_FlipsWithScale_WhenMovingLeft_AndKeepsSide_WhenMovingUp()
        {
            var sprite = new GameObject("SpritePrueba").AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(player.transform, false);

            // Se añade con el objeto inactivo para asignar las referencias antes de Awake.
            player.SetActive(false);
            var facing = player.AddComponent<CharacterSpriteFacing>();
            const BindingFlags privateField = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(CharacterSpriteFacing).GetField("movement", privateField).SetValue(facing, player.GetComponent<PlayerMovement>());
            typeof(CharacterSpriteFacing).GetField("spriteRenderer", privateField).SetValue(facing, sprite);
            player.SetActive(true);

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            yield return new WaitForSeconds(0.2f);
            Assert.That(sprite.transform.localScale.x, Is.EqualTo(-1f), "No se volteó al ir a la izquierda.");
            Assert.IsFalse(sprite.flipX, "Debe voltear con la escala, no con flipX.");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
            yield return new WaitForSeconds(0.2f);
            Assert.That(sprite.transform.localScale.x, Is.EqualTo(-1f), "Al moverse en vertical debe conservar el lado.");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Assert.That(sprite.transform.localScale.x, Is.EqualTo(1f), "No volvió a mirar a la derecha.");
        }
    }
}
