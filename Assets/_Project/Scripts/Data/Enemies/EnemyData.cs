using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Rewards;
using UnityEngine;

namespace CronicasDeMexico.Data.Enemies
{
    /// <summary>Cómo se acerca al objetivo (Docs/Enemigos/Olmeca.md).</summary>
    public enum EnemyMovement
    {
        /// <summary>En línea recta.</summary>
        Chase,
        /// <summary>Persigue serpenteando de lado a lado (serpiente).</summary>
        Zigzag,
        /// <summary>Guarda la distancia y ataca desde lejos (mono).</summary>
        KeepDistance,
        /// <summary>Espera quieto hasta tener al objetivo muy cerca y no lo sigue lejos (cocodrilo).</summary>
        Ambush,
        /// <summary>Persigue cambiando de rumbo a saltos (murciélago, avispa).</summary>
        Erratic,
    }

    /// <summary>Forma del ataque normal, avisada en el suelo con TelegraphMarker.</summary>
    public enum EnemyAttackShape
    {
        /// <summary>Círculo delante del enemigo (mordida, zarpazo).</summary>
        Circle,
        /// <summary>Franja hacia el objetivo, que recorre embistiendo (embestida, picada).</summary>
        Line,
        /// <summary>Proyectil hacia el objetivo (piedra), con un destello de aviso.</summary>
        Projectile,
    }

    /// <summary>Defensa propia de cada enemigo.</summary>
    public enum EnemyDefense
    {
        None,
        /// <summary>Tras un golpe, se cubre unos segundos y recibe menos daño (serpiente enroscada).</summary>
        GuardAfterHit,
        /// <summary>Recibe menos daño de frente (escamas del cocodrilo).</summary>
        FrontArmor,
        /// <summary>Tras un golpe, se desvanece: no recibe daño y reaparece junto al objetivo (espíritu).</summary>
        Phase,
        /// <summary>Salta hacia atrás si el objetivo se le pega (mono).</summary>
        LeapBack,
        /// <summary>Tras un golpe, se eleva fuera de alcance (guacamaya).</summary>
        FlyHigh,
        /// <summary>Tras un golpe, se aparta de lado (avispa, murciélago).</summary>
        Dodge,
    }

    /// <summary>Habilidad especial, con su propio aviso y su propia espera.</summary>
    public enum EnemySpecial
    {
        None,
        /// <summary>Escupitajo: proyectil que, si alcanza, frena al objetivo (serpiente).</summary>
        SlowSpit,
        /// <summary>Coletazo: círculo alrededor de sí mismo cuando lo tienen cerca (cocodrilo).</summary>
        TailSweep,
        /// <summary>Dos zarpazos seguidos en círculo delante (espíritu jaguar).</summary>
        DoubleSwipe,
        /// <summary>Tres proyectiles en abanico (mono).</summary>
        FanThrow,
        /// <summary>Grito en anillo alrededor de sí mismo que frena y daña poco (guacamaya, murciélago).</summary>
        SlowScreech,
    }

    /// <summary>
    /// Datos de diseño de un enemigo común: vida, movimiento, ataque, defensa, botín y animaciones. Los lee WildEnemy.
    /// Los crea EnemyCharacterAssets solo si faltan; después se editan en el Inspector. Escala ×10 del M5: el jugador
    /// tiene 50 de vida y hace 10 de daño.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Enemigo", fileName = "ED_Enemigo")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Identidad")]
        public string id;
        public string displayName;

        [Header("Vida y movimiento")]
        [Min(1)] public int maxHealth = 30;
        [Min(0f)] public float moveSpeed = 2.4f;
        [Min(0f)] public float detectionRadius = 5f;
        [Min(0f)] public float loseRadius = 8f;
        public EnemyMovement movement;
        [Min(0f), Tooltip("KeepDistance: distancia que intenta guardar.")] public float preferredDistance = 4f;
        [Tooltip("Vuela: el sprite va a esta altura sobre el suelo y no lo frena el agua.")] public bool flies;
        [Min(0f)] public float hoverHeight = 0.9f;
        [Min(0.1f), Tooltip("Radio de la cápsula: cuerpo largo (cocodrilo) o pequeño (avispa).")] public float bodyRadius = 0.3f;

        [Header("Ataque normal")]
        public EnemyAttackShape attackShape;
        [Min(0f), Tooltip("Distancia a la que empieza el ataque.")] public float attackRange = 1.1f;
        [Min(0f), Tooltip("Radio del círculo, ancho de la franja o radio de impacto del proyectil.")] public float attackSize = 0.9f;
        [Min(0f), Tooltip("Franja: metros que recorre. Proyectil: velocidad en m/s.")] public float attackReach = 3f;
        [Min(0f), Tooltip("Segundos de aviso antes del golpe.")] public float attackWindup = 0.5f;
        [Min(0f), Tooltip("Segundos quieto tras golpear: el momento de atacarlo.")] public float attackRecover = 0.6f;
        [Min(0f)] public float attackCooldown = 1.4f;
        [Min(1)] public int attackDamage = 8;

        [Tooltip("La mordida le devuelve la vida que quita (murciélago).")] public bool attackDrains;
        [Tooltip("Enjambre: cuando uno ataca, los de su grupo cercanos atacan a la vez (avispa).")] public bool swarm;
        [Min(0f)] public float swarmRadius = 4f;

        [Header("Habilidad especial")]
        public EnemySpecial special;
        [Min(0f), Tooltip("Distancia a la que la usa.")] public float specialRange = 2f;
        [Min(0f), Tooltip("Radio del círculo o del anillo, o de impacto del proyectil.")] public float specialRadius = 1.5f;
        [Min(0f)] public float specialWindup = 0.6f;
        [Min(0f)] public float specialCooldown = 5f;
        [Min(1)] public int specialDamage = 5;
        [Range(0.1f, 1f), Tooltip("SlowSpit, SlowScreech: velocidad del objetivo mientras dura el freno.")]
        public float slowFactor = 0.5f;
        [Min(0f)] public float slowSeconds = 2f;

        [Header("Defensa")]
        public EnemyDefense defense;
        [Range(0f, 1f), Tooltip("Parte del daño que recibe mientras se defiende (GuardAfterHit, FrontArmor).")]
        public float defenseFactor = 0.5f;
        [Min(0f), Tooltip("Segundos que dura la defensa.")] public float defenseSeconds = 1.2f;
        [Min(0f)] public float defenseCooldown = 3f;

        [Header("Al caer")]
        public LootTable loot;
        [Min(0), Tooltip("Nivel de la zona para la experiencia (0 = sin reducción).")] public int zoneLevel = 1;
        [Min(0f), Tooltip("Segundos hasta reaparecer en su sitio (0 = no reaparece: mazmorra).")] public float respawnDelay = 20f;

        [Header("Animaciones (las crea EnemyCharacterAssets)")]
        public DirectionalSpriteSet walk;
        public DirectionalSpriteSet run;
        public DirectionalSpriteSet attack;
        public DirectionalSpriteSet guard;
        public DirectionalSpriteSet hit;
        public DirectionalSpriteSet death;
    }
}
