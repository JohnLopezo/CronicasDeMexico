using System.Collections;
using System.Linq;
using System.Reflection;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Rewards;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Piezas de la lámina «Mazmorras ancestrales» en los pisos (D4): las que tocan, sobre suelo firme, las de suelo
    /// debajo de los personajes, la placa enlazada a sus pinchos, las vasijas con botín y el fuego animado.
    /// </summary>
    public class OlmecaPiezasSceneTests
    {
        [UnityTearDown]
        public IEnumerator UnloadRegions()
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name.StartsWith("Olmeca_"))
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest] public IEnumerator Threshold_HasItsPieces() => Check(1, spikes: 4, plates: 1, pots: 4, braziers: 4);
        [UnityTest] public IEnumerator Swamp_HasItsPieces() => Check(2, spikes: 3, plates: 0, pots: 3, braziers: 3);
        [UnityTest] public IEnumerator Antechamber_HasItsPieces() => Check(3, spikes: 2, plates: 0, pots: 3, braziers: 2);

        static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        static IEnumerator Check(int floor, int spikes, int plates, int pots, int braziers)
        {
            var name = $"Olmeca_Mazmorra_P{floor}";
            yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(name);
            SceneManager.SetActiveScene(scene);
            yield return null;

            var traps = All<SpikeTrap>(scene);
            var plateList = All<PressurePlate>(scene);
            var potList = All<BreakablePot>(scene);
            var fires = All<SpriteFrameCycler>(scene).Where(c => c.name.StartsWith("Brasero")).ToArray();
            Assert.That(traps.Length, Is.EqualTo(spikes), "pinchos");
            Assert.That(plateList.Length, Is.EqualTo(plates), "placas");
            Assert.That(potList.Length, Is.EqualTo(pots), "vasijas");
            Assert.That(fires.Length, Is.EqualTo(braziers), "braseros");
            Assert.That(fires.All(f => f.FrameCount == 3), "El fuego tiene sus tres cuadros.");

            foreach (var plate in plateList)
            {
                var linked = (SpikeTrap[])typeof(PressurePlate).GetField("traps", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(plate);
                Assert.That(linked.Length, Is.EqualTo(2), "La placa activa los dos pinchos de sus lados.");
            }
            foreach (var pot in potList)
                Assert.IsNotNull(pot.GetComponent<EnemyReward>()?.Table, $"{pot.name}: sin botín.");

            // Las piezas de suelo se dibujan antes que los personajes (cola de render menor) para no taparles los pies.
            var character = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .First(r => r.GetComponentInParent<CharacterController>() != null && r.transform.parent != null && r.transform.parent.CompareTag("Player"));
            foreach (var floorPiece in traps.Select(t => t.GetComponentInChildren<SpriteRenderer>())
                         .Concat(plateList.Select(p => p.GetComponentInChildren<SpriteRenderer>())))
                Assert.Less(floorPiece.sharedMaterial.renderQueue, character.sharedMaterial.renderQueue, $"{floorPiece.transform.parent.name} taparía al jugador.");

            // Todas sobre suelo firme: los pisos son losas sobre el vacío.
            foreach (var piece in traps.Cast<Component>().Concat(plateList).Concat(potList).Concat(fires))
                Assert.IsTrue(Physics.Raycast(piece.transform.position + Vector3.up * 3f, Vector3.down, 6f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore), $"{piece.name} en {name} no tiene suelo debajo.");
        }
    }
}
