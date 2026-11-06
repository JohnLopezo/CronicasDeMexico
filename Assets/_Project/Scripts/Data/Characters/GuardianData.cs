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
    /// Guerrero que el jugador elige como Guardián, ligado a un animal. Su id es el que se guarda (SaveData.guardianId).
    /// Se elige en Nueva partida (StartMenuController) y se puede cambiar en la pestaña Guardián del menú (GameMenuController).
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Personajes/Guardián", fileName = "GUA_")]
    public sealed class GuardianData : ScriptableObject, IStatProfile
    {
        // Atributos por nivel (Docs/Sistemas/Sistema_Progresion.md): cada animal tiene su base y su crecimiento, así no
        // terminan todos iguales (GuerreroCharacterAssets los reparte con el mismo presupuesto).
        [SerializeField, Tooltip("Atributos en el nivel 1.")]
        CharacterStats baseStats = new(50, 10, 0, 100);

        [SerializeField, Tooltip("Lo que gana por nivel (vida, ataque, defensa, energía); se redondea hacia abajo.")]
        Vector4 growthPerLevel = new(6f, 1.5f, 0.5f, 5f);

        [SerializeField, Tooltip("Identificador estable, p. ej. \"jaguar\".")]
        string id;

        [SerializeField] string displayName;

        [SerializeField, Tooltip("Como lo presenta su ficha, p. ej. \"Guardián Jaguar\".")]
        string title;

        [SerializeField, TextArea(2, 4), Tooltip("Estilo de juego, como lo describe la biblia.")]
        string description;

        [SerializeField] Affinity initialAffinity;

        [SerializeField] SpeciesStatus status;

        [SerializeField, Tooltip("Sprites del personaje: reposo y caminata.")]
        DirectionalSpriteSet spriteSet;

        [SerializeField] DirectionalSpriteSet attackSet;
        [SerializeField] DirectionalSpriteSet runSet;
        [SerializeField] DirectionalSpriteSet hitSet;
        [SerializeField] DirectionalSpriteSet deathSet;
        [SerializeField, Tooltip("Vacío hasta que su arte esté: sin pose, defiende igual.")] DirectionalSpriteSet defenseSet;
        [SerializeField, Tooltip("Vacío hasta que su arte esté: pasa directo a los resultados.")] DirectionalSpriteSet victorySet;
        [SerializeField, Tooltip("Vacío hasta que su arte esté: aún no tiene habilidad especial.")] DirectionalSpriteSet specialSet;

        public DirectionalSpriteSet AttackSet => attackSet;
        public DirectionalSpriteSet RunSet => runSet;
        public DirectionalSpriteSet HitSet => hitSet;
        public DirectionalSpriteSet DeathSet => deathSet;
        public DirectionalSpriteSet DefenseSet => defenseSet;
        public DirectionalSpriteSet VictorySet => victorySet;
        public DirectionalSpriteSet SpecialSet => specialSet;

        public string Id => id;
        public string DisplayName => displayName;
        public string Title => title;
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

        public void Configure(string name, string guardianTitle, string guardianDescription, Affinity affinity, SpeciesStatus speciesStatus,
            CharacterStats stats, Vector4 growth, DirectionalSpriteSet walk, DirectionalSpriteSet attack, DirectionalSpriteSet run,
            DirectionalSpriteSet hit, DirectionalSpriteSet death)
        {
            displayName = name;
            title = guardianTitle;
            description = guardianDescription;
            initialAffinity = affinity;
            status = speciesStatus;
            baseStats = stats;
            growthPerLevel = growth;
            spriteSet = walk;
            attackSet = attack;
            runSet = run;
            hitSet = hit;
            deathSet = death;
        }

        public void ConfigureDefenseVictorySpecial(DirectionalSpriteSet defense, DirectionalSpriteSet victory, DirectionalSpriteSet special)
        {
            defenseSet = defense;
            victorySet = victory;
            specialSet = special;
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
