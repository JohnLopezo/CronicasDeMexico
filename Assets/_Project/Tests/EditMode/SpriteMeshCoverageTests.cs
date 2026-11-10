using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    /// <summary>
    /// Ningún píxel visible queda fuera de la malla con la que Unity dibuja el sprite. Una malla ajustada (Tight) sin
    /// margen corta en diagonal las esquinas y las puntas de un píxel, y en el juego los personajes se ven «mordidos»
    /// aunque el PNG esté completo (reportado por el autor el 2026-11-10).
    /// </summary>
    public class SpriteMeshCoverageTests
    {
        const string Root = "Assets/_Project/Art/Environment/Olmeca/Sprites";

        [TestCase("Guerreros")]
        [TestCase("Npc")]
        [TestCase("Enemigos")]
        [TestCase("Jefes")]
        [TestCase("Objetos")]
        public void EveryVisiblePixel_IsInsideTheSpriteMesh(string folder)
        {
            var bitten = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { $"{Root}/{folder}" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var missing = PixelsOutsideMesh(sprite, path);
                if (missing > 0)
                    bitten.Add($"{sprite.name} ({missing} px)");
            }
            Assert.IsEmpty(bitten, $"Sprites con píxeles fuera de su malla en {folder}: {string.Join(", ", bitten.Take(15))}");
        }

        static int PixelsOutsideMesh(Sprite sprite, string path)
        {
            // La textura importada no es legible: se lee el PNG tal cual (mismos píxeles, mismo origen abajo a la izquierda).
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            var rect = sprite.rect;
            var vertices = sprite.vertices.Select(v => v * sprite.pixelsPerUnit + sprite.pivot).ToArray();
            var triangles = sprite.triangles;
            var missing = 0;
            for (var y = 0; y < (int)rect.height; y++)
            for (var x = 0; x < (int)rect.width; x++)
            {
                if (texture.GetPixel((int)rect.x + x, (int)rect.y + y).a <= 0f)
                    continue;
                // Las cuatro esquinas del píxel (algo hacia dentro): si alguna queda fuera, se ve cortado.
                foreach (var corner in new[] { new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.05f), new Vector2(0.05f, 0.95f), new Vector2(0.95f, 0.95f) })
                {
                    if (Inside(new Vector2(x, y) + corner, vertices, triangles))
                        continue;
                    missing++;
                    break;
                }
            }
            Object.DestroyImmediate(texture);
            return missing;
        }

        static bool Inside(Vector2 p, Vector2[] v, ushort[] t)
        {
            for (var i = 0; i < t.Length; i += 3)
            {
                Vector2 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                var d1 = Cross(p, a, b);
                var d2 = Cross(p, b, c);
                var d3 = Cross(p, c, a);
                var negative = d1 < 0 || d2 < 0 || d3 < 0;
                var positive = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(negative && positive))
                    return true;
            }
            return false;
        }

        static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }
}
