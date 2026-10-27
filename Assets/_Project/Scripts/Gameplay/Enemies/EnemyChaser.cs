using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Enemigo cuerpo a cuerpo: persigue al objetivo cuando se acerca, lo daña al tocarlo y vuelve a su sitio
    /// cuando lo pierde. Al recibir un golpe retrocede y queda aturdido un instante; al morir desaparece y
    /// reaparece en su sitio pasado un tiempo. Camina en línea recta (sin NavMesh) y el contacto se comprueba
    /// por distancia, sin física.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class EnemyChaser : MonoBehaviour, IPlanarMotion
    {
        const float HomeArriveDistance = 0.2f;
        const float ReturnSpeedFactor = 0.6f;

        [SerializeField, Tooltip("Si está vacío se usa el objeto con la etiqueta Player al iniciar.")]
        Transform target;

        [SerializeField, Min(0f), Tooltip("Distancia a la que empieza a perseguir.")]
        float detectionRadius = 6f;

        [SerializeField, Min(0f), Tooltip("Distancia a la que deja de perseguir y vuelve a su sitio.")]
        float loseRadius = 9f;

        [SerializeField, Min(0f), Tooltip("Velocidad de persecución en m/s.")]
        float moveSpeed = 2.6f;

        [SerializeField, Min(0f), Tooltip("Aceleración y frenado en m/s².")]
        float acceleration = 12f;

        [SerializeField, Min(0f), Tooltip("Distancia entre centros a la que golpea al objetivo.")]
        float contactDistance = 0.8f;

        [SerializeField, Min(1)] int contactDamage = 1;

        [SerializeField, Min(0f), Tooltip("Velocidad del retroceso al recibir un golpe, en m/s.")]
        float knockbackSpeed = 5f;

        [SerializeField, Min(0f), Tooltip("Segundos aturdido tras recibir un golpe.")]
        float stunTime = 0.3f;

        [SerializeField, Min(0f), Tooltip("Segundos hasta reaparecer en su sitio tras morir.")]
        float respawnDelay = 8f;

        [SerializeField, Tooltip("Gravedad en m/s² (negativa).")]
        float gravity = -20f;

        CharacterController controller;
        Health health;
        Health targetHealth;
        Renderer[] renderers;
        Vector3 home;
        Vector3 planarVelocity;
        float verticalSpeed;
        float stunTimer;
        float respawnTimer;

        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public bool IsChasing { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            home = transform.position;
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            if (target != null)
                targetHealth = target.GetComponentInParent<Health>();
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            if (health.IsDead)
            {
                respawnTimer -= deltaTime;
                if (respawnTimer <= 0f)
                    Respawn();
                return;
            }

            Vector3 desiredVelocity;
            if (stunTimer > 0f)
            {
                stunTimer -= deltaTime;
                desiredVelocity = Vector3.zero;
            }
            else
            {
                desiredVelocity = ChooseVelocity();
            }

            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * deltaTime);
            if (desiredVelocity.sqrMagnitude > 0.0001f)
                FacingDirection = desiredVelocity.normalized;

            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);
        }

        Vector3 ChooseVelocity()
        {
            var targetAlive = target != null && (targetHealth == null || !targetHealth.IsDead);
            if (targetAlive)
            {
                var toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                var distance = toTarget.magnitude;
                IsChasing = distance <= (IsChasing ? loseRadius : detectionRadius);

                if (IsChasing)
                {
                    if (distance > contactDistance)
                        return toTarget / distance * moveSpeed;

                    if (targetHealth != null)
                        targetHealth.TryDamage(contactDamage, transform.position);
                    return Vector3.zero;
                }
            }
            else
            {
                IsChasing = false;
            }

            var toHome = Vector3.ProjectOnPlane(home - transform.position, Vector3.up);
            return toHome.magnitude > HomeArriveDistance ? toHome.normalized * (moveSpeed * ReturnSpeedFactor) : Vector3.zero;
        }

        void OnDamaged(Vector3 sourcePosition)
        {
            var away = Vector3.ProjectOnPlane(transform.position - sourcePosition, Vector3.up);
            var direction = away.sqrMagnitude > 0.0001f ? away.normalized : -FacingDirection;
            planarVelocity = direction * knockbackSpeed;
            stunTimer = stunTime;
            IsChasing = true; // al recibir un golpe se enfada aunque el jugador esté lejos
        }

        void OnDied()
        {
            SetPresent(false);
            planarVelocity = Vector3.zero;
            IsChasing = false;
            respawnTimer = respawnDelay;
        }

        void Respawn()
        {
            transform.position = home;
            SetPresent(true);
            health.Restore();
            stunTimer = 0f;
        }

        // Muerto no se dibuja ni tiene collider (el golpe del jugador y el resto de personajes lo ignoran).
        void SetPresent(bool present)
        {
            controller.enabled = present;
            foreach (var r in renderers)
                r.enabled = present;
        }
    }
}
