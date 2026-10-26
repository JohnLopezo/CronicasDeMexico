using System;
using CronicasDeMexico.Core.Services;
using NUnit.Framework;

namespace CronicasDeMexico.Tests
{
    public class ServiceLocatorTests
    {
        class FakeService { }

        [SetUp]
        public void SetUp() => ServiceLocator.Clear();

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void Get_ReturnsRegisteredInstance()
        {
            var service = new FakeService();
            ServiceLocator.Register(service);

            Assert.AreSame(service, ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void Get_Throws_WhenNotRegistered()
        {
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void TryGet_ReturnsFalse_AfterUnregister()
        {
            ServiceLocator.Register(new FakeService());
            ServiceLocator.Unregister<FakeService>();

            Assert.IsFalse(ServiceLocator.TryGet<FakeService>(out var found));
            Assert.IsNull(found);
        }
    }
}
