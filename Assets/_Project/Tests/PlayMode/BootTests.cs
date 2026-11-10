using System.Collections;
using CronicasDeMexico.Core.Boot;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CronicasDeMexico.Tests
{
    public class BootTests
    {
        [UnityTest]
        public IEnumerator BootScene_IsLoaded_AndRegistersServices()
        {
            yield return null;

            Assert.IsTrue(SceneManager.GetSceneByName(BootSceneLoader.BootSceneName).isLoaded);
            Assert.IsNotNull(Object.FindAnyObjectByType<GameBootstrapper>());
            Assert.IsTrue(ServiceLocator.TryGet<QualityService>(out _));
            Assert.IsTrue(ServiceLocator.TryGet<StoryState>(out _));
            Assert.IsTrue(ServiceLocator.TryGet<QuestService>(out _));
            Assert.IsTrue(ServiceLocator.TryGet<SaveService>(out _));
        }
    }
}
