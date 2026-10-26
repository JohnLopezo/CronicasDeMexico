using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    public class ShaderTests
    {
        const string SpriteLitPath = "Assets/_Project/Art/Shaders/HD2D_SpriteLit.shader";

        [Test]
        public void HD2DSpriteLit_CompilesWithoutErrors()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SpriteLitPath);
            Assert.IsNotNull(shader, $"No se encontró {SpriteLitPath}");

            var errors = ShaderUtil.GetShaderMessages(shader)
                .Where(message => message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                .Select(message => $"{message.message} (línea {message.line})");

            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", errors));
        }
    }
}
