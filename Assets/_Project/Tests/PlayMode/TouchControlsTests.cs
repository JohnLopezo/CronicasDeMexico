using System.Collections;
using System.Linq;
using CronicasDeMexico.UI.Touch;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CronicasDeMexico.Tests
{
    /// <summary>Controles táctiles (TouchControls): cuándo se ven y que cada control mueve el mando virtual que lee el juego.</summary>
    public class TouchControlsTests
    {
        const string PrefabPath = "Assets/_Project/Prefabs/UI/Controles_Tactiles.prefab";
        GameObject instance;
        TouchControls touch;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            touch = instance.GetComponent<TouchControls>();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator Hidden_WithoutTouchscreen_OrWhilePaused()
        {
            Assume.That(Touchscreen.current, Is.Null, "Solo vale en un equipo sin pantalla táctil.");
            Assert.IsFalse(touch.IsVisible, "Sin pantalla táctil no estorban en el PC.");
            touch.ForceVisible = true;
            yield return null;
            Assert.IsTrue(touch.IsVisible);
            Time.timeScale = 0f; // menú abierto
            yield return null;
            Assert.IsFalse(touch.IsVisible, "Con el menú abierto se ocultan: sus botones ya responden al toque.");
        }

        [UnityTest]
        public IEnumerator AttackButton_PressesTheGamepadButtonTheGameReads()
        {
            touch.ForceVisible = true;
            yield return null;
            var button = instance.GetComponentsInChildren<OnScreenButton>(true).Single(b => b.name == "Atacar");
            var attack = new InputAction(binding: "<Gamepad>/buttonWest");
            attack.Enable();
            try
            {
                button.OnPointerDown(new PointerEventData(EventSystem.current));
                yield return null;
                Assert.IsTrue(attack.IsPressed(), "Pulsar Atacar en pantalla es pulsar el botón Oeste del mando.");
                button.OnPointerUp(new PointerEventData(EventSystem.current));
                yield return null;
                Assert.IsFalse(attack.IsPressed());
            }
            finally
            {
                attack.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Joystick_MovesTheLeftStick()
        {
            touch.ForceVisible = true;
            yield return null;
            var stick = instance.GetComponentInChildren<OnScreenStick>(true);
            var move = new InputAction(binding: "<Gamepad>/leftStick");
            move.Enable();
            try
            {
                var start = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position);
                var down = new PointerEventData(EventSystem.current) { position = start, pressPosition = start };
                stick.OnPointerDown(down);
                var drag = new PointerEventData(EventSystem.current) { position = start + new Vector2(200f, 0f), pressPosition = start };
                stick.OnDrag(drag);
                yield return null;
                Assert.That(move.ReadValue<Vector2>().x, Is.GreaterThan(0.5f), "Arrastrar a la derecha mueve el stick a la derecha.");
                stick.OnPointerUp(drag);
                yield return null;
                Assert.That(move.ReadValue<Vector2>().magnitude, Is.LessThan(0.01f), "Al soltar vuelve al centro.");
            }
            finally
            {
                move.Dispose();
            }
        }

        /// <summary>
        /// Un toque de verdad, por el EventSystem de la escena, y no llamando al control: así falló en el S24+, donde el
        /// EventSystem propio del prefab había perdido sus acciones de UI y ningún toque llegaba al joystick.
        /// </summary>
        [UnityTest]
        public IEnumerator RealTouch_InAGameScene_MovesTheStick_AndPressesAttack()
        {
            // En modo batch el Editor no tiene el foco y el Input System tiraría los toques simulados.
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var screen = InputSystem.AddDevice<Touchscreen>();
            yield return SceneManager.LoadSceneAsync("Olmeca_Ciudad", LoadSceneMode.Additive);
            var move = new InputAction(binding: "<Gamepad>/leftStick");
            var attack = new InputAction(binding: "<Gamepad>/buttonWest");
            move.Enable();
            attack.Enable();
            try
            {
                for (var i = 0; i < 5; i++)
                    yield return null;
                Assert.IsTrue(touch.IsVisible, "Con pantalla táctil se muestran solos.");
                Assert.IsNotNull(EventSystem.current, "La escena de juego trae el EventSystem que reparte los toques.");

                var start = ScreenPoint(instance.GetComponentInChildren<OnScreenStick>().transform);
                yield return Touch(screen, TouchPhase.Began, start);
                for (var i = 1; i <= 4; i++)
                    yield return Touch(screen, TouchPhase.Moved, start + new Vector2(40f * i, 0f));
                Assert.That(move.ReadValue<Vector2>().x, Is.GreaterThan(0.5f), "Arrastrar el joystick a la derecha mueve el stick.");
                yield return Touch(screen, TouchPhase.Ended, start + new Vector2(160f, 0f));
                yield return null;
                Assert.That(move.ReadValue<Vector2>().magnitude, Is.LessThan(0.01f));

                var button = ScreenPoint(instance.GetComponentsInChildren<OnScreenButton>().Single(b => b.name == "Atacar").transform);
                yield return Touch(screen, TouchPhase.Began, button, 2);
                yield return null;
                Assert.IsTrue(attack.IsPressed(), "Tocar Atacar pulsa el botón Oeste.");
                yield return Touch(screen, TouchPhase.Ended, button, 2);
            }
            finally
            {
                move.Dispose();
                attack.Dispose();
                InputSystem.RemoveDevice(screen);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
            yield return SceneManager.UnloadSceneAsync("Olmeca_Ciudad");
        }

        static Vector2 ScreenPoint(Transform control) => RectTransformUtility.WorldToScreenPoint(null, control.position);

        static IEnumerator Touch(Touchscreen screen, TouchPhase phase, Vector2 position, int id = 1)
        {
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = id, phase = phase, position = position });
            yield return null;
            yield return null;
        }

        [Test]
        public void EveryOnScreenControl_IsBoundToAPlayerAction()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            var player = asset.FindActionMap("Player");
            var paths = player.bindings.Select(b => b.path).ToHashSet();
            foreach (var control in instance.GetComponentsInChildren<OnScreenControl>(true))
                Assert.That(paths, Does.Contain(control.controlPath), $"{control.name}: ninguna acción del jugador escucha {control.controlPath}.");
            Assert.That(player.FindAction("Attack").bindings.Select(b => b.path), Has.No.Member("<Touchscreen>/primaryTouch/tap"),
                "Tocar cualquier parte de la pantalla no debe atacar: tocarías el joystick o un botón.");
        }
    }
}
