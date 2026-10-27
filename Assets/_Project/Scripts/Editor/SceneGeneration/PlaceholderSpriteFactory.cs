using System.IO;
using System.Linq;
using CronicasDeMexico.Data.Characters;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Dibuja por código la hoja de sprites provisional del jugador y la importa como pixel art.
    /// Cinco direcciones (filas) × cinco cuadros de 32×48 px (columnas): pose quieta y cuatro pasos de caminata.
    /// Se sustituirá por el arte definitivo; mientras tanto permite probar escala, luz, sombras y direcciones.
    /// </summary>
    public static class PlaceholderSpriteFactory
    {
        public const int Width = 32;
        public const int Height = 48;
        public const int FramesPerDirection = 5; // 0 = quieto, 1-4 = caminata

        // Mismo orden que DirectionalSpriteSet.
        static readonly string[] DirectionNames = { "N", "NE", "E", "SE", "S" };
        static readonly string[] FrameNames = { "Quieto", "Paso1", "Paso2", "Paso3", "Paso4" };

        const int North = 0, NorthEast = 1, East = 2, SouthEast = 3, South = 4;

        static readonly Color32 Clear = new(0, 0, 0, 0);
        static readonly Color32 Outline = new(28, 20, 16, 255);
        static readonly Color32 Skin = new(141, 94, 64, 255);
        static readonly Color32 Jade = new(52, 138, 96, 255);
        static readonly Color32 Cloth = new(226, 212, 176, 255);
        static readonly Color32 Sash = new(190, 112, 44, 255);
        static readonly Color32 Sandal = new(96, 64, 42, 255);

        /// <summary>
        /// Crea o sobrescribe el PNG y devuelve los sprites importados como [dirección][cuadro], con el pivote en
        /// los pies. Conserva el identificador de cada sprite para que las referencias existentes sigan valiendo.
        /// </summary>
        public static Sprite[][] CreateOrUpdate(string assetPath, int pixelsPerUnit)
        {
            const int directionCount = DirectionalSpriteSet.DirectionCount;
            var sheetWidth = Width * FramesPerDirection;
            var sheetHeight = Height * directionCount;

            var texture = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false);
            for (var direction = 0; direction < directionCount; direction++)
            for (var frame = 0; frame < FramesPerDirection; frame++)
                texture.SetPixels32(frame * Width, RowY(direction), Width, Height, DrawFrame(direction, frame));
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            File.WriteAllBytes(fullPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // un quad: 4 vértices y sombra limpia
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport(); // el proveedor de rectángulos debe leer el modo Multiple ya aplicado

            WriteSpriteRects(importer, directionCount);
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var result = new Sprite[directionCount][];
            for (var direction = 0; direction < directionCount; direction++)
            {
                result[direction] = new Sprite[FramesPerDirection];
                for (var frame = 0; frame < FramesPerDirection; frame++)
                    result[direction][frame] = sprites[SpriteName(direction, frame)];
            }

            return result;
        }

        static string SpriteName(int direction, int frame) => $"Jugador_{DirectionNames[direction]}_{FrameNames[frame]}";

        // El Norte queda en la fila superior de la imagen (en la textura, y = 0 es la fila inferior).
        static int RowY(int direction) => (DirectionalSpriteSet.DirectionCount - 1 - direction) * Height;

        static void WriteSpriteRects(TextureImporter importer, int directionCount)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var existingIds = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);

            var rects = new SpriteRect[directionCount * FramesPerDirection];
            for (var direction = 0; direction < directionCount; direction++)
            for (var frame = 0; frame < FramesPerDirection; frame++)
            {
                var name = SpriteName(direction, frame);
                rects[direction * FramesPerDirection + frame] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(frame * Width, RowY(direction), Width, Height),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = existingIds.TryGetValue(name, out var id) ? id : GUID.Generate()
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
        }

        // Figura con casco de jade (como las cabezas colosales). Cada dirección cambia la posición de cabeza,
        // ojos y orejeras; la caminata levanta un pie y balancea los brazos.
        static Color32[] DrawFrame(int direction, int frame)
        {
            var pixels = new Color32[Width * Height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Clear;

            // Los pasos 2 y 4 son los de apoyo: el cuerpo sube 1 px.
            var bob = frame is 2 or 4 ? 1 : 0;
            var leftLift = frame == 1 ? 2 : 0;
            var rightLift = frame == 3 ? 2 : 0;
            var armSwing = frame == 1 ? 1 : frame == 3 ? -1 : 0;

            void Fill(int x0, int y0, int x1, int y1, Color32 color)
            {
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    pixels[y * Width + x] = color;
            }

            void Pixel(int x, int y, Color32 color) => pixels[y * Width + x] = color;

            var profile = direction == East;
            var threeQuarter = direction is NorthEast or SouthEast;
            var back = direction is North or NorthEast;

            // Piernas y sandalias: de perfil quedan casi superpuestas.
            int leftLegX = profile ? 13 : 11, rightLegX = profile ? 16 : 17;
            Fill(leftLegX, 1 + leftLift, leftLegX + 3, 12, Skin);
            Fill(rightLegX, 1 + rightLift, rightLegX + 3, 12, Skin);
            Fill(leftLegX - (profile ? 0 : 1), 1 + leftLift, leftLegX + 3 + (profile ? 1 : 0), 2 + leftLift, Sandal);
            Fill(rightLegX, 1 + rightLift, rightLegX + 4, 2 + rightLift, Sandal);

            // Torso y faja
            int torsoX0 = profile ? 12 : threeQuarter ? 11 : 10, torsoX1 = profile ? 19 : 21;
            Fill(torsoX0, 13 + bob, torsoX1, 27 + bob, Cloth);
            Fill(torsoX0, 18 + bob, torsoX1, 20 + bob, Sash);

            // Brazos: de perfil se ve uno solo, por delante del torso.
            if (profile)
            {
                Fill(15, 16 + bob + armSwing, 17, 26 + bob + armSwing, Skin);
            }
            else
            {
                Fill(torsoX0 - 3, 16 + bob + armSwing, torsoX0 - 1, 27 + bob + armSwing, Skin);
                Fill(22, 16 + bob - armSwing, 24, 27 + bob - armSwing, Skin);
            }

            Fill(14, 28 + bob, 17, 29 + bob, Skin); // cuello

            // Cabeza con casco: de espaldas el casco cubre casi toda la cabeza.
            int headX = profile || threeQuarter ? 17 : 16, headY = 36 + bob;
            const int headRadius = 7;
            var helmetFrom = direction == North ? headY - 4 : back ? headY - 1 : headY + 3;
            for (var y = headY - headRadius; y <= headY + headRadius; y++)
            for (var x = headX - headRadius; x <= headX + headRadius; x++)
            {
                var dx = x - headX;
                var dy = y - headY;
                if (dx * dx + dy * dy <= headRadius * headRadius)
                    Pixel(x, y, y >= helmetFrom ? Jade : Skin);
            }

            // Ojos (solo de frente o de lado) y orejeras
            var eyeY = headY - 1;
            switch (direction)
            {
                case South:
                    Pixel(13, eyeY, Outline);
                    Pixel(19, eyeY, Outline);
                    Fill(10, headY - 3, 10, headY - 2, Jade);
                    Fill(22, headY - 3, 22, headY - 2, Jade);
                    break;
                case SouthEast:
                    Pixel(16, eyeY, Outline);
                    Pixel(21, eyeY, Outline);
                    Fill(11, headY - 3, 11, headY - 2, Jade);
                    break;
                case East:
                    Pixel(22, eyeY, Outline);
                    Fill(15, headY - 3, 16, headY - 2, Jade);
                    break;
                case NorthEast:
                    Fill(22, headY - 3, 22, headY - 2, Jade);
                    break;
                case North:
                    Fill(10, headY - 3, 10, headY - 2, Jade);
                    Fill(22, headY - 3, 22, headY - 2, Jade);
                    break;
            }

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
