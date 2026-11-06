using System.Collections.Generic;
using CronicasDeMexico.Core.Progress;
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
        const string CatalogFolder = "Assets/_Project/Resources/Personajes";
        public const string DefaultGuerrero = "Balam";

        // Una animación de una vez dura lo mismo aunque la ficha traiga menos cuadros (Naira y Ameyali atacan con uno).
        const float AttackSeconds = 0.29f;
        const float WalkFramesPerSecond = 8f;
        const float RunFramesPerSecond = 12f;
        const float HitFramesPerSecond = 5f;
        const float DeathFramesPerSecond = 5f;
        const float SpecialFramesPerSecond = 1f / 0.7f; // un cuadro, 0,7 s: se queda quieto mientras dura

        // Balam conserva el id "jaguar": es el de las partidas guardadas y el de OBJ_marca_jaguar. La afinidad inicial es la de
        // su ficha (decisión del autor, 2026-11-06). Atributos: mismo presupuesto para todos, repartido según el estilo de la
        // biblia (Docs/Sistemas/Sistema_Progresion.md; lo vigila GuerreroAssetsTests). Yaret es el perfil de referencia.
        static readonly (string name, string id, string title, string description, Affinity affinity, SpeciesStatus status, CharacterStats stats, Vector4 growth)[] Guerreros =
        {
            ("Balam", "jaguar", "Guardián Jaguar", "Fuerza, movilidad y combate cercano. Habilidad (F): espíritu del jaguar.", Affinity.Tierra, SpeciesStatus.Nativa,
                new CharacterStats(50, 12, 0, 80), new Vector4(4.5f, 2f, 0.5f, 3f)),
            ("Xali", "ajolote", "Guardiana Ajolote", "Recuperación, resistencia y afinidad acuática.", Affinity.Agua, SpeciesStatus.Endemica,
                new CharacterStats(65, 8, 1, 80), new Vector4(8.5f, 1f, 0.5f, 5f)),
            ("Naira", "lobo", "Guardiana Lobo Mexicano", "Persecución, velocidad y combate sostenido.", Affinity.Viento, SpeciesStatus.Nativa,
                new CharacterStats(60, 11, 0, 70), new Vector4(7f, 1.5f, 0.5f, 3f)),
            ("Ameyali", "mariposa", "Guardiana Mariposa Monarca", "Velocidad, evasión y poderes de luz.", Affinity.Luz, SpeciesStatus.Nativa,
                new CharacterStats(45, 9, 0, 120), new Vector4(5f, 1.5f, 0.5f, 7f)),
            ("Itzel", "aguila", "Guardiana Águila Real", "Percepción, precisión y ataques a distancia.", Affinity.Luz, SpeciesStatus.Emblematica,
                new CharacterStats(40, 13, 0, 90), new Vector4(4f, 2f, 0.5f, 4f)),
            ("Yaret", "quetzal", "Guardián Quetzal", "Agilidad, distancia y movilidad aérea.", Affinity.Viento, SpeciesStatus.Nativa,
                new CharacterStats(50, 10, 0, 100), new Vector4(6f, 1.5f, 0.5f, 5f)),
            ("Ocel", "xolo", "Guardián Xoloitzcuintle", "Resistencia espiritual, rastreo y protección.", Affinity.Oscuridad, SpeciesStatus.Nativa,
                new CharacterStats(55, 8, 2, 90), new Vector4(6.5f, 1f, 1f, 4f)),
        };

        public static bool SpritesExist() => LoadSprite(DefaultGuerrero, "Caminar1", "03") != null;

        /// <summary>Crea o actualiza los GuardianData de todos los guerreros, por nombre.</summary>
        public static Dictionary<string, GuardianData> BuildAll()
        {
            var guardians = new Dictionary<string, GuardianData>();
            var ordered = new List<GuardianData>();
            foreach (var (name, id, title, description, affinity, status, stats, growth) in Guerreros)
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
                guardian.Configure(name, title, description, affinity, status, stats, growth, walk, attack, run, hit, death);
                guardian.ConfigureDefenseVictorySpecial(OptionalSet(name, "Defensa", 1f), OptionalSet(name, "Victoria", 1f), OptionalSet(name, "Especial", SpecialFramesPerSecond));
                EditorUtility.SetDirty(guardian);
                guardians[name] = guardian;
                ordered.Add(guardian);
            }

            OlmecaPrototypeGenerator.EnsureFolder(CatalogFolder);
            var catalog = OlmecaPrototypeGenerator.LoadOrCreateAsset<GuardianCatalog>($"Assets/_Project/Resources/{GuardianCatalog.ResourcePath}.asset");
            catalog.Guardians = ordered.ToArray();
            EditorUtility.SetDirty(catalog);
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

        // Una pose fija. Hoy solo la tiene el piloto, Balam (Docs/Personajes/Auditoria_Animaciones.md).
        static DirectionalSpriteSet OptionalSet(string name, string animation, float framesPerSecond) =>
            LoadSprite(name, $"{animation}1", "03") != null ? BuildSet(name, animation, framesPerSecond, out _) : null;

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
