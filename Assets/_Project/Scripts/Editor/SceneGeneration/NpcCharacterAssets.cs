using System.Collections.Generic;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Editor.PixelArt;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Juego de sprites de un NPC recortado de las fichas de Docs/NPC (Tools/ArtPipeline/recortar_npc.py): reposo y
    /// caminata en tres vistas. Las diagonales reutilizan la vista Este, porque las fichas no las traen.
    /// </summary>
    public static class NpcCharacterAssets
    {
        const string DataFolder = "Assets/_Project/Data/Characters";
        const float WalkFramesPerSecond = 6f;

        public static DirectionalSpriteSet Build(string npcName)
        {
            Sprite Load(string frame, string direction) =>
                AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArtSpriteImporter.NpcFolder}/Npc_{npcName}_{frame}_{direction}.png");

            DirectionalSpriteSet.DirectionClip Clip(string direction)
            {
                var walk = new List<Sprite>();
                for (var frame = 1; Load($"Caminar{frame}", direction) is { } sprite; frame++)
                    walk.Add(sprite);
                var idle = Load("Reposo1", direction);
                return idle != null && walk.Count > 0
                    ? new DirectionalSpriteSet.DirectionClip(idle, walk.ToArray())
                    : throw new System.InvalidOperationException($"Faltan sprites de '{npcName}' ({direction}): ejecuta recortar_npc.py.");
            }

            var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{DataFolder}/DSS_Npc_{npcName}.asset");
            set.SetClips(new[] { Clip("01"), Clip("03"), Clip("03"), Clip("03"), Clip("05") }); // N, NE, E, SE, S
            set.FramesPerSecond = WalkFramesPerSecond;
            EditorUtility.SetDirty(set);
            return set;
        }

        /// <summary>
        /// Ataque de quien pelea (NpcDefender): un solo cuadro, 1/3 de segundo. shortcut: las fichas solo traen la pose de
        /// lado, así que también se usa al atacar hacia el norte o el sur; dibujarlas si llegan esas vistas.
        /// </summary>
        public static DirectionalSpriteSet BuildAttack(string npcName)
        {
            var path = $"{PixelArtSpriteImporter.NpcFolder}/Npc_{npcName}_Ataque1_03.png";
            var pose = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (pose == null)
                throw new System.InvalidOperationException($"Falta el sprite '{path}': ejecuta recortar_npc.py.");

            var clip = new DirectionalSpriteSet.DirectionClip(pose, new[] { pose });
            var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{DataFolder}/DSS_Npc_{npcName}_Ataque.asset");
            set.SetClips(new[] { clip, clip, clip, clip, clip });
            set.FramesPerSecond = 3f;
            EditorUtility.SetDirty(set);
            return set;
        }
    }
}
