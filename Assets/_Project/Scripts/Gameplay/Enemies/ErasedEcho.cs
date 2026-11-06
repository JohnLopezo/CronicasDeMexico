using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Eco borrado (Docs/Enemigos/Olmeca.md): silueta incompleta que no camina. Cuando el jugador se acerca, brilla para
    /// avisar y le dispara fragmentos de símbolos (Projectile, reutilizados); tras unos disparos, o al recibir un golpe,
    /// se desvanece y reaparece en otro de sus puntos. Mientras está desvanecido no se le puede dañar. No reaparece al
    /// caer (como los enemigos de la mazmorra, vuelve al cargar el piso).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class ErasedEcho : MonoBehaviour, IPlanarMotion
    {
        static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        const float ShotHeight = 0.8f;

        [SerializeField, Tooltip("Si está vacío se usa el objeto con la etiqueta Player al iniciar.")]
        Transform target;

        [SerializeField, Tooltip("Puntos donde aparece, en orden. El primero es donde empieza.")]
        Vector3[] anchors = System.Array.Empty<Vector3>();

        [SerializeField] SpriteRenderer spriteRenderer;

        [SerializeField, Tooltip("Fragmentos que dispara (hijos inactivos).")]
        Projectile[] projectiles = System.Array.Empty<Projectile>();

        [SerializeField, Min(0f), Tooltip("Distancia a la que empieza a disparar.")]
        float detectionRadius = 8f;

        [SerializeField, Min(0f), Tooltip("Segundos de aviso (brillo) antes de cada disparo.")]
        float windupTime = 0.6f;

        [SerializeField, Min(0f), Tooltip("Segundos entre disparos.")]
        float shotInterval = 1.2f;

        [SerializeField, Min(1), Tooltip("Disparos antes de cambiar de sitio.")]
        int shotsPerAnchor = 2;

        [SerializeField, Min(0f)] float projectileSpeed = 5f;

        [SerializeField, Min(1)] int projectileDamage = 10;

        [SerializeField, Min(0f), Tooltip("Segundos desvanecido al cambiar de sitio.")]
        float fadeTime = 0.9f;

        enum State { Waiting, Windup, Cooldown, Faded }

        CharacterController controller;
        Health health;
        Health targetHealth;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        State state;
        float timer;
        int shots;
        int anchorIndex;

        public Vector3 PlanarVelocity => Vector3.zero;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public bool IsFaded => state == State.Faded;

        public Vector3[] Anchors
        {
            get => anchors;
            set => anchors = value;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            block = new MaterialPropertyBlock();
            foreach (var projectile in projectiles)
                projectile.transform.SetParent(null, true); // vuelan por su cuenta
            renderers = GetComponentsInChildren<Renderer>(true);
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

        void Update()
        {
            if (health.IsDead)
                return;

            timer -= Time.deltaTime;
            var toTarget = target != null ? Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up) : Vector3.zero;
            if (toTarget.sqrMagnitude > 0.0001f && state != State.Faded)
                FacingDirection = toTarget.normalized;
            var targetAlive = target != null && (targetHealth == null || !targetHealth.IsDead);
            var inRange = targetAlive && toTarget.sqrMagnitude <= detectionRadius * detectionRadius;

            switch (state)
            {
                case State.Waiting:
                    if (inRange)
                        BeginWindup();
                    break;
                case State.Windup:
                    SetGlow(1f - Mathf.Clamp01(timer / windupTime));
                    if (timer > 0f)
                        break;
                    SetGlow(0f);
                    if (inRange)
                        Fire(toTarget);
                    state = State.Cooldown;
                    timer = shotInterval;
                    break;
                case State.Cooldown:
                    if (timer > 0f)
                        break;
                    if (shots >= shotsPerAnchor)
                        FadeOut();
                    else if (inRange)
                        BeginWindup();
                    else
                        state = State.Waiting;
                    break;
                case State.Faded:
                    if (timer <= 0f)
                        Reappear();
                    break;
            }
        }

        void BeginWindup()
        {
            state = State.Windup;
            timer = windupTime;
        }

        void Fire(Vector3 toTarget)
        {
            shots++;
            foreach (var projectile in projectiles)
            {
                if (projectile.IsFlying)
                    continue;
                projectile.Launch(transform.position + Vector3.up * ShotHeight, toTarget.normalized * projectileSpeed,
                    projectileDamage, targetHealth, 4f);
                return;
            }
        }

        void FadeOut()
        {
            state = State.Faded;
            timer = fadeTime;
            shots = 0;
            SetGlow(0f);
            SetPresent(false);
            health.GrantInvulnerability(fadeTime + 0.1f);
        }

        void Reappear()
        {
            if (anchors.Length > 0)
            {
                anchorIndex = (anchorIndex + 1) % anchors.Length;
                transform.position = anchors[anchorIndex];
            }
            SetPresent(true);
            state = State.Waiting;
        }

        // Al recibir un golpe se esfuma enseguida y cambia de sitio.
        void OnDamaged(Vector3 sourcePosition)
        {
            if (!health.IsDead)
                FadeOut();
        }

        void OnDied()
        {
            SetGlow(0f);
            SetPresent(false);
            enabled = false;
        }

        // Ausente no se dibuja ni tiene collider: el golpe del jugador lo ignora.
        void SetPresent(bool present)
        {
            controller.enabled = present;
            foreach (var r in renderers)
                r.enabled = present;
        }

        void SetGlow(float amount)
        {
            if (spriteRenderer == null)
                return;
            spriteRenderer.GetPropertyBlock(block);
            block.SetFloat(FlashAmountId, amount * 0.6f);
            spriteRenderer.SetPropertyBlock(block);
        }
    }
}
