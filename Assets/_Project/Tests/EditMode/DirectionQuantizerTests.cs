using CronicasDeMexico.Gameplay.Movement;
using NUnit.Framework;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    public class DirectionQuantizerTests
    {
        [TestCase(0f, 1f, ScreenDirection.North)]
        [TestCase(1f, 1f, ScreenDirection.NorthEast)]
        [TestCase(1f, 0f, ScreenDirection.East)]
        [TestCase(1f, -1f, ScreenDirection.SouthEast)]
        [TestCase(0f, -1f, ScreenDirection.South)]
        [TestCase(-1f, -1f, ScreenDirection.SouthWest)]
        [TestCase(-1f, 0f, ScreenDirection.West)]
        [TestCase(-1f, 1f, ScreenDirection.NorthWest)]
        public void FromWorld_ReturnsTheEightDirections_WithCameraYaw0(float x, float z, ScreenDirection expected)
        {
            Assert.That(DirectionQuantizer.FromWorld(new Vector3(x, 0f, z), 0f), Is.EqualTo(expected));
        }

        [Test]
        public void FromWorld_IsRelativeToCameraYaw()
        {
            // Con la cámara mirando hacia +X, moverse hacia +X es alejarse de ella.
            Assert.That(DirectionQuantizer.FromWorld(Vector3.right, 90f), Is.EqualTo(ScreenDirection.North));
            Assert.That(DirectionQuantizer.FromWorld(Vector3.forward, 90f), Is.EqualTo(ScreenDirection.West));
        }

        [Test]
        public void FromWorld_RoundsToNearestSector()
        {
            // 20° está más cerca del Norte (0°) que del Noreste (45°); 25°, del Noreste.
            Assert.That(DirectionQuantizer.FromWorld(Quaternion.Euler(0f, 20f, 0f) * Vector3.forward, 0f), Is.EqualTo(ScreenDirection.North));
            Assert.That(DirectionQuantizer.FromWorld(Quaternion.Euler(0f, 25f, 0f) * Vector3.forward, 0f), Is.EqualTo(ScreenDirection.NorthEast));
        }

        [TestCase(ScreenDirection.West, ScreenDirection.East)]
        [TestCase(ScreenDirection.NorthWest, ScreenDirection.NorthEast)]
        [TestCase(ScreenDirection.SouthWest, ScreenDirection.SouthEast)]
        [TestCase(ScreenDirection.North, ScreenDirection.North)]
        [TestCase(ScreenDirection.South, ScreenDirection.South)]
        public void Mirror_SwapsWestAndEast(ScreenDirection direction, ScreenDirection expected)
        {
            Assert.That(DirectionQuantizer.Mirror(direction), Is.EqualTo(expected));
            Assert.That(DirectionQuantizer.Mirror(expected), Is.EqualTo(direction));
        }

        [Test]
        public void IsWestward_IsTrueOnlyForTheThreeWestDirections()
        {
            Assert.IsTrue(DirectionQuantizer.IsWestward(ScreenDirection.West));
            Assert.IsTrue(DirectionQuantizer.IsWestward(ScreenDirection.NorthWest));
            Assert.IsTrue(DirectionQuantizer.IsWestward(ScreenDirection.SouthWest));
            Assert.IsFalse(DirectionQuantizer.IsWestward(ScreenDirection.North));
            Assert.IsFalse(DirectionQuantizer.IsWestward(ScreenDirection.East));
            Assert.IsFalse(DirectionQuantizer.IsWestward(ScreenDirection.South));
        }
    }
}
