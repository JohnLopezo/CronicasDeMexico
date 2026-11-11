using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Npc;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Enemigo cuerpo a cuerpo: persigue al objetivo cuando se acerca, lo daña al tocarlo y vuelve a su sitio
    /// cuando lo pierde. Al recibir un golpe retrocede y queda aturdido un instante; al morir desaparece y
    /// reaparece en su sitio pasado un tiempo (salvo en la mazmorra). Camina en línea recta (sin NavMesh), el terreno
    /// lento lo frena (SlowZone) y el contacto se comprueba por distancia, sin física.
    /// Opcional (jaguar corrupto): a media distancia se agazapa, avisa con una franja en el suelo y salta en línea
    /// recta; un golpe durante el aviso lo interrumpe y, tras el salto, queda un instante expuesto. También puede
    /// quedarse visible al morir para mostrar su caída.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class EnemyChaser : MonoBehaviour, IPlanarMotion, ISlowable, IStaggerable
    {
        const float HomeArriveDistance = 0.2f;
        const float ReturnSpeedFactor = 0.6f;
        const float LungeMarkerWidth = 0.9f;

        public enum LungePhase { None, Windup, Leap, Recover }

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

        [SerializeField, Min(1)] int contactDamage = 10;

        [SerializeField, Min(0f), Tooltip("Velocidad del retroceso al recibir un golpe, en m/s.")]
        float knockbackSpeed = 5f;

        [SerializeField, Min(0f), Tooltip("Segundos aturdido tras recibir un golpe.")]
        float stunTime = 0.3f;

        [SerializeField, Min(0f), Tooltip("Segundos hasta reaparecer en su sitio tras morir.")]
        float respawnDelay = 8f;

        [SerializeField, Tooltip("Si no reaparece, queda vencido hasta que se vuelva a cargar la escena (mazmorra).")]
        bool respawns = true;

        [SerializeField, Tooltip("Gravedad en m/s² (negativa).")]
        float gravity = -20f;

        [Header("Salto avisado (jaguar corrupto)")]
        [SerializeField] bool lunges;
        [SerializeField, Min(0f), Tooltip("Salta si el objetivo está entre esta distancia y lungeRange.")] float lungeMinRange = 1.4f;
        [SerializeField, Min(0f)] float lungeRange = 3.2f;
        [SerializeField, Min(0f), Tooltip("Segundos agazapado con la franja a la vista.")] float lungeWindup = 0.6f;
        [SerializeField, Min(0f)] float lungeSpeed = 9f;
        [SerializeField, Min(0f), Tooltip("Metros que recorre el salto.")] float lungeDistance = 3.4f;
        [SerializeField, Min(0f), Tooltip("Segundos quieto tras caer: el momento de atacarlo.")] float lungeRecovery = 0.7f;
        [SerializeField, Min(0f)] float lungeCooldown = 2.5f;
        [SerializeField, Min(1)] int lungeDamage = 10;
        [SerializeField] TelegraphMarker lungeMarker;

        [SerializeField, Min(0f), Tooltip("Segundos que sigue visible al morir (animación de caída) antes de desaparecer.")]
        float deathLinger;

        [Header("Incursión (aldea)")]
        [SerializeField, Tooltip("Al aparecer, en vez de quedarse en su sitio, marcha hasta marchPoint.")] bool marches;
        [SerializeField] Vector3 marchPoint;

        CharacterController controller;
        Health health;
        Health targetHealth;
        Transform player;
        Health playerHealth;
        Renderer[] renderers;
        Vector3 home;
        Vector3 planarVelocity;
        float verticalSpeed;
        float stunTimer;
        float respawnTimer;
        float lingerTimer;
        float lungeTimer;
        float lungeCooldownTimer;
        Vector3 lungeDirection;
        bool lungeHit;
        SlowTracker slow;

        public Vector3 PlanarVelocity => planarVelocity;
        public float CruiseSpeed => moveSpeed;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public bool IsChasing { get; private set; }
        public LungePhase Lunge { get; private set; }

        public bool Lunges
        {
            get => lunges;
            set => lunges = value;
        }

        public TelegraphMarker LungeMarker
        {
            get => lungeMarker;
            set => lungeMarker = value;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            home = marches ? marchPoint : transform.position;
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
            player = target;
            playerHealth = targetHealth;
        }

        // Va a por quien tenga más cerca: el jugador o un defensor (al que no puede dañar: no tiene vida).
        void ChooseNearestTarget()
        {
            if (NpcDefender.Active.Count == 0)
                return;

            var playerAlive = player != null && (playerHealth == null || !playerHealth.IsDead);
            target = playerAlive ? player : null;
            targetHealth = playerAlive ? playerHealth : null;
            var nearest = playerAlive ? (player.position - transform.position).sqrMagnitude : float.PositiveInfinity;
            foreach (var defender in NpcDefender.Active)
            {
                var distance = (defender.transform.position - transform.position).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    target = defender.transform;
                    targetHealth = null;
                }
            }
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            if (health.IsDead)
            {
                if (lingerTimer > 0f)
                {
                    lingerTimer -= deltaTime;
                    if (lingerTimer <= 0f)
                        SetRenderersVisible(false);
                }
                respawnTimer -= deltaTime;
                if (respawnTimer <= 0f)
                    Respawn();
                return;
            }

            lungeCooldownTimer -= deltaTime;
            Vector3 desiredVelocity;
            if (stunTimer > 0f)
            {
                stunTimer -= deltaTime;
                desiredVelocity = Vector3.zero;
            }
            else if (Lunge != LungePhase.None)
            {
                UpdateLunge(deltaTime);
                desiredVelocity = Vector3.zero;
            }
            else
            {
                desiredVelocity = ChooseVelocity();
            }

            if (Lunge == LungePhase.Leap)
                planarVelocity = lungeDirection * (lungeSpeed * slow.Multiplier);
            else
                planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * deltaTime);
            if (desiredVelocity.sqrMagnitude > 0.0001f)
                FacingDirection = desiredVelocity.normalized;

            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);
        }

        Vector3 ChooseVelocity()
        {
            ChooseNearestTarget();
            var targetAlive = target != null && (targetHealth == null || !targetHealth.IsDead);
            if (targetAlive)
            {
                var toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                var distance = toTarget.magnitude;
                IsChasing = distance <= (IsChasing ? loseRadius : detectionRadius);

                if (IsChasing)
                {
                    if (lunges && lungeCooldownTimer <= 0f && distance >= lungeMinRange && distance <= lungeRange)
                    {
                        BeginLunge(toTarget / distance);
                        return Vector3.zero;
                    }

                    if (distance > contactDistance)
                        return toTarget / distance * (moveSpeed * slow.Multiplier);

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
            return toHome.magnitude > HomeArriveDistance
                ? toHome.normalized * (moveSpeed * ReturnSpeedFactor * slow.Multiplier)
                : Vector3.zero;
        }

        // ---------- Salto avisado ----------

        void BeginLunge(Vector3 direction)
        {
            Lunge = LungePhase.Windup;
            lungeDirection = direction;
            FacingDirection = direction;
            lungeTimer = lungeWindup;
            lungeHit = false;
            if (lungeMarker != null)
                lungeMarker.ShowLine(transform.position, direction, lungeDistance + contactDistance, LungeMarkerWidth, lungeWindup);
        }

        void UpdateLunge(float deltaTime)
        {
            lungeTimer -= deltaTime;
            switch (Lunge)
            {
                case LungePhase.Windup:
                    if (lungeTimer > 0f)
                        return;
                    if (lungeMarker != null)
                        lungeMarker.Hide();
                    Lunge = LungePhase.Leap;
                    lungeTimer = lungeSpeed > 0f ? lungeDistance / lungeSpeed : 0f;
                    break;
                case LungePhase.Leap:
                    if (!lungeHit && target != null && targetHealth != null &&
                        Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).magnitude <= contactDistance)
                        lungeHit = targetHealth.TryDamage(lungeDamage, transform.position);
                    if (lungeTimer > 0f)
                        return;
                    Lunge = LungePhase.Recover;
                    lungeTimer = lungeRecovery;
                    planarVelocity = Vector3.zero;
                    break;
                case LungePhase.Recover:
                    if (lungeTimer <= 0f)
                        EndLunge();
                    break;
            }
        }

        void EndLunge()
        {
            if (lungeMarker != null && Lunge == LungePhase.Windup)
                lungeMarker.Hide();
            Lunge = LungePhase.None;
            lungeCooldownTimer = lungeCooldown;
        }

        public void ApplySlow(float speedFactor) => slow.Apply(speedFactor);

        public void Stagger(float seconds)
        {
            if (health != null && health.IsDead)
                return;
            if (Lunge != LungePhase.None)
                EndLunge();
            stunTimer = Mathf.Max(stunTimer, seconds);
            planarVelocity = Vector3.zero;
        }

        void OnDamaged(Vector3 sourcePosition)
        {
            if (Lunge != LungePhase.None)
                EndLunge(); // un golpe durante el aviso o el salto lo interrumpe
            var away = Vector3.ProjectOnPlane(transform.position - sourcePosition, Vector3.up);
            var direction = away.sqrMagnitude > 0.0001f ? away.normalized : -FacingDirection;
            planarVelocity = direction * knockbackSpeed;
            stunTimer = stunTime;
            IsChasing = true; // al recibir un golpe se enfada aunque el jugador esté lejos
        }

        void OnDied()
        {
            if (Lunge != LungePhase.None)
                EndLunge();
            controller.enabled = false;
            lingerTimer = deathLinger;
            if (deathLinger <= 0f)
                SetRenderersVisible(false);
            planarVelocity = Vector3.zero;
            IsChasing = false;
            respawnTimer = respawns ? Mathf.Max(respawnDelay, deathLinger) : float.PositiveInfinity;
        }

        void Respawn()
        {
            transform.position = home;
            controller.enabled = true;
            SetRenderersVisible(true);
            health.ReviveFull();
            stunTimer = 0f;
            lungeCooldownTimer = 0f;
        }

        // Muerto no se dibuja ni tiene collider (el golpe del jugador y el resto de personajes lo ignoran). El aviso del
        // salto tiene su propio SpriteRenderer y lo maneja TelegraphMarker.
        void SetRenderersVisible(bool visible)
        {
            foreach (var r in renderers)
                if (lungeMarker == null || r.gameObject != lungeMarker.gameObject)
                    r.enabled = visible;
        }
    }
}
