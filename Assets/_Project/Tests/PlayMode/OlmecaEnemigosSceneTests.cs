using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Rewards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Los enemigos de la lámina del autor colocados en las escenas (etapa 4, Docs/Enemigos/Olmeca.md): los que tocan en
    /// cada una, sobre suelo firme (los pisos son losas sobre el vacío), con su botín, y en la mazmorra sin reaparecer.
    /// </summary>
    public class OlmecaEnemigosSceneTests
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

        [UnityTest]
        public IEnumerator Jungle_HasItsFauna_OnSolidGround() =>
            Check("Olmeca_Selva", "serpiente:1 guacamaya:1 mono:1 avispa:3", respawns: true);

        [UnityTest]
        public IEnumerator Threshold_HasBats() => Check("Olmeca_Mazmorra_P1", "murcielago:2", respawns: false);

        [UnityTest]
        public IEnumerator Swamp_HasCrocodiles() => Check("Olmeca_Mazmorra_P2", "cocodrilo:2", respawns: false);

        [UnityTest]
        public IEnumerator Antechamber_HasJaguarSpirits() => Check("Olmeca_Mazmorra_P3", "espiritu_jaguar:2", respawns: false);

        static IEnumerator Check(string sceneName, string expected, bool respawns)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(scene);
            yield return null;

            var enemies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WildEnemy>(true)).ToArray();
            var counts = enemies.GroupBy(enemy => enemy.Data.id).ToDictionary(group => group.Key, group => group.Count());
            var wanted = expected.Split(' ').Select(pair => pair.Split(':')).ToDictionary(pair => pair[0], pair => int.Parse(pair[1]));
            CollectionAssert.AreEquivalent(wanted, counts, sceneName);

            var start = new Dictionary<WildEnemy, float>();
            foreach (var enemy in enemies)
            {
                start[enemy] = enemy.transform.position.y;
                Assert.IsNotNull(enemy.GetComponent<EnemyReward>()?.Table, $"{enemy.name}: sin botín.");
                var respawnField = typeof(WildEnemy).GetField("respawns", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.AreEqual(respawns, respawnField.GetValue(enemy), $"{enemy.name}: reaparición.");
            }

            yield return new WaitForSeconds(1.5f); // si alguno no tiene suelo, la gravedad lo deja caer
            foreach (var enemy in enemies)
            {
                Assert.That(enemy.transform.position.y, Is.GreaterThan(start[enemy] - 0.5f), $"{enemy.name} se cae al vacío.");
                Assert.That(enemy.Current, Is.Not.EqualTo(WildEnemy.State.Dead), enemy.name);
            }
        }
    }
}
