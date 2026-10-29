using CronicasDeMexico.Gameplay.Movement;
using NUnit.Framework;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    public class PlanarMovementTests
    {
        const float Tolerance = 0.0001f;

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(Tolerance), $"Esperado {expected}, obtenido {actual}");
        }

        [Test]
        public void InputToWorldDirection_ReturnsZero_WithoutInput()
        {
            AssertVector(Vector3.zero, PlanarMovement.InputToWorldDirection(Vector2.zero, 0f));
        }

        [Test]
        public void InputToWorldDirection_PointsToPositiveZ_WhenForwardAndCameraYaw0()
        {
            AssertVector(Vector3.forward, PlanarMovement.InputToWorldDirection(Vector2.up, 0f));
        }

        [Test]
        public void InputToWorldDirection_PointsToPositiveX_WhenForwardAndCameraYaw90()
        {
            AssertVector(Vector3.right, PlanarMovement.InputToWorldDirection(Vector2.up, 90f));
        }

        [Test]
        public void InputToWorldDirection_HasUnitMagnitude_ForRawDiagonal()
        {
            var direction = PlanarMovement.InputToWorldDirection(new Vector2(1f, 1f), 0f);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(Tolerance));
            AssertVector(new Vector3(1f, 0f, 1f).normalized, direction);
        }

        [Test]
        public void InputToWorldDirection_KeepsMagnitude_ForPartialInput()
        {
            AssertVector(new Vector3(0.5f, 0f, 0f), PlanarMovement.InputToWorldDirection(new Vector2(0.5f, 0f), 0f));
        }

        [Test]
        public void InputToWorldDirection_HasNoVerticalComponent()
        {
            Assert.That(PlanarMovement.InputToWorldDirection(new Vector2(-0.3f, 0.8f), 37f).y, Is.EqualTo(0f));
        }

        [Test]
        public void ApplyGravity_ResetsFallSpeed_WhenGrounded()
        {
            var speed = PlanarMovement.ApplyGravity(-15f, grounded: true, gravity: -20f, deltaTime: 0.1f);
            Assert.That(speed, Is.EqualTo(PlanarMovement.GroundedVerticalSpeed - 2f).Within(Tolerance));
        }

        [Test]
        public void ApplyGravity_Accelerates_WhenAirborne()
        {
            var speed = PlanarMovement.ApplyGravity(-15f, grounded: false, gravity: -20f, deltaTime: 0.1f);
            Assert.That(speed, Is.EqualTo(-17f).Within(Tolerance));
        }
    }
}
