using System.Collections;
using System.Reflection;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>Trampas y vasijas de la mazmorra (D2): pinchos avisados, placa de presión, cerca de estacas y vasijas.</summary>
    public class DungeonTrapsTests
    {
        const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

        GameObject ground;
        GameObject player;
        Health playerHealth;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            // Con PlayerMovement el CharacterController se mueve cada cuadro: así Unity le avisa de los triggers.
            player = new GameObject("JugadorPrueba") { tag = "Player" };
            player.SetActive(false);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            player.AddComponent<PlayerMovement>();
            playerHealth = player.AddComponent<Health>();
            typeof(Health).GetField("invulnerabilityTime", PrivateField).SetValue(playerHealth, 0.3f);
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(ground);
        }

        static Sprite Pixel(Color color)
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        static GameObject Zone(string name, Vector3 position)
        {
            var zone = new GameObject(name);
            zone.transform.position = position;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1f, 1f, 1f);
            box.center = new Vector3(0f, 0.5f, 0f);
            return zone;
        }

        SpikeTrap Trap(Vector3 position, bool periodic, out SpriteRenderer renderer, out Sprite raised)
        {
            var zone = Zone("Pinchos", position);
            zone.SetActive(false);
            renderer = zone.AddComponent<SpriteRenderer>();
            raised = Pixel(Color.red);
            var trap = zone.AddComponent<SpikeTrap>();
            trap.Configure(renderer, Pixel(Color.gray), raised, periodic, 0f);
            typeof(SpikeTrap).GetField("period", PrivateField).SetValue(trap, 2.5f);
            typeof(SpikeTrap).GetField("warning", PrivateField).SetValue(trap, 0.5f);
            typeof(SpikeTrap).GetField("raisedTime", PrivateField).SetValue(trap, 1f);
            zone.SetActive(true);
            return trap;
        }

        static IEnumerator Until(System.Func<bool> condition, float seconds, string message)
        {
            for (var t = 0f; !condition(); t += Time.deltaTime)
            {
                Assert.Less(t, seconds, message);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Spikes_WarnBeforeRising_AndOnlyHurtWhenRaised()
        {
            var trap = Trap(Vector3.zero, periodic: true, out var renderer, out var raised);
            try
            {
                yield return new WaitForSeconds(0.3f);
                Assert.That(trap.Current, Is.EqualTo(SpikeTrap.State.Lowered));
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "Bajados no dañan.");
                yield return Until(() => trap.Current == SpikeTrap.State.Warning, 2f, "Los pinchos no avisaron.");
                yield return new WaitForSeconds(0.2f);
                Assert.That(playerHealth.Current, Is.EqualTo(playerHealth.Max), "Durante el aviso no dañan: da tiempo a apartarse.");
                Assert.That(renderer.color, Is.Not.EqualTo(Color.white), "El aviso se ve.");
                yield return Until(() => trap.Current == SpikeTrap.State.Raised, 1f, "Los pinchos no subieron.");
                yield return new WaitForSeconds(0.15f);
                Assert.That(playerHealth.Current, Is.LessThan(playerHealth.Max), "Levantados, dañan a quien esté encima.");
                Assert.AreSame(raised, renderer.sprite);
            }
            finally
            {
                Object.Destroy(trap.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator PressurePlate_RaisesItsLinkedSpikes()
        {
            var trap = Trap(new Vector3(4f, 0f, 0f), periodic: false, out _, out _);
            var plateObject = Zone("Placa", Vector3.zero);
            var plateRenderer = plateObject.AddComponent<SpriteRenderer>();
            var pressed = Pixel(Color.cyan);
            var plate = plateObject.AddComponent<PressurePlate>();
            plate.Configure(plateRenderer, Pixel(Color.gray), pressed, new[] { trap });
            try
            {
                yield return new WaitForSeconds(0.1f);
                Assert.IsTrue(plate.IsPressed, "El jugador está encima.");
                Assert.AreSame(pressed, plateRenderer.sprite, "Se hunde y se enciende.");
                Assert.That(trap.Current, Is.Not.EqualTo(SpikeTrap.State.Lowered), "Activa los pinchos enlazados.");

                var controller = player.GetComponent<CharacterController>();
                controller.enabled = false; // con el controlador activo, Move lo devolvería a su sitio
                player.transform.position = new Vector3(-5f, 0f, 0f);
                controller.enabled = true;
                yield return new WaitForSeconds(0.6f);
                Assert.IsFalse(plate.IsPressed, "Sin nadie encima, vuelve a subir.");
            }
            finally
            {
                Object.Destroy(trap.gameObject);
                Object.Destroy(plateObject);
            }
        }

        [UnityTest]
        public IEnumerator StakeFence_HurtsOnTouch()
        {
            var fence = Zone("Estacas", Vector3.zero);
            fence.AddComponent<StakeFence>();
            try
            {
                yield return new WaitForSeconds(0.2f);
                Assert.That(playerHealth.Current, Is.LessThan(playerHealth.Max));
            }
            finally
            {
                Object.Destroy(fence);
            }
        }

        [UnityTest]
        public IEnumerator Pot_BreaksWithOneHit_AndStopsBlocking()
        {
            var pot = new GameObject("Vasija");
            pot.SetActive(false);
            pot.transform.position = new Vector3(3f, 0f, 0f);
            var collider = pot.AddComponent<BoxCollider>();
            var health = pot.AddComponent<Health>();
            typeof(Health).GetField("maxHealth", PrivateField).SetValue(health, 1);
            var renderer = pot.AddComponent<SpriteRenderer>();
            renderer.sprite = Pixel(Color.yellow);
            var broken = Pixel(Color.black);
            pot.AddComponent<BreakablePot>().Configure(renderer, broken);
            pot.SetActive(true);
            try
            {
                yield return null;
                Assert.IsTrue(health.TryDamage(10, player.transform.position));
                Assert.IsTrue(pot.GetComponent<BreakablePot>().IsBroken);
                Assert.AreSame(broken, renderer.sprite, "Quedan los tiestos.");
                Assert.IsFalse(collider.enabled, "Rota no estorba ni recibe más golpes.");
            }
            finally
            {
                Object.Destroy(pot);
            }
        }
    }
}
