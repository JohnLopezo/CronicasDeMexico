using CronicasDeMexico.Core.Feedback;
using NUnit.Framework;

namespace CronicasDeMexico.Tests
{
    // E5: las opciones de comodidad se guardan y se respetan. Se restauran al acabar: son las del dispositivo.
    public class ComfortSettingsTests
    {
        bool shake;
        bool numbers;
        TextSpeed speed;

        [SetUp]
        public void SetUp()
        {
            shake = ComfortSettings.ScreenShake;
            numbers = ComfortSettings.DamageNumbers;
            speed = ComfortSettings.TextSpeed;
        }

        [TearDown]
        public void TearDown()
        {
            ComfortSettings.ScreenShake = shake;
            ComfortSettings.DamageNumbers = numbers;
            ComfortSettings.TextSpeed = speed;
        }

        [Test]
        public void ScreenShake_IsIgnored_WhenTurnedOff()
        {
            ComfortSettings.ScreenShake = false;
            ScreenShake.Request(0.5f, 1f);
            Assert.That(ScreenShake.CurrentAmplitude, Is.EqualTo(0f));

            ComfortSettings.ScreenShake = true;
            ScreenShake.Request(0.5f, 1f);
            Assert.That(ScreenShake.CurrentAmplitude, Is.GreaterThan(0f));
        }

        [Test]
        public void DamageNumbers_AreNotReported_WhenTurnedOff()
        {
            var reported = 0;
            void Count(UnityEngine.Vector3 position, int amount, DamageKind kind) => reported++;
            DamageNumbers.Reported += Count;
            try
            {
                ComfortSettings.DamageNumbers = false;
                DamageNumbers.Report(UnityEngine.Vector3.zero, 5, DamageKind.Dealt);
                Assert.That(reported, Is.EqualTo(0));
                ComfortSettings.DamageNumbers = true;
                DamageNumbers.Report(UnityEngine.Vector3.zero, 5, DamageKind.Dealt);
                DamageNumbers.Report(UnityEngine.Vector3.zero, 0, DamageKind.Dealt);
                Assert.That(reported, Is.EqualTo(1), "Un golpe de 0 no muestra número.");
            }
            finally
            {
                DamageNumbers.Reported -= Count;
            }
        }

        [Test]
        public void TextSpeed_SetsCharactersPerSecond_AndInstantIsZero()
        {
            ComfortSettings.TextSpeed = TextSpeed.Lento;
            var slow = ComfortSettings.CharactersPerSecond;
            ComfortSettings.TextSpeed = TextSpeed.Rapido;
            Assert.That(ComfortSettings.CharactersPerSecond, Is.GreaterThan(slow));
            ComfortSettings.TextSpeed = TextSpeed.Instantaneo;
            Assert.That(ComfortSettings.CharactersPerSecond, Is.EqualTo(0f));
        }
    }
}
