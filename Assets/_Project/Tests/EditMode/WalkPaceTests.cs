using CronicasDeMexico.Gameplay.Player;
using NUnit.Framework;

namespace CronicasDeMexico.Tests
{
    public class WalkPaceTests
    {
        [TestCase(4f, 4f, 1f)]     // marcha normal: a su ritmo
        [TestCase(2f, 4f, 0.5f)]   // agua que frena a la mitad
        [TestCase(0.5f, 4f, 0.5f)] // mando apenas inclinado: no baja de la mitad
        [TestCase(10f, 4f, 1.5f)]  // esquiva o empujón: no sube de 1,5
        [TestCase(3f, 0f, 1f)]     // sin velocidad de marcha (jefes): ritmo fijo
        public void WalkPace_FollowsTheSpeed_WithinLimits(float speed, float cruise, float expected)
        {
            Assert.That(DirectionalSpriteAnimator.WalkPace(speed, cruise), Is.EqualTo(expected).Within(1e-4f));
        }
    }
}
