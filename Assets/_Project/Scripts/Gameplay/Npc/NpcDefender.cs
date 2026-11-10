using System.Collections.Generic;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Npc
{
    /// <summary>
    /// NPC que pelea (el guardia de la aldea, el joven guerrero): va a por el enemigo que entre en su radio, lo golpea y
    /// vuelve a su patrulla. No tiene vida: los enemigos pueden ir a por él (EnemyChaser), pero no muere, para no romper
    /// la historia. Durante una conversación no pelea.
    /// </summary>
    [RequireComponent(typeof(NpcPatrol))]
    public sealed class NpcDefender : MonoBehaviour
    {
        const float ScanInterval = 0.2f;

        /// <summary>Defensores activos, para que los enemigos elijan objetivo sin buscarlos en la escena.</summary>
        public static readonly List<NpcDefender> Active = new();

        // La recarga de dominio está desactivada: el estado estático se limpia a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStatics() => Active.Clear();

        [SerializeField, Min(0f), Tooltip("Metros a los que detecta enemigos.")] float detectionRadius = 5f;
        [SerializeField, Min(0f), Tooltip("Metros a los que alcanza su golpe.")] float strikeRange = 1.2f;
        [SerializeField, Min(1)] int damage = 10;
        [SerializeField, Min(0f), Tooltip("Segundos entre golpes.")] float strikeCooldown = 1.2f;
        [SerializeField] LayerMask enemyLayers;
        [SerializeField] DirectionalSpriteSet attackSet;

        readonly Collider[] hits = new Collider[8];
        NpcPatrol patrol;
        DirectionalSpriteAnimator animator;
        Health target;
        float scanTimer;
        float cooldownTimer;

        public void Configure(LayerMask enemies, float radius, DirectionalSpriteSet attack)
        {
            enemyLayers = enemies;
            detectionRadius = radius;
            attackSet = attack;
        }

        void Awake()
        {
            patrol = GetComponent<NpcPatrol>();
            animator = GetComponent<DirectionalSpriteAnimator>();
        }

        void OnEnable() => Active.Add(this);

        void OnDisable()
        {
            Active.Remove(this);
            if (patrol != null)
                patrol.ChasePoint = null;
        }

        void Update()
        {
            cooldownTimer -= Time.deltaTime;
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = ScanInterval;
                target = FindNearestEnemy();
            }

            if (target == null || target.IsDead || patrol.IsHeld)
            {
                patrol.ChasePoint = null;
                return;
            }

            var toTarget = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
            if (toTarget.magnitude > strikeRange)
            {
                patrol.ChasePoint = target.transform.position;
                return;
            }

            patrol.ChasePoint = transform.position; // quieto mientras golpea
            patrol.Face(target.transform.position);
            if (cooldownTimer > 0f)
                return;

            cooldownTimer = strikeCooldown;
            if (animator != null)
                animator.Play(attackSet);
            target.TryDamage(damage, transform.position);
        }

        Health FindNearestEnemy()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hits, enemyLayers, QueryTriggerInteraction.Ignore);
            Health nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                if (!hits[i].TryGetComponent<Health>(out var health) || health.IsDead)
                    continue;
                var distance = (health.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = health;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
    }
}
