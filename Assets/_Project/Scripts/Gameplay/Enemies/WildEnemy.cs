using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Enemigo común guiado por EnemyData (serpiente, cocodrilo, mono, avispa...). Sigue la IA modular del GDD §10:
    /// reposo → persecución (con su forma de moverse) → aviso → golpe → recuperación, y reacciona a los golpes con su
    /// defensa. Todo ataque se avisa: con una marca en el suelo (círculo o franja, TelegraphMarker) o, el proyectil, con
    /// la pose de ataque. Camina en línea recta sin NavMesh, como EnemyChaser, y el contacto se mide por distancia.
    /// Los voladores mueven un hijo intermedio (heightRoot) para que el sprite suba sin perder la rejilla de píxeles.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class WildEnemy : MonoBehaviour, IPlanarMotion, ISlowable
    {
        public enum State { Idle, Chase, Windup, Strike, Recover, Guard, Phased, Dead }

        const float HomeArriveDistance = 0.2f;
        const float ReturnSpeedFactor = 0.6f;
        const float Acceleration = 14f;
        const float KnockbackSpeed = 4f;
        const float StunTime = 0.25f;
        const float LineSpeed = 9f;
        const float LeapBackSpeed = 7f;
        const float LeapBackDistance = 1.2f;
        const float DodgeSpeed = 6f;
        const float PhaseReappearDistance = 1.8f;
        const float PhasedAlpha = 0.3f;
        const float FlyHighFactor = 2.5f;
        const float ZigzagAngle = 35f;
        const float ZigzagFrequency = 4f;
        const float ErraticAngle = 50f;
        const float ErraticInterval = 0.45f;
        const float FrontArmorDot = 0.3f;
        const float AmbushTriggerFactor = 0.5f;
        const float DeathLinger = 0.9f;
        const float BobAmplitude = 0.06f;
        const float BobFrequency = 6f;

        [SerializeField] EnemyData data;

        [SerializeField, Tooltip("Si está vacío se usa el objeto con la etiqueta Player al iniciar.")]
        Transform target;

        [SerializeField, Tooltip("Aviso del ataque normal (círculo o franja, según EnemyData.attackShape).")]
        TelegraphMarker marker;

        [SerializeField, Tooltip("Reserva de proyectiles (attackShape = Projectile).")]
        Projectile[] projectiles;

        [SerializeField, Tooltip("Voladores: hijo entre la raíz y el sprite que sube a la altura de vuelo.")]
        Transform heightRoot;

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] DirectionalSpriteAnimator spriteAnimator;

        [SerializeField, Tooltip("En la mazmorra no reaparece: queda vencido hasta que se vuelva a cargar el piso.")]
        bool respawns = true;

        [SerializeField] float gravity = -20f;

        CharacterController controller;
        Health health;
        Health targetHealth;
        Vector3 home;
        Vector3 planarVelocity;
        Vector3 attackDirection;
        float verticalSpeed;
        float stateTimer;
        float stunTimer;
        float attackCooldownTimer;
        float defenseCooldownTimer;
        float flyHighTimer;
        float erraticTimer;
        float erraticAngle;
        float lingerTimer;
        float respawnTimer;
        bool strikeHit;
        SlowTracker slow;

        public EnemyData Data
        {
            get => data;
            set => data = value;
        }

        public State Current { get; private set; }
        public bool IsChasing { get; private set; }
        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public float CruiseSpeed => data != null ? data.moveSpeed : 0f;

        /// <summary>Altura actual del sprite sobre el suelo (voladores).</summary>
        public float Height => heightRoot != null ? heightRoot.localPosition.y : 0f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            home = transform.position;
            if (data != null)
                health.SetMaxHealth(data.maxHealth);
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
            if (data == null)
                return;
            var deltaTime = Time.deltaTime;
            if (Current == State.Dead)
            {
                UpdateDead(deltaTime);
                return;
            }

            attackCooldownTimer -= deltaTime;
            defenseCooldownTimer -= deltaTime;
            flyHighTimer -= deltaTime;
            UpdateFrontArmor();
            TryLeapBack();

            // Aturdido no elige rumbo ni ataca, pero la defensa y el desvanecido siguen contando su tiempo.
            var desired = Vector3.zero;
            if (stunTimer > 0f)
            {
                stunTimer -= deltaTime;
                if (Current is State.Guard or State.Phased)
                    UpdateState(deltaTime);
            }
            else
            {
                desired = UpdateState(deltaTime);
            }

            if (Current == State.Strike && data.attackShape == EnemyAttackShape.Line)
                planarVelocity = attackDirection * LineSpeed;
            else
                planarVelocity = Vector3.MoveTowards(planarVelocity, desired, Acceleration * deltaTime);
            if (desired.sqrMagnitude > 0.0001f)
                FacingDirection = desired.normalized;

            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);
            UpdateHeight();
        }

        Vector3 UpdateState(float deltaTime)
        {
            stateTimer -= deltaTime;
            switch (Current)
            {
                case State.Windup:
                    if (stateTimer <= 0f)
                        BeginStrike();
                    return Vector3.zero;
                case State.Strike:
                    UpdateStrike();
                    return Vector3.zero;
                case State.Recover:
                    if (stateTimer <= 0f)
                        Current = State.Chase;
                    return Vector3.zero;
                case State.Guard:
                    if (stateTimer <= 0f)
                        EndGuard();
                    return Vector3.zero;
                case State.Phased:
                    if (stateTimer <= 0f)
                        Reappear();
                    return Vector3.zero;
                default:
                    return ChooseVelocity(deltaTime);
            }
        }

        // ---------- Movimiento ----------

        Vector3 ChooseVelocity(float deltaTime)
        {
            var speed = data.moveSpeed * (data.flies ? 1f : slow.Multiplier);
            if (target != null && (targetHealth == null || !targetHealth.IsDead))
            {
                var toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                var distance = toTarget.magnitude;
                var detection = data.movement == EnemyMovement.Ambush ? data.detectionRadius * AmbushTriggerFactor : data.detectionRadius;
                IsChasing = distance <= (IsChasing ? data.loseRadius : detection);
                if (IsChasing && distance > 0.001f)
                {
                    var direction = toTarget / distance;
                    Current = State.Chase;
                    if (attackCooldownTimer <= 0f && distance <= data.attackRange && !TooCloseToShoot(distance))
                    {
                        BeginWindup(direction);
                        return Vector3.zero;
                    }
                    return Steer(direction, distance, deltaTime) * speed;
                }
            }
            else
            {
                IsChasing = false;
            }

            Current = State.Idle;
            var toHome = Vector3.ProjectOnPlane(home - transform.position, Vector3.up);
            return toHome.magnitude > HomeArriveDistance ? toHome.normalized * (speed * ReturnSpeedFactor) : Vector3.zero;
        }

        // Quien ataca de lejos primero se aparta: con el objetivo encima no dispara.
        bool TooCloseToShoot(float distance) =>
            data.movement == EnemyMovement.KeepDistance && distance < data.preferredDistance - 1f;

        /// <summary>Rumbo (unitario, o cero para quedarse) según la forma de moverse de EnemyData.</summary>
        Vector3 Steer(Vector3 direction, float distance, float deltaTime)
        {
            switch (data.movement)
            {
                case EnemyMovement.Zigzag:
                    return Quaternion.Euler(0f, Mathf.Sin(Time.time * ZigzagFrequency) * ZigzagAngle, 0f) * direction;
                case EnemyMovement.Erratic:
                    erraticTimer -= deltaTime;
                    if (erraticTimer <= 0f)
                    {
                        erraticTimer = ErraticInterval;
                        erraticAngle = Random.Range(-ErraticAngle, ErraticAngle);
                    }
                    return Quaternion.Euler(0f, erraticAngle, 0f) * direction;
                case EnemyMovement.KeepDistance:
                    if (distance > data.preferredDistance + 0.5f)
                        return direction;
                    if (distance < data.preferredDistance - 1f)
                        return -direction;
                    return Vector3.zero;
                default:
                    return distance > data.attackRange * 0.8f ? direction : Vector3.zero;
            }
        }

        // ---------- Ataque normal ----------

        void BeginWindup(Vector3 direction)
        {
            Current = State.Windup;
            attackDirection = direction;
            FacingDirection = direction;
            stateTimer = data.attackWindup;
            Play(data.attack, hold: true);
            if (marker == null)
                return;
            switch (data.attackShape)
            {
                case EnemyAttackShape.Circle:
                    marker.Show(CircleCenter(), data.attackSize, data.attackWindup);
                    break;
                case EnemyAttackShape.Line:
                    marker.ShowLine(transform.position, direction, data.attackReach + data.attackSize * 0.5f, data.attackSize, data.attackWindup);
                    break;
            }
        }

        Vector3 CircleCenter() => transform.position + attackDirection * (data.attackSize * 0.8f);

        void BeginStrike()
        {
            if (marker != null)
                marker.Hide();
            Current = State.Strike;
            strikeHit = false;
            switch (data.attackShape)
            {
                case EnemyAttackShape.Circle:
                    if (targetHealth != null && Planar(target.position - CircleCenter()).magnitude <= data.attackSize)
                        targetHealth.TryDamage(data.attackDamage, transform.position);
                    EndStrike();
                    break;
                case EnemyAttackShape.Line:
                    stateTimer = data.attackReach / LineSpeed;
                    Play(data.run, loop: true);
                    break;
                case EnemyAttackShape.Projectile:
                    Launch();
                    EndStrike();
                    break;
            }
        }

        void UpdateStrike()
        {
            if (!strikeHit && targetHealth != null && Planar(target.position - transform.position).magnitude <= data.attackSize)
                strikeHit = targetHealth.TryDamage(data.attackDamage, transform.position);
            if (stateTimer <= 0f)
                EndStrike();
        }

        void EndStrike()
        {
            Stop(data.run);
            Stop(data.attack);
            planarVelocity = Vector3.zero;
            Current = State.Recover;
            stateTimer = data.attackRecover;
            attackCooldownTimer = data.attackCooldown;
        }

        void Launch()
        {
            if (projectiles == null || target == null)
                return;
            foreach (var projectile in projectiles)
            {
                if (projectile == null || projectile.IsFlying)
                    continue;
                var origin = transform.position + Vector3.up * (0.5f + Height);
                var toTarget = Planar(target.position - origin);
                var speed = Mathf.Max(0.1f, data.attackReach);
                projectile.Launch(origin, toTarget.normalized * speed, data.attackDamage, targetHealth,
                    (data.attackRange + 2f) / speed);
                return;
            }
        }

        void CancelAttack()
        {
            if (marker != null && Current == State.Windup)
                marker.Hide();
            if (Current is State.Windup or State.Strike)
            {
                Stop(data.run);
                Stop(data.attack);
                Current = State.Chase;
                attackCooldownTimer = data.attackCooldown * 0.5f;
            }
        }

        // ---------- Defensa ----------

        void UpdateFrontArmor()
        {
            if (data.defense != EnemyDefense.FrontArmor || target == null)
                return;
            var toTarget = Planar(target.position - transform.position);
            var inFront = toTarget.sqrMagnitude > 0.0001f && Vector3.Dot(FacingDirection, toTarget.normalized) > FrontArmorDot;
            health.DamageFactor = inFront ? data.defenseFactor : 1f;
        }

        void TryLeapBack()
        {
            if (data.defense != EnemyDefense.LeapBack || defenseCooldownTimer > 0f || target == null ||
                Current is not (State.Idle or State.Chase or State.Recover))
                return;
            var away = Planar(transform.position - target.position);
            if (away.magnitude > LeapBackDistance)
                return;
            planarVelocity = (away.sqrMagnitude > 0.0001f ? away.normalized : -FacingDirection) * LeapBackSpeed;
            stunTimer = StunTime;
            health.GrantInvulnerability(StunTime);
            defenseCooldownTimer = data.defenseCooldown;
            Play(data.guard);
        }

        void OnDamaged(Vector3 source)
        {
            if (Current == State.Dead)
                return;
            CancelAttack();
            var away = Planar(transform.position - source);
            var push = away.sqrMagnitude > 0.0001f ? away.normalized : -FacingDirection;
            planarVelocity = push * KnockbackSpeed;
            stunTimer = StunTime;
            IsChasing = true; // al recibir un golpe se enfada aunque el jugador esté lejos

            var defending = Current is State.Guard or State.Phased; // un golpe más no la alarga
            if (health.IsDead || defending || defenseCooldownTimer > 0f || !React(push))
            {
                if (!health.IsDead && Current != State.Guard)
                    Play(data.hit);
                return;
            }
            defenseCooldownTimer = data.defenseCooldown;
        }

        /// <summary>Defensa que se activa al recibir un golpe. Devuelve false si este enemigo no tiene ninguna.</summary>
        bool React(Vector3 push)
        {
            switch (data.defense)
            {
                case EnemyDefense.GuardAfterHit:
                    Current = State.Guard;
                    stateTimer = data.defenseSeconds;
                    health.DamageFactor = data.defenseFactor;
                    Play(data.guard, hold: true);
                    return true;
                case EnemyDefense.Phase:
                    Current = State.Phased;
                    stateTimer = data.defenseSeconds;
                    health.GrantInvulnerability(data.defenseSeconds);
                    SetAlpha(PhasedAlpha);
                    return true;
                case EnemyDefense.FlyHigh:
                    flyHighTimer = data.defenseSeconds;
                    health.GrantInvulnerability(data.defenseSeconds);
                    return true;
                case EnemyDefense.Dodge:
                    planarVelocity = Vector3.Cross(Vector3.up, push) * (Random.value < 0.5f ? DodgeSpeed : -DodgeSpeed);
                    health.GrantInvulnerability(StunTime);
                    return true;
                default:
                    return false;
            }
        }

        void EndGuard()
        {
            health.DamageFactor = 1f;
            Stop(data.guard);
            Current = State.Chase;
        }

        void Reappear()
        {
            SetAlpha(1f);
            Current = State.Chase;
            if (target == null)
                return;
            // Reaparece a un lado del objetivo, sin teletransportarse dentro de él.
            var side = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * PhaseReappearDistance;
            controller.enabled = false;
            transform.position = target.position + side;
            controller.enabled = true;
        }

        // ---------- Muerte y reaparición ----------

        void OnDied()
        {
            CancelAttack();
            if (marker != null)
                marker.Hide();
            health.DamageFactor = 1f;
            SetAlpha(1f);
            Current = State.Dead;
            IsChasing = false;
            planarVelocity = Vector3.zero;
            controller.enabled = false;
            lingerTimer = DeathLinger;
            respawnTimer = respawns && data.respawnDelay > 0f ? Mathf.Max(data.respawnDelay, DeathLinger) : float.PositiveInfinity;
            Play(data.death, hold: true);
        }

        void UpdateDead(float deltaTime)
        {
            if (lingerTimer > 0f)
            {
                lingerTimer -= deltaTime;
                if (lingerTimer <= 0f && spriteRenderer != null)
                    spriteRenderer.enabled = false;
            }
            respawnTimer -= deltaTime;
            if (respawnTimer <= 0f)
                Respawn();
        }

        void Respawn()
        {
            transform.position = home;
            controller.enabled = true;
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            health.ReviveFull();
            if (spriteAnimator != null)
                spriteAnimator.Stop();
            Current = State.Idle;
            attackCooldownTimer = 0f;
            defenseCooldownTimer = 0f;
        }

        // ---------- Utilidades ----------

        public void ApplySlow(float speedFactor)
        {
            if (data == null || !data.flies)
                slow.Apply(speedFactor);
        }

        void UpdateHeight()
        {
            if (heightRoot == null || data == null || !data.flies)
                return;
            var height = data.hoverHeight * (flyHighTimer > 0f ? FlyHighFactor : 1f);
            heightRoot.localPosition = Vector3.up * (height + Mathf.Sin(Time.time * BobFrequency) * BobAmplitude);
        }

        void SetAlpha(float alpha)
        {
            if (spriteRenderer == null)
                return;
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        void Play(DirectionalSpriteSet set, bool loop = false, bool hold = false)
        {
            if (spriteAnimator != null && set != null)
                spriteAnimator.Play(set, loop, hold);
        }

        void Stop(DirectionalSpriteSet set)
        {
            if (spriteAnimator != null && set != null)
                spriteAnimator.Stop(set);
        }

        static Vector3 Planar(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);
    }
}
