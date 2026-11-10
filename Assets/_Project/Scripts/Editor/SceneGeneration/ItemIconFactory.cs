using System.IO;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Iconos de 16 × 16 px de los objetos (inventario y avisos), dibujados por código con formas simples, los colores
    /// de las rampas del flujo de arte y un contorno oscuro. Provisionales hasta el arte definitivo, como los sprites de
    /// personajes. Se guardan en Art/UI/Iconos.
    /// </summary>
    public static class ItemIconFactory
    {
        const string Folder = "Assets/_Project/Art/UI/Iconos";
        const int Size = 16;

        public enum Shape { Piedra, Lasca, Haz, Semilla, Terron, Esencia, Orbe, Bolsa, Frasco, Amuleto, Talisman, Corazon, Figurilla, Hacha, Espejo, Fragmento }

        static readonly Color32 Clear = new(0, 0, 0, 0);
        static readonly Color32 Outline = new(28, 20, 16, 255);

        /// <summary>Dibuja (o redibuja) el icono y devuelve su sprite.</summary>
        public static Sprite Create(string name, Shape shape, Color32 main, Color32 light)
        {
            var pixels = new Color32[Size * Size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Clear;

            void Put(int x, int y, Color32 c)
            {
                if (x >= 0 && x < Size && y >= 0 && y < Size)
                    pixels[y * Size + x] = c;
            }

            void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
            {
                for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                    if ((x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry) <= 1f)
                        Put(x, y, c);
            }

            void Rect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    Put(x, y, c);
            }

            void Diamond(int cx, int cy, int rx, int ry, Color32 c)
            {
                for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                    if (Mathf.Abs(x - cx) / (float)rx + Mathf.Abs(y - cy) / (float)ry <= 1f)
                        Put(x, y, c);
            }

            switch (shape)
            {
                case Shape.Piedra:
                    Ellipse(7.5f, 6.5f, 6f, 4.5f, main); Ellipse(6f, 8f, 2.5f, 1.5f, light); break;
                case Shape.Lasca:
                    Diamond(8, 8, 4, 7, main); Rect(7, 9, 7, 12, light); break;
                case Shape.Haz:
                    for (var i = 0; i < 4; i++) Rect(4 + 2 * i, 2, 4 + 2 * i, 13, i % 2 == 0 ? main : light);
                    Rect(3, 7, 12, 8, Outline); break;
                case Shape.Semilla:
                    Ellipse(5.5f, 6f, 2.5f, 3.5f, main); Ellipse(10.5f, 8f, 2.5f, 3.5f, main); Put(5, 7, light); Put(10, 9, light); break;
                case Shape.Terron:
                    Rect(3, 3, 12, 9, main); Rect(4, 8, 8, 9, light); break;
                case Shape.Esencia:
                    Ellipse(7.5f, 6f, 4f, 4f, main); Diamond(8, 11, 2, 4, main); Ellipse(6.5f, 7f, 1.5f, 1.5f, light); break;
                case Shape.Orbe:
                    Ellipse(7.5f, 7.5f, 5f, 5f, main); Ellipse(6f, 9f, 2f, 2f, light); break;
                case Shape.Bolsa:
                    Ellipse(7.5f, 6f, 5f, 4.5f, main); Rect(6, 10, 9, 12, main); Rect(5, 11, 10, 11, Outline); Put(6, 7, light); break;
                case Shape.Frasco:
                    Ellipse(7.5f, 5.5f, 4.5f, 4.5f, main); Rect(6, 9, 9, 13, main); Rect(6, 13, 9, 13, light); Put(6, 6, light); break;
                case Shape.Amuleto:
                    for (var x = 3; x <= 12; x++) Put(x, 14 - Mathf.Abs(x - 7) / 2, Outline);
                    Ellipse(7.5f, 6f, 3.5f, 4f, main); Put(6, 7, light); Put(7, 7, light); break;
                case Shape.Talisman:
                    Rect(7, 11, 8, 14, Outline); Diamond(8, 6, 3, 6, main); Rect(8, 4, 8, 8, light); break;
                case Shape.Corazon:
                    Ellipse(5.5f, 9f, 3.5f, 3.5f, main); Ellipse(10.5f, 9f, 3.5f, 3.5f, main); Diamond(8, 5, 6, 5, main);
                    Ellipse(8f, 8f, 1.5f, 1.5f, light); break;
                case Shape.Figurilla:
                    Ellipse(7.5f, 12f, 2.5f, 2.5f, main); Rect(5, 3, 10, 9, main); Rect(5, 1, 6, 3, main); Rect(9, 1, 10, 3, main); Put(7, 12, light); break;
                case Shape.Hacha:
                    Diamond(8, 8, 4, 7, main); Rect(6, 3, 9, 3, light); break;
                case Shape.Espejo:
                    Ellipse(7.5f, 8f, 5.5f, 5.5f, main); Ellipse(7.5f, 8f, 3.5f, 3.5f, light); Ellipse(6.5f, 9f, 1f, 1f, main); break;
                default:
                    Diamond(8, 8, 3, 6, main); Rect(8, 5, 8, 10, light); break;
            }

            // Contorno exterior de 1 px.
            var result = (Color32[])pixels.Clone();
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                if (pixels[y * Size + x].a != 0)
                    continue;
                bool Opaque(int px, int py) => px >= 0 && px < Size && py >= 0 && py < Size && pixels[py * Size + px].a != 0;
                if (Opaque(x - 1, y) || Opaque(x + 1, y) || Opaque(x, y - 1) || Opaque(x, y + 1))
                    result[y * Size + x] = Outline;
            }

            return Save(name, result);
        }

        static Sprite Save(string name, Color32[] pixels)
        {
            OlmecaPrototypeGenerator.EnsureFolder(Folder);
            var path = $"{Folder}/ICO_{name}.png";
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, path), png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Size;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
