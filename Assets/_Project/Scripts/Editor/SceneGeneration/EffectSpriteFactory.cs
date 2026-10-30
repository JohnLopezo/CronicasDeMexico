using System.IO;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Sprites de efectos dibujados por código e importados como pixel art (32 px por unidad, filtro Point): la marca de
    /// aviso de los ataques (un círculo de 2 m de diámetro, blanco para teñirlo con SpriteRenderer.color) y el fragmento
    /// de símbolo que dispara el eco borrado. Sin semitransparencias: el material recorta el alfa a 0,5, así que el
    /// interior de la marca va tramado.
    /// </summary>
    public static class EffectSpriteFactory
    {
        const string Folder = "Assets/_Project/Art/Effects";
        const int PixelsPerUnit = 32;

        static readonly Color32 Clear = new(0, 0, 0, 0);
        static readonly Color32 White = new(255, 255, 255, 255);

        public static Sprite TelegraphCircle()
        {
            const int size = 64;
            var pixels = new Color32[size * size];
            const float center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                var ring = distance <= center && distance > center - 3f;
                var dither = distance <= center - 3f && (x + y) % 2 == 0 && (x / 2 + y / 2) % 2 == 0;
                pixels[y * size + x] = ring || dither ? White : Clear;
            }
            return Save("Aviso_Circulo", size, size, pixels);
        }

        public static Sprite SymbolShard()
        {
            // Rombo de 9 px con un trazo dentro, en los violetas de la rampa Olvido.
            const int size = 9;
            var outline = new Color32(42, 24, 56, 255);
            var fill = new Color32(169, 139, 232, 255);
            var glow = new Color32(217, 204, 255, 255);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Mathf.Abs(x - 4) + Mathf.Abs(y - 4);
                pixels[y * size + x] = d > 4 ? Clear : d == 4 ? outline : (x == 4 || y == 4) && d <= 2 ? glow : fill;
            }
            return Save("Fragmento_Simbolo", size, size, pixels);
        }

        static Sprite Save(string name, int width, int height, Color32[] pixels)
        {
            OlmecaPrototypeGenerator.EnsureFolder(Folder);
            var path = $"{Folder}/{name}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, path), png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
