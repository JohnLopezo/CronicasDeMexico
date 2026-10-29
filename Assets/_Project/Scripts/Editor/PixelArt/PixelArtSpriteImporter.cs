using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.PixelArt
{
    /// <summary>
    /// Importa como pixel art los sprites que exporta el flujo Blender (Tools/ArtPipeline/generar_olmeca.py) a
    /// <see cref="SpritesFolder"/>: 45,2548 px por unidad (rombo de 64×32 por metro, Docs/Arte_Olmeca_Demo.md §3),
    /// filtro Point, sin mipmaps ni compresión, y el pivote de cada sprite leído de pivotes_unity.json.
    /// </summary>
    public sealed class PixelArtSpriteImporter : AssetPostprocessor
    {
        public const string SpritesFolder = "Assets/_Project/Art/Environment/Olmeca/Sprites";
        public const string PivotsPath = SpritesFolder + "/pivotes_unity.json";
        public const string GroundFolder = SpritesFolder + "/Suelo";
        public const float PixelsPerUnit = 45.254834f; // 32·√2

        [Serializable]
        sealed class PivotEntry
        {
            public string nombre;
            public float[] lienzo;
            public float[] pivote;
        }

        [Serializable]
        sealed class PivotFile
        {
            public float ppu;
            public PivotEntry[] sprites;
        }

        static Dictionary<string, Vector2> pivots;
        static DateTime pivotsStamp;

        // Subir el número reimporta los sprites cuando cambian estos ajustes.
        public override uint GetVersion() => 1;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesFolder + "/", StringComparison.Ordinal))
                return;

            // Si el flujo cambia los pivotes, los sprites se reimportan.
            context.DependsOnSourceAsset(PivotsPath);

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // El suelo horneado se dibuja una vez y entero; en los objetos, la malla ajustada reduce el overdraw.
            settings.spriteMeshType = assetPath.StartsWith(GroundFolder + "/", StringComparison.Ordinal)
                ? SpriteMeshType.FullRect
                : SpriteMeshType.Tight;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;

            var spriteName = Path.GetFileNameWithoutExtension(assetPath);
            if (TryGetPivot(spriteName, out var pivot))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
            }
            else
            {
                Debug.LogWarning($"[PixelArt] '{spriteName}' no está en {PivotsPath}; se usa el pivote central.");
            }

            importer.SetTextureSettings(settings);
        }

        /// <summary>Pivote normalizado (0–1 desde abajo a la izquierda) que el flujo calculó para el sprite.</summary>
        public static bool TryGetPivot(string spriteName, out Vector2 pivot)
        {
            LoadPivots();
            return pivots.TryGetValue(spriteName, out pivot);
        }

        static void LoadPivots()
        {
            var stamp = File.Exists(PivotsPath) ? File.GetLastWriteTimeUtc(PivotsPath) : DateTime.MinValue;
            if (pivots != null && stamp == pivotsStamp)
                return;

            pivots = new Dictionary<string, Vector2>();
            pivotsStamp = stamp;
            if (stamp == DateTime.MinValue)
                return;

            var file = JsonUtility.FromJson<PivotFile>(File.ReadAllText(PivotsPath));
            foreach (var entry in file.sprites)
                pivots[entry.nombre] = new Vector2(entry.pivote[0] / entry.lienzo[0], entry.pivote[1] / entry.lienzo[1]);
        }
    }
}
