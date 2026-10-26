using System.IO;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Dibuja por código el sprite provisional del jugador (32×48 px) y lo importa como sprite pixel art.
    /// Se sustituirá por el arte definitivo; mientras tanto permite probar escala, luz, sombras y volteo.
    /// </summary>
    public static class PlaceholderSpriteFactory
    {
        public const int Width = 32;
        public const int Height = 48;

        static readonly Color32 Clear = new(0, 0, 0, 0);
        static readonly Color32 Outline = new(28, 20, 16, 255);
        static readonly Color32 Skin = new(141, 94, 64, 255);
        static readonly Color32 Jade = new(52, 138, 96, 255);
        static readonly Color32 Cloth = new(226, 212, 176, 255);
        static readonly Color32 Sash = new(190, 112, 44, 255);
        static readonly Color32 Sandal = new(96, 64, 42, 255);

        /// <summary>Crea o sobrescribe el PNG y devuelve el sprite importado (pivote en los pies).</summary>
        public static Sprite CreateOrUpdate(string assetPath, int pixelsPerUnit)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(DrawCharacter());
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            File.WriteAllBytes(fullPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spriteMeshType = SpriteMeshType.FullRect; // un quad: 4 vértices y sombra limpia
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // Figura de frente con casco de jade (como las cabezas colosales); el ojo mira a la derecha
        // para que se note el volteo.
        static Color32[] DrawCharacter()
        {
            var pixels = new Color32[Width * Height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Clear;

            void Fill(int x0, int y0, int x1, int y1, Color32 color)
            {
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    pixels[y * Width + x] = color;
            }

            // Piernas y sandalias
            Fill(11, 1, 14, 12, Skin);
            Fill(17, 1, 20, 12, Skin);
            Fill(10, 1, 14, 2, Sandal);
            Fill(17, 1, 21, 2, Sandal);

            // Torso, faja y brazos
            Fill(10, 13, 21, 27, Cloth);
            Fill(10, 18, 21, 20, Sash);
            Fill(7, 16, 9, 27, Skin);
            Fill(22, 16, 24, 27, Skin);
            Fill(14, 28, 17, 29, Skin);

            // Cabeza con casco
            const int headX = 16, headY = 36, headRadius = 7;
            for (var y = headY - headRadius; y <= headY + headRadius; y++)
            for (var x = headX - headRadius; x <= headX + headRadius; x++)
            {
                var dx = x - headX;
                var dy = y - headY;
                if (dx * dx + dy * dy <= headRadius * headRadius)
                    pixels[y * Width + x] = y >= headY + 3 ? Jade : Skin;
            }

            pixels[35 * Width + 20] = Outline; // ojo
            pixels[34 * Width + 10] = Jade;    // orejera
            pixels[33 * Width + 10] = Jade;

            return AddOutline(pixels);
        }

        // Contorno de 1 px alrededor de la silueta para que se lea sobre cualquier fondo.
        static Color32[] AddOutline(Color32[] source)
        {
            var result = (Color32[])source.Clone();
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                if (source[y * Width + x].a != 0)
                    continue;

                if (IsOpaque(source, x - 1, y) || IsOpaque(source, x + 1, y) ||
                    IsOpaque(source, x, y - 1) || IsOpaque(source, x, y + 1))
                    result[y * Width + x] = Outline;
            }

            return result;
        }

        static bool IsOpaque(Color32[] pixels, int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height && pixels[y * Width + x].a != 0;
    }
}
