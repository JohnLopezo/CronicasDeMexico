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
    public sealed class GuardianData : ScriptableObject
    {
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
