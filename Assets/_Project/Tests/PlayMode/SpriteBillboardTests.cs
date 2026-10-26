using System.Collections;
using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    public class SpriteBillboardTests
    {
        GameObject cameraObject;
        GameObject spriteObject;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("CamaraPrueba");
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 6f, -9f), Quaternion.Euler(30f, 45f, 0f));
            spriteObject = new GameObject("SpritePrueba");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(cameraObject);
            Object.Destroy(spriteObject);
        }

        SpriteBillboard AddBillboard(SpriteBillboard.BillboardMode mode)
        {
            var billboard = spriteObject.AddComponent<SpriteBillboard>();
            billboard.Mode = mode;
            billboard.CameraTransform = cameraObject.transform;
            return billboard;
        }

        [UnityTest]
        public IEnumerator VerticalAxis_StaysUpright_AndMatchesCameraYaw()
        {
            AddBillboard(SpriteBillboard.BillboardMode.VerticalAxis);
            yield return null;

            var euler = spriteObject.transform.eulerAngles;
            Assert.That(Mathf.DeltaAngle(euler.x, 0f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(euler.z, 0f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(euler.y, 45f), Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator CameraPlane_CopiesCameraRotation()
        {
            AddBillboard(SpriteBillboard.BillboardMode.CameraPlane);
            yield return null;

            Assert.That(Quaternion.Angle(spriteObject.transform.rotation, cameraObject.transform.rotation), Is.LessThan(0.01f));
        }
    }
}
