using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Editor.PixelArt;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Juegos de sprites del jaguar jugable y del jaguar corrupto (etapas A y B del plan de personajes; ficha en
    /// Docs/Personajes/Fichas). Los sprites los renderiza el flujo de Blender (Tools/ArtPipeline/personajes.py) en
    /// Sprites/Personajes como &lt;Prefijo&gt;_&lt;Animación&gt;&lt;cuadro&gt;_&lt;dirección 01–05&gt;; aquí se agrupan en un
    /// DirectionalSpriteSet por animación (DSS_&lt;Prefijo&gt;_&lt;Animación&gt;), cada uno con su velocidad. Se rehacen en
    /// cada generación para seguir al arte.
    /// </summary>
    public static class JaguarCharacterAssets
    {
        const string SpritesFolder = PixelArtSpriteImporter.SpritesFolder + "/Personajes";
        const string DataFolder = "Assets/_Project/Data/Characters";
        public const string PlayerPrefix = "Jaguar";
        public const string CorruptedPrefix = "JaguarCorrupto";

        // Animación, cuadros y cuadros por segundo (igual que ANIMACIONES_* de personajes.py)
        static readonly (string anim, int frames, float fps)[] PlayerAnimations =
        {
            ("Reposo", 4, 3f), ("Caminar", 6, 10f), ("Correr", 6, 16f), ("Zarpazo", 4, 14f), ("Golpeado", 2, 10f), ("Muerte", 4, 7f),
        };
        static readonly (string anim, int frames, float fps)[] CorruptedAnimations =
        {
            ("Reposo", 4, 3f), ("Caminar", 6, 11f), ("Acecho", 2, 6f), ("Salto", 2, 6f), ("Golpeado", 2, 10f), ("Muerte", 3, 6f),
        };

        public sealed class Sets
        {
            public readonly Dictionary<string, DirectionalSpriteSet> ByAnimation = new();
            public DirectionalSpriteSet this[string anim] => ByAnimation.TryGetValue(anim, out var set) ? set : null;
            public DirectionalSpriteSet Walk => this["Caminar"];
            public DirectionalSpriteSet Idle => this["Reposo"];
        }

        /// <summary>Si el flujo de arte ya exportó los sprites del jaguar.</summary>
        public static bool SpritesExist() =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/{PlayerPrefix}_Caminar1_01.png") != null;

        public static Sets Player() => Build(PlayerPrefix, PlayerAnimations);

        public static Sets Corrupted() => Build(CorruptedPrefix, CorruptedAnimations);

        static Sets Build(string prefix, IEnumerable<(string anim, int frames, float fps)> animations)
        {
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { SpritesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(sprite => sprite != null && sprite.name.StartsWith(prefix + "_"))
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First());
            Sprite Get(string name) => sprites.TryGetValue(name, out var sprite)
                ? sprite
                : throw new System.InvalidOperationException($"Falta el sprite '{name}' en {SpritesFolder}: ejecuta el flujo de arte.");

            var sets = new Sets();
            foreach (var (anim, frames, fps) in animations)
            {
                var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{DataFolder}/DSS_{prefix}_{anim}.asset");
                set.SetClips(Enumerable.Range(1, DirectionalSpriteSet.DirectionCount)
                    .Select(d =>
                    {
                        var clip = Enumerable.Range(1, frames).Select(f => Get($"{prefix}_{anim}{f}_{d:00}")).ToArray();
                        // La pose quieta de cualquier juego es el primer cuadro del reposo
                        return new DirectionalSpriteSet.DirectionClip(Get($"{prefix}_Reposo1_{d:00}"), clip);
                    })
                    .ToArray());
                set.FramesPerSecond = fps;
                EditorUtility.SetDirty(set);
                sets.ByAnimation[anim] = set;
            }
            AssetDatabase.SaveAssets();
            return sets;
        }

        /// <summary>
        /// Colisión de cuadrúpedo: cápsula baja y algo más ancha que la del humano provisional, centrada en el cuerpo
        /// (los sprites están a la resolución del escenario, sin escalar la raíz). El radio es el medio ancho del
        /// cuerpo: así pasa entre los postes de las enramadas, como el humano provisional.
        /// </summary>
        public static void ConfigureController(CharacterController controller)
        {
            controller.height = 0.9f;
            controller.radius = 0.24f;
            controller.center = new Vector3(0f, 0.45f, 0f);
            controller.stepOffset = 0.3f;
        }
    }
}
