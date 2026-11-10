using System.Collections;
using System.Linq;
using CronicasDeMexico.UI.Touch;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.TestTools;

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
