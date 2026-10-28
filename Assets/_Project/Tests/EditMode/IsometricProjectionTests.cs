using CronicasDeMexico.World.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    public class IsometricProjectionTests
    {
        const float Tolerance = 1e-4f;

        static void AssertVector(Vector3 expected, Vector3 actual) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(Tolerance), $"esperado {expected}, obtenido {actual}");

        // Los ejes de pantalla del flujo Blender (pixelart_comun.py: DERECHA, ARRIBA) deben coincidir con los de la
        // cámara de Unity: si no, los sprites prerenderizados no casarían con la proyección del juego.
        [Test]
        public void CameraAxes_MatchPipelineScreenAxes()
        {
            AssertVector(IsometricProjection.FromPipeline(0.70710678f, 0.70710678f, 0f),
                IsometricProjection.CameraRotation * Vector3.right);
            AssertVector(IsometricProjection.FromPipeline(-0.35355339f, 0.35355339f, 0.8660254f),
                IsometricProjection.CameraRotation * Vector3.up);
            AssertVector(IsometricProjection.FromPipeline(-0.61237244f, 0.61237244f, -0.5f),
                IsometricProjection.CameraRotation * Vector3.forward);
        }

        [Test]
        public void SnapToPixelGrid_KeepsDepth_AndIsIdempotent()
        {
            const float pixel = 1f / IsometricProjection.PixelsPerUnit;
            var forward = IsometricProjection.CameraRotation * Vector3.forward;
            var position = new Vector3(3.217f, 0.4f, -7.91f);

            var snapped = IsometricProjection.SnapToPixelGrid(position, pixel);

            Assert.AreEqual(Vector3.Dot(position, forward), Vector3.Dot(snapped, forward), Tolerance);
            Assert.That(Vector3.Distance(position, snapped), Is.LessThanOrEqualTo(pixel * 0.71f));
            AssertVector(snapped, IsometricProjection.SnapToPixelGrid(snapped, pixel));
        }

        [TestCase(1080, 3, 3.9774f)]
        [TestCase(720, 2, 3.9774f)]
        [TestCase(768, 2, 4.2426f)]
        [TestCase(200, 1, 2.2097f)]
        public void OrthographicSize_ShowsArtAtIntegerScale(int screenHeight, int scale, float size)
        {
            Assert.AreEqual(scale, IsometricProjection.IntegerScale(screenHeight));
            Assert.AreEqual(size, IsometricProjection.OrthographicSize(screenHeight), 1e-3f);
        }
    }
}
