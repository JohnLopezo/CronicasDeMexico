using System.Collections.Generic;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Editor.PixelArt;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Guerreros jugables: un GuardianData por guerrero con sus juegos de sprites, recortados de las fichas de
    /// Docs/Guerreros (Tools/ArtPipeline/recortar_guerreros.py). Las fichas traen Norte, Este y Sur; las diagonales
    /// reutilizan el Este, y lo que solo existe de lado (carrera, ataque, daño y muerte) se usa en todas las direcciones.
    /// </summary>
    public static class GuerreroCharacterAssets
    {
        const string DataFolder = "Assets/_Project/Data/Characters";
        public const string DefaultGuerrero = "Balam";

        // Una animación de una vez dura lo mismo aunque la ficha traiga menos cuadros (Naira y Ameyali atacan con uno).
        const float AttackSeconds = 0.29f;
        const float WalkFramesPerSecond = 8f;
        const float RunFramesPerSecond = 12f;
        const float HitFramesPerSecond = 5f;
        const float DeathFramesPerSecond = 5f;

        // Balam conserva el id "jaguar": es el de las partidas guardadas y el de OBJ_marca_jaguar. Afinidad y estado salen
        // de Docs/Personajes/Protagonista.md; donde la ficha dice otra cosa (Naira, Itzel) falta que decida el autor.
        static readonly (string name, string id, string description, Affinity affinity, SpeciesStatus status)[] Guerreros =
        {
            ("Balam", "jaguar", "Fuerza, movilidad y combate cercano.", Affinity.Oscuridad, SpeciesStatus.Nativa),
            ("Xali", "ajolote", "Recuperación, resistencia y afinidad acuática.", Affinity.Agua, SpeciesStatus.Endemica),
            ("Naira", "lobo", "Persecución, velocidad y combate sostenido.", Affinity.Tierra, SpeciesStatus.Nativa),
            ("Ameyali", "mariposa", "Velocidad, evasión y poderes de luz.", Affinity.Luz, SpeciesStatus.Nativa),
            ("Itzel", "aguila", "Percepción, precisión y ataques a distancia.", Affinity.Viento, SpeciesStatus.Emblematica),
            ("Yaret", "quetzal", "Agilidad, distancia y movilidad aérea.", Affinity.Viento, SpeciesStatus.Nativa),
            ("Ocel", "xolo", "Resistencia espiritual, rastreo y protección.", Affinity.Oscuridad, SpeciesStatus.Nativa),
        };

        public static bool SpritesExist() => LoadSprite(DefaultGuerrero, "Caminar1", "03") != null;

        /// <summary>Crea o actualiza los GuardianData de todos los guerreros, por nombre.</summary>
        public static Dictionary<string, GuardianData> BuildAll()
        {
            var guardians = new Dictionary<string, GuardianData>();
            foreach (var (name, id, description, affinity, status) in Guerreros)
            {
                var walk = BuildSet(name, "Caminar", WalkFramesPerSecond, out _);
                var run = BuildSet(name, "Correr", RunFramesPerSecond, out _);
                var attack = BuildSet(name, "Ataque", 1f, out var attackFrames);
                attack.FramesPerSecond = attackFrames / AttackSeconds;
                var hit = BuildSet(name, "Dano", HitFramesPerSecond, out _);
                var death = BuildSet(name, "Muerte", DeathFramesPerSecond, out _);

                var path = $"{DataFolder}/GUA_{id}.asset";
                var guardian = AssetDatabase.LoadAssetAtPath<GuardianData>(path);
                if (guardian == null)
                {
                    guardian = GuardianData.Create(id, name, description, affinity, status, walk);
                    AssetDatabase.CreateAsset(guardian, path);
                }
                guardian.Configure(name, walk, attack, run, hit, death);
                EditorUtility.SetDirty(guardian);
                guardians[name] = guardian;
            }
            AssetDatabase.SaveAssets();
            return guardians;
        }

        /// <summary>Cápsula de la figura humana (1,125 m a la resolución del escenario); el radio es el que pasa entre postes.</summary>
        public static void ConfigureController(CharacterController controller)
        {
            controller.height = 1.1f;
            controller.radius = 0.24f;
            controller.center = new Vector3(0f, 0.55f, 0f);
            controller.stepOffset = 0.3f;
        }

        static Sprite LoadSprite(string name, string frame, string direction) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArtSpriteImporter.GuerrerosFolder}/Guerrero_{name}_{frame}_{direction}.png");

        static DirectionalSpriteSet BuildSet(string name, string animation, float framesPerSecond, out int frames)
        {
            var east = Clip(name, animation, "03");
            frames = east.Walk.Length;
            var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{DataFolder}/DSS_Guerrero_{name}_{animation}.asset");
            set.SetClips(new[] { Clip(name, animation, "01"), east, east, east, Clip(name, animation, "05") }); // N, NE, E, SE, S
            set.FramesPerSecond = framesPerSecond;
            EditorUtility.SetDirty(set);
            return set;
        }

        static DirectionalSpriteSet.DirectionClip Clip(string name, string animation, string direction)
        {
            var frames = Frames(name, animation, direction);
            if (frames.Length == 0)
                frames = Frames(name, animation, "03");
            var idle = LoadSprite(name, "Reposo1", direction);
            return idle != null && frames.Length > 0
                ? new DirectionalSpriteSet.DirectionClip(idle, frames)
                : throw new System.InvalidOperationException($"Faltan sprites de '{name}' ({animation}, {direction}): ejecuta recortar_guerreros.py.");
        }

        static Sprite[] Frames(string name, string animation, string direction)
        {
            var frames = new List<Sprite>();
            for (var n = 1; LoadSprite(name, $"{animation}{n}", direction) is { } sprite; n++)
                frames.Add(sprite);
            return frames.ToArray();
        }
    }
}
