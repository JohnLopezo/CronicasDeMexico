using System.IO;
using System.Linq;
using CronicasDeMexico.Data.Characters;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Dibuja por código hojas de sprites provisionales de personajes y las importa como pixel art.
    /// Cinco direcciones (filas) × cinco cuadros de 32×48 px (columnas): pose quieta y cuatro pasos de caminata.
    /// Se sustituirán por el arte definitivo; mientras tanto permiten probar escala, luz, sombras y direcciones.
    /// </summary>
    public static class PlaceholderSpriteFactory
    {
        public const int Width = 32;
        public const int Height = 48;
        public const int FramesPerDirection = 5; // 0 = quieto, 1-4 = caminata

        /// <summary>Colores de un personaje provisional.</summary>
        public readonly struct CharacterPalette
        {
            public readonly Color32 Skin, Headgear, Cloth, Sash, Sandal;
            public readonly bool Spots; // manchas de jaguar en ropa y tocado

            public CharacterPalette(Color32 skin, Color32 headgear, Color32 cloth, Color32 sash, Color32 sandal, bool spots)
            {
                Skin = skin;
                Headgear = headgear;
                Cloth = cloth;
                Sash = sash;
                Sandal = sandal;
                Spots = spots;
            }
        }

        // Casco de jade, como las cabezas colosales.
        public static readonly CharacterPalette Player = new(
            new Color32(141, 94, 64, 255), new Color32(52, 138, 96, 255), new Color32(226, 212, 176, 255),
            new Color32(190, 112, 44, 255), new Color32(96, 64, 42, 255), false);

        // Tocado de algodón y faja de jade.
        public static readonly CharacterPalette Artisan = new(
            new Color32(150, 100, 68, 255), new Color32(232, 226, 210, 255), new Color32(176, 74, 52, 255),
            new Color32(52, 138, 96, 255), new Color32(96, 64, 42, 255), false);

        public static readonly CharacterPalette JaguarSpirit = new(
            new Color32(70, 52, 40, 255), new Color32(214, 160, 60, 255), new Color32(214, 160, 60, 255),
            new Color32(40, 30, 24, 255), new Color32(40, 30, 24, 255), true);

        // Mismo orden que DirectionalSpriteSet.
        static readonly string[] DirectionNames = { "N", "NE", "E", "SE", "S" };
        static readonly string[] FrameNames = { "Quieto", "Paso1", "Paso2", "Paso3", "Paso4" };

        const int North = 0, NorthEast = 1, East = 2, SouthEast = 3, South = 4;

        static readonly Color32 Clear = new(0, 0, 0, 0);
        static readonly Color32 Outline = new(28, 20, 16, 255);
        static readonly Color32 Spot = new(40, 28, 20, 255);

        /// <summary>
        /// Crea o sobrescribe el PNG y devuelve los sprites importados como [dirección][cuadro], con el pivote en
        /// los pies. Conserva el identificador de cada sprite para que las referencias existentes sigan valiendo.
        /// </summary>
        public static Sprite[][] CreateOrUpdate(string assetPath, string namePrefix, CharacterPalette palette, int pixelsPerUnit)
        {
            const int directionCount = DirectionalSpriteSet.DirectionCount;
            var sheetWidth = Width * FramesPerDirection;
            var sheetHeight = Height * directionCount;

            var texture = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false);
            for (var direction = 0; direction < directionCount; direction++)
            for (var frame = 0; frame < FramesPerDirection; frame++)
                texture.SetPixels32(frame * Width, RowY(direction), Width, Height, DrawFrame(palette, direction, frame));
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

            WriteSpriteRects(importer, namePrefix, directionCount);
            importer.SaveAndReimport();

            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var result = new Sprite[directionCount][];
            for (var direction = 0; direction < directionCount; direction++)
            {
                result[direction] = new Sprite[FramesPerDirection];
                for (var frame = 0; frame < FramesPerDirection; frame++)
                    result[direction][frame] = sprites[SpriteName(namePrefix, direction, frame)];
            }

            return result;
        }

        static string SpriteName(string prefix, int direction, int frame) => $"{prefix}_{DirectionNames[direction]}_{FrameNames[frame]}";

        // El Norte queda en la fila superior de la imagen (en la textura, y = 0 es la fila inferior).
        static int RowY(int direction) => (DirectionalSpriteSet.DirectionCount - 1 - direction) * Height;

        static void WriteSpriteRects(TextureImporter importer, string namePrefix, int directionCount)
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
                var name = SpriteName(namePrefix, direction, frame);
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

        // Figura con tocado. Cada dirección cambia la posición de cabeza, ojos y orejeras; la caminata levanta un
        // pie y balancea los brazos.
        static Color32[] DrawFrame(CharacterPalette palette, int direction, int frame)
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

            // Patrón fijo de manchas, separado para que se lean a 32 px.
            bool IsSpot(int x, int y) => palette.Spots && y % 2 == 0 && (x + 2 * y) % 5 == 0;

            var profile = direction == East;
            var threeQuarter = direction is NorthEast or SouthEast;
            var back = direction is North or NorthEast;

            // Piernas y sandalias: de perfil quedan casi superpuestas.
            int leftLegX = profile ? 13 : 11, rightLegX = profile ? 16 : 17;
            Fill(leftLegX, 1 + leftLift, leftLegX + 3, 12, palette.Skin);
            Fill(rightLegX, 1 + rightLift, rightLegX + 3, 12, palette.Skin);
            Fill(leftLegX - (profile ? 0 : 1), 1 + leftLift, leftLegX + 3 + (profile ? 1 : 0), 2 + leftLift, palette.Sandal);
            Fill(rightLegX, 1 + rightLift, rightLegX + 4, 2 + rightLift, palette.Sandal);

            // Torso y faja
            int torsoX0 = profile ? 12 : threeQuarter ? 11 : 10, torsoX1 = profile ? 19 : 21;
            for (var y = 13 + bob; y <= 27 + bob; y++)
            for (var x = torsoX0; x <= torsoX1; x++)
                Pixel(x, y, IsSpot(x, y) ? Spot : palette.Cloth);
            Fill(torsoX0, 18 + bob, torsoX1, 20 + bob, palette.Sash);

            // Brazos: de perfil se ve uno solo, por delante del torso.
            if (profile)
            {
                Fill(15, 16 + bob + armSwing, 17, 26 + bob + armSwing, palette.Skin);
            }
            else
            {
                Fill(torsoX0 - 3, 16 + bob + armSwing, torsoX0 - 1, 27 + bob + armSwing, palette.Skin);
                Fill(22, 16 + bob - armSwing, 24, 27 + bob - armSwing, palette.Skin);
            }

            Fill(14, 28 + bob, 17, 29 + bob, palette.Skin); // cuello

            // Cabeza con tocado: de espaldas el tocado cubre casi toda la cabeza.
            int headX = profile || threeQuarter ? 17 : 16, headY = 36 + bob;
            const int headRadius = 7;
            var headgearFrom = direction == North ? headY - 4 : back ? headY - 1 : headY + 3;
            for (var y = headY - headRadius; y <= headY + headRadius; y++)
            for (var x = headX - headRadius; x <= headX + headRadius; x++)
            {
                var dx = x - headX;
                var dy = y - headY;
                if (dx * dx + dy * dy > headRadius * headRadius)
                    continue;

                var color = y < headgearFrom ? palette.Skin : IsSpot(x, y) ? Spot : palette.Headgear;
                Pixel(x, y, color);
            }

            // Ojos (solo de frente o de lado) y orejeras
            var eyeY = headY - 1;
            switch (direction)
            {
                case South:
                    Pixel(13, eyeY, Outline);
                    Pixel(19, eyeY, Outline);
                    Fill(10, headY - 3, 10, headY - 2, palette.Headgear);
                    Fill(22, headY - 3, 22, headY - 2, palette.Headgear);
                    break;
                case SouthEast:
                    Pixel(16, eyeY, Outline);
                    Pixel(21, eyeY, Outline);
                    Fill(11, headY - 3, 11, headY - 2, palette.Headgear);
                    break;
                case East:
                    Pixel(22, eyeY, Outline);
                    Fill(15, headY - 3, 16, headY - 2, palette.Headgear);
                    break;
                case NorthEast:
                    Fill(22, headY - 3, 22, headY - 2, palette.Headgear);
                    break;
                case North:
                    Fill(10, headY - 3, 10, headY - 2, palette.Headgear);
                    Fill(22, headY - 3, 22, headY - 2, palette.Headgear);
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
