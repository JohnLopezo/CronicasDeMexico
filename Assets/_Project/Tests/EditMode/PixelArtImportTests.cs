using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace CronicasDeMexico.Tests
{
    /// <summary>Sprites del flujo Blender importados como pixel art, y materiales y datos de la ciudad olmeca.</summary>
    public class PixelArtImportTests
    {
        const string SpritesFolder = "Assets/_Project/Art/Environment/Olmeca/Sprites";
        const string ShaderPath = "Assets/_Project/Art/Shaders/PixelArt_Unlit.shader";
        const string MaterialsFolder = "Assets/_Project/Art/Environment/Olmeca/Materials/PixelArt";
        const string CityDataPath = "Assets/_Project/Data/World/Olmeca/Ciudad.json";
        const string AtlasPath = SpritesFolder + "/Atlas_Olmeca.spriteatlasv2";
        const float PixelsPerUnit = 45.254834f;

        [Test]
        public void Sprites_AreImportedAsPixelArt()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            Assert.Greater(paths.Length, 50, "Faltan sprites: ¿se ejecutó el flujo de arte?");

            foreach (var path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
                Assert.AreEqual(PixelsPerUnit, importer.spritePixelsPerUnit, 1e-3f, path);
                Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
                Assert.IsFalse(importer.mipmapEnabled, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);
            }
        }

        [Test]
        public void Pivots_MatchPipeline()
        {
            // Palma: lienzo de 64×128 con la base del tronco en (36, 12) desde abajo a la izquierda.
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritesFolder + "/Objetos/Palma_Corozo_01.png");
            Assert.IsNotNull(importer);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.AreEqual((int)SpriteAlignment.Custom, settings.spriteAlignment);
            Assert.AreEqual(36f / 64f, settings.spritePivot.x, 1e-4f);
            Assert.AreEqual(12f / 128f, settings.spritePivot.y, 1e-4f);
        }

        [Test]
        public void PixelArtUnlit_CompilesWithoutErrors()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Assert.IsNotNull(shader, $"No se encontró {ShaderPath}");

            var errors = ShaderUtil.GetShaderMessages(shader)
                .Where(message => message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                .Select(message => $"{message.message} (línea {message.line})");

            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), string.Join("\n", errors));
        }

        [TestCase("M_PixelArt_Agua", 1900, 0f)]
        [TestCase("M_PixelArt_Suelo", 1950, 0f)]
        [TestCase("M_PixelArt_Objeto", 2450, 1f)]
        public void Materials_DrawInPipelineOrder(string name, int queue, float zWrite)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{name}.mat");
            Assert.IsNotNull(material, name);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath), material.shader);
            Assert.AreEqual(queue, material.renderQueue);
            Assert.AreEqual(zWrite, material.GetFloat("_ZWrite"));
        }

        [Test]
        public void Atlas_PacksObjectAndWaterSprites()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            Assert.IsNotNull(atlas, $"No se encontró {AtlasPath}");
            Assert.AreEqual(SpritePackerMode.SpriteAtlasV2, EditorSettings.spritePackerMode);

            var expected = AssetDatabase.FindAssets("t:Sprite", new[] { SpritesFolder + "/Objetos", SpritesFolder + "/Agua" }).Length;
            Assert.AreEqual(expected, atlas.spriteCount);

            // Empaquetado: los sprites que entrega el atlas comparten una página, no su textura original.
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            var palm = atlas.GetSprite("Palma_Corozo_01");
            var house = atlas.GetSprite("Casa_Bajareque_01");
            Assert.IsNotNull(palm);
            Assert.IsNotNull(house);
            Assert.AreSame(palm.texture, house.texture, "El atlas no se empaquetó");
            Assert.Greater(palm.texture.width, 128);
            Assert.LessOrEqual(Mathf.Max(palm.texture.width, palm.texture.height), 4096);
            Assert.AreEqual(FilterMode.Point, palm.texture.filterMode);
        }

        [Test]
        public void CityData_IsExported()
        {
            var data = AssetDatabase.LoadAssetAtPath<TextAsset>(CityDataPath);
            Assert.IsNotNull(data, $"No se encontró {CityDataPath}");
            StringAssert.Contains("\"objetos\"", data.text);
            StringAssert.Contains("\"suelo\"", data.text);
        }
    }
}
