using System.Collections;
using System.Reflection;
using CronicasDeMexico.Gameplay.Npc;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    public class NpcPatrolTests
    {
        GameObject ground;
        GameObject npc;
        NpcPatrol patrol;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);

            // Se crea inactivo para asignar los puntos antes de que empiece a moverse.
            npc = new GameObject("NpcPrueba");
            npc.SetActive(false);
            var controller = npc.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            controller.minMoveDistance = 0f;
            patrol = npc.AddComponent<NpcPatrol>();
            typeof(NpcPatrol).GetField("patrolPoints", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(patrol, new[] { new Vector3(3f, 0f, 0f), new Vector3(3f, 0f, 3f) });
            npc.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(npc);
            Object.Destroy(ground);
        }

        [UnityTest]
        public IEnumerator Patrol_WalksToFirstPoint_ThenTargetsTheNext()
        {
            // 3 m a 1.5 m/s: unos 2 s más la aceleración.
            yield return new WaitForSeconds(2.6f);

            Assert.That(npc.transform.position.x, Is.EqualTo(3f).Within(0.3f), "No llegó al primer punto.");
            Assert.That(patrol.TargetIndex, Is.EqualTo(1), "Al llegar debe apuntar al siguiente punto.");
            Assert.That(npc.transform.position.y, Is.EqualTo(0f).Within(0.1f), "Debe seguir sobre el suelo.");
        }

        [UnityTest]
        public IEnumerator Hold_StopsAndFacesTarget_ReleaseResumes()
        {
            yield return new WaitForSeconds(0.5f);
            patrol.Hold(npc.transform.position + Vector3.back * 2f);
            yield return new WaitForSeconds(0.5f);

            var heldPosition = npc.transform.position;
            Assert.That(patrol.PlanarVelocity.magnitude, Is.LessThan(0.01f), "Detenido no debe moverse.");
            Assert.That(Vector3.Dot(patrol.FacingDirection, Vector3.back), Is.GreaterThan(0.99f), "Debe mirar hacia quien le habla.");

            yield return new WaitForSeconds(0.5f);
            Assert.That(Vector3.Distance(npc.transform.position, heldPosition), Is.LessThan(0.01f));

            patrol.Release();
            yield return new WaitForSeconds(0.5f);
            Assert.That(npc.transform.position.x, Is.GreaterThan(heldPosition.x + 0.3f), "Al soltarlo debe seguir su patrulla.");
        }
    }
}
