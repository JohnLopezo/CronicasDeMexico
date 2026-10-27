using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    public class PlayerInteractorTests
    {
        const string InscriptionText = "Texto de prueba";
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

        InputSettings previousSettings;
        InputSettings testSettings;
        Keyboard keyboard;
        InputActionAsset actions;
        StringEventChannel promptChannel;
        StringEventChannel messageChannel;
        GameObject inscription;
        GameObject player;
        readonly List<string> prompts = new();
        readonly List<string> messages = new();

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

            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var interactAction = actions.AddActionMap("Player").AddAction("Interact", InputActionType.Button, "<Keyboard>/e");

            promptChannel = ScriptableObject.CreateInstance<StringEventChannel>();
            messageChannel = ScriptableObject.CreateInstance<StringEventChannel>();
            prompts.Clear();
            messages.Clear();
            promptChannel.Subscribe(prompts.Add);
            messageChannel.Subscribe(messages.Add);

            var layer = LayerMask.NameToLayer("Interactuable");
            Assert.That(layer, Is.Not.EqualTo(-1), "Falta el layer 'Interactuable' en Tags and Layers.");

            // Lejos del jugador al principio; cada prueba lo acerca cuando lo necesita.
            inscription = new GameObject("InscripcionPrueba") { layer = layer };
            inscription.transform.position = new Vector3(10f, 0.7f, 0f);
            var trigger = inscription.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            var interactable = inscription.AddComponent<InscriptionInteractable>();
            typeof(InscriptionInteractable).GetField("text", PrivateField).SetValue(interactable, InscriptionText);
            typeof(InscriptionInteractable).GetField("messageChannel", PrivateField).SetValue(interactable, messageChannel);

            // Se crea inactivo para asignar las referencias antes de que corra OnEnable.
            player = new GameObject("JugadorPrueba");
            player.SetActive(false);
            var interactor = player.AddComponent<PlayerInteractor>();
            typeof(PlayerInteractor).GetField("interactAction", PrivateField).SetValue(interactor, InputActionReference.Create(interactAction));
            typeof(PlayerInteractor).GetField("interactableLayers", PrivateField).SetValue(interactor, (LayerMask)(1 << layer));
            typeof(PlayerInteractor).GetField("promptChannel", PrivateField).SetValue(interactor, promptChannel);
            typeof(PlayerInteractor).GetField("messageChannel", PrivateField).SetValue(interactor, messageChannel);
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(inscription);
            Object.Destroy(promptChannel);
            Object.Destroy(messageChannel);
            Object.Destroy(actions);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = previousSettings != null ? previousSettings : ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(testSettings);
        }

        IEnumerator PressInteract()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        IEnumerator MoveInscriptionNearPlayer()
        {
            inscription.transform.position = new Vector3(1f, 0.7f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f); // más de una búsqueda (cada 0.1 s)
        }

        [UnityTest]
        public IEnumerator Interact_DoesNothing_WithoutInteractableInRange()
        {
            yield return new WaitForSeconds(0.25f);
            yield return PressInteract();

            Assert.That(prompts, Is.Empty, "No debe mostrarse aviso sin nada cerca.");
            Assert.That(messages, Is.Empty, "No debe abrirse ningún mensaje.");
        }

        [UnityTest]
        public IEnumerator Prompt_Shows_WhenInRange_AndInteractOpensThenClosesMessage()
        {
            yield return MoveInscriptionNearPlayer();
            Assert.That(prompts, Is.Not.Empty, "No apareció el aviso al acercarse.");
            StringAssert.Contains("Examinar", prompts[^1]);
            StringAssert.Contains("E", prompts[^1]);

            yield return PressInteract();
            Assert.That(messages, Is.EqualTo(new[] { InscriptionText }), "Interactuar no mostró la inscripción.");
            Assert.That(prompts[^1], Is.Null, "Con el mensaje abierto el aviso debe ocultarse.");

            yield return PressInteract();
            Assert.That(messages.Count, Is.EqualTo(2), "La segunda pulsación debe cerrar el mensaje y no reabrirlo.");
            Assert.That(messages[^1], Is.Null);
            StringAssert.Contains("Examinar", prompts[^1], "Al cerrar el mensaje debe volver el aviso.");
        }

        [UnityTest]
        public IEnumerator WalkingAway_ClosesMessage_AndHidesPrompt()
        {
            yield return MoveInscriptionNearPlayer();
            yield return PressInteract();
            Assert.That(messages, Is.EqualTo(new[] { InscriptionText }));

            inscription.transform.position = new Vector3(10f, 0.7f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);

            Assert.That(messages[^1], Is.Null, "Alejarse debe cerrar el mensaje.");
            Assert.That(prompts[^1], Is.Null, "Alejarse debe ocultar el aviso.");
        }
    }
}
