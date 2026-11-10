using System;
using CronicasDeMexico.Data.Characters;
using UnityEngine;

namespace CronicasDeMexico.Data.Enemies
{
    /// <summary>Forma de una habilidad de jefe. Todas se avisan en el suelo salvo la protección y la invocación.</summary>
    public enum BossAbilityKind
    {
        /// <summary>Círculo delante (zarpazo, golpe). count &gt; 1: golpes seguidos.</summary>
        Swipe,
        /// <summary>Franja hacia el objetivo, sin moverse (aliento, rayo).</summary>
        Beam,
        /// <summary>Franja que recorre embistiendo.</summary>
        Charge,
        /// <summary>Salta (o vuela) y cae donde estaba el objetivo: círculo allí. Invulnerable en el aire.</summary>
        Leap,
        /// <summary>count círculos alrededor del objetivo (lluvia, llamas).</summary>
        Rain,
        /// <summary>Círculo grande a su alrededor (onda, terremoto); con factor &lt; 1, además frena.</summary>
        Nova,
        /// <summary>count proyectiles en un abanico de spread grados (360: en todas direcciones).</summary>
        Volley,
        /// <summary>Hace aparecer count enemigos de su reserva a su alrededor.</summary>
        Summon,
        /// <summary>Protección durante seconds: recibe factor del daño (0 = invulnerable).</summary>
        Shield,
    }

    [Serializable]
    public sealed class BossAbility
    {
        public string name;
        public BossAbilityKind kind;
        [Min(1), Tooltip("Fase desde la que la usa (1, 2 o 3).")] public int minPhase = 1;
        [Min(0f), Tooltip("Distancia al objetivo a la que la usa.")] public float range = 3f;
        [Min(0f), Tooltip("Radio del círculo, ancho de la franja o radio de impacto.")] public float radius = 1.2f;
        [Min(0f), Tooltip("Franja: largo. Proyectil: velocidad en m/s.")] public float length = 5f;
        [Min(1)] public int count = 1;
        [Min(0f), Tooltip("Abanico de proyectiles, en grados.")] public float spread = 60f;
        [Min(0f)] public float windup = 0.8f;
        [Min(0f)] public float cooldown = 5f;
        [Min(0)] public int damage = 10;
        [Range(0f, 1f), Tooltip("Nova: velocidad del objetivo mientras lo frena. Shield: parte del daño que recibe.")]
        public float factor = 1f;
        [Min(0f), Tooltip("Nova: segundos de freno. Shield y Leap: segundos que dura.")] public float seconds = 2f;
    }

    /// <summary>
    /// Jefe de la arena de pruebas (TrialBoss): ecos ficticios inspirados en figuras de otras culturas, no dioses
    /// documentados (GDD §3, Docs/Enemigos/Jefes_Prueba.md). Los crea BossCharacterAssets solo si faltan; después se
    /// editan en el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Jefe de prueba", fileName = "BD_Jefe")]
    public sealed class BossData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string inspiration;

        [Min(1)] public int maxHealth = 300;
        [Min(0f)] public float moveSpeed = 2f;
        [Min(0f), Tooltip("Distancia a la que despierta.")] public float engageRadius = 7f;
        [Min(0.1f)] public float bodyRadius = 0.6f;
        [Tooltip("Vida (fracción) bajo la que pasa a la fase 2 y a la fase 3.")]
        public float[] phaseThresholds = { 0.6f, 0.3f };
        [Range(0f, 1f), Tooltip("Al caer por primera vez, vuelve con esta parte de la vida (0 = no).")]
        public float resurrectFraction;

        public BossAbility[] abilities = Array.Empty<BossAbility>();
        [Tooltip("Lo que invoca Summon (enemigo de la lámina).")] public EnemyData summon;

        [Header("Animaciones (las crea BossCharacterAssets)")]
        public DirectionalSpriteSet walk;
        public DirectionalSpriteSet attack;
        public DirectionalSpriteSet special;
        [Tooltip("Caída: se aplasta contra el suelo (generada desde el reposo por recortar_jefes.py).")]
        public DirectionalSpriteSet death;
    }
}
