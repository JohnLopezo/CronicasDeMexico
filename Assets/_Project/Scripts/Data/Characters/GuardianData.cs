using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Characters
{
    /// <summary>Afinidades elementales (Docs/Historia_Mundo.md §5). Sistema de fantasía, no una clasificación histórica.</summary>
    public enum Affinity
    {
        Agua,
        Tierra,
        Fuego,
        Viento,
        Luz,
        Oscuridad,
    }

    /// <summary>Cómo se presenta la especie en el juego (Historia_Mundo §2.3; Docs/Personajes/Protagonista.md).</summary>
    public enum SpeciesStatus
    {
        Endemica,
        Nativa,
        Emblematica,
    }

    /// <summary>
    /// Forma animal que el jugador elige como Guardián. Su id es el que se guarda (SaveData.guardianId). El vertical
    /// slice solo tiene el Jaguar; la pantalla de elección se hará cuando haya más de uno.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Personajes/Guardián", fileName = "GUA_")]
    public sealed class GuardianData : ScriptableObject, IStatProfile
    {
        // Atributos por nivel (Docs/Sistemas/Sistema_Progresion.md): cada animal tiene su base y su crecimiento, así no
        // terminan todos iguales. Escala ×10 desde M5 (el Jaguar empieza con 50 de vida y 10 de ataque).
        [SerializeField, Tooltip("Atributos en el nivel 1.")]
        CharacterStats baseStats = new(50, 10, 0, 100);

        [SerializeField, Tooltip("Lo que gana por nivel (vida, ataque, defensa, energía); se redondea hacia abajo.")]
        Vector4 growthPerLevel = new(6f, 1.5f, 0.5f, 5f);

        [SerializeField, Tooltip("Identificador estable, p. ej. \"jaguar\".")]
        string id;

        [SerializeField] string displayName;

        [SerializeField, TextArea(2, 4), Tooltip("Estilo de juego, como lo describe la biblia.")]
        string description;

        [SerializeField] Affinity initialAffinity;

        [SerializeField] SpeciesStatus status;

        [SerializeField, Tooltip("Sprites del personaje (provisionales hasta el arte final).")]
        DirectionalSpriteSet spriteSet;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Affinity InitialAffinity => initialAffinity;
        public SpeciesStatus Status => status;
        public DirectionalSpriteSet SpriteSet => spriteSet;
        public string GuardianId => id;

        public CharacterStats At(int level)
        {
            var gained = Mathf.Max(0, level - 1);
            return baseStats + new CharacterStats(Mathf.FloorToInt(growthPerLevel.x * gained), Mathf.FloorToInt(growthPerLevel.y * gained),
                Mathf.FloorToInt(growthPerLevel.z * gained), Mathf.FloorToInt(growthPerLevel.w * gained));
        }

        public static GuardianData Create(string id, string displayName, string description, Affinity initialAffinity,
            SpeciesStatus status, DirectionalSpriteSet spriteSet)
        {
            var guardian = CreateInstance<GuardianData>();
            guardian.name = $"GUA_{id}";
            guardian.id = id;
            guardian.displayName = displayName;
            guardian.description = description;
            guardian.initialAffinity = initialAffinity;
            guardian.status = status;
            guardian.spriteSet = spriteSet;
            return guardian;
        }
    }
}
