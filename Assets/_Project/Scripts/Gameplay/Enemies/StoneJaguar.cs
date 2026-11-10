using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Jaguar de Piedra, mini-jefe del Pantano (Docs/Enemigos/Olmeca.md): escultura felina que la corrupción anima. Duerme
    /// hasta que el jugador entra en su arena; entonces muestra su barra en el HUD y acecha. Todos sus ataques avisan con
    /// una marca en el suelo (TelegraphMarker) y una pose (GDD §10: Telegraph → Attack → Recover):
    /// zarpazo (círculo corto delante, de cerca), salto (cae donde estaba el jugador al avisar) y rugido (anillo amplio
    /// que empuja; más frecuente por debajo de la mitad de la vida). No hace daño por contacto. Al caer avisa
    /// "vencer:jaguar_piedra" (ReportOnDefeat) y su bandera lo retira para siempre (ActiveWhenFlags).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class StoneJaguar : MonoBehaviour, IPlanarMotion, ISlowable
    {
        enum State { Dormant, Stalk, Windup, Leaping, Recover, Defeated }
        enum Attack { Swipe, Leap, Roar }

        [SerializeField, Tooltip("Si está vacío se usa el objeto con la etiqueta Player al iniciar.")]
        Transform target;

        [SerializeField] DirectionalSpriteAnimator spriteAnimator;
        [SerializeField] TelegraphMarker marker;

        [Header("Poses")]
        [SerializeField, Tooltip("Acecho: quieto y caminando.")] DirectionalSpriteSet stalkPose;
        [SerializeField, Tooltip("Agazapado: aviso del salto y del rugido.")] DirectionalSpriteSet crouchPose;
        [SerializeField] DirectionalSpriteSet leapPose;
        [SerializeField] DirectionalSpriteSet swipePose;

        [Header("HUD")]
        [SerializeField] string displayName = "Jaguar de Piedra";
        [SerializeField, Tooltip("Nombre en la barra del jefe (vacío la oculta).")] StringEventChannel bossNameChannel;
        [SerializeField] StringEventChannel messageChannel;
        [SerializeField, TextArea(2, 6)] string defeatText;

        [Header("Movimiento")]
        [SerializeField, Min(0f), Tooltip("Distancia a la que despierta.")] float engageRadius = 6.5f;
        [SerializeField, Min(0f)] float moveSpeed = 2.2f;
        [SerializeField, Min(0f), Tooltip("Segundos entre el fin de un ataque y el siguiente.")] float attackCooldown = 0.8f;
        [SerializeField] float gravity = -20f;

        [Header("Zarpazo")]
        [SerializeField, Min(0f), Tooltip("Distancia hasta la que prefiere el zarpazo.")] float swipeRange = 2.2f;
        [SerializeField, Min(0f)] float swipeWindup = 0.55f;
        [SerializeField, Min(0f)] float swipeRadius = 1.2f;
        [SerializeField, Min(0f)] float swipeReach = 1f;
        [SerializeField, Min(1)] int swipeDamage = 20;
        [SerializeField, Min(0f)] float swipeRecover = 0.5f;

        [Header("Salto")]
        [SerializeField, Min(0f)] float leapWindup = 0.8f;
        [SerializeField, Min(0f)] float leapRadius = 1.5f;
        [SerializeField, Min(0.05f)] float leapTime = 0.5f;
        [SerializeField, Min(0f)] float leapHeight = 1.6f;
        [SerializeField, Min(1)] int leapDamage = 20;
        [SerializeField, Min(0f), Tooltip("Al caer queda expuesto un rato: el momento de atacarlo.")] float leapRecover = 0.9f;

        [Header("Rugido")]
        [SerializeField, Min(0f)] float roarWindup = 0.9f;
        [SerializeField, Min(0f)] float roarRadius = 3.2f;
        [SerializeField, Min(1)] int roarDamage = 10;
        [SerializeField, Min(0f)] float roarKnockback = 9f;
        [SerializeField, Min(0f)] float roarRecover = 0.6f;

        CharacterController controller;
        Health health;
        Health targetHealth;
        PlayerMovement targetMovement;
        State state;
        Attack attack;
        Vector3 planarVelocity;
        Vector3 leapFrom;
        float verticalSpeed;
        float timer;
        int attacksDone;
        SlowTracker slow;

        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public bool IsEngaged => state != State.Dormant && state != State.Defeated;
        public TelegraphMarker Marker => marker;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
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
            {
                targetHealth = target.GetComponentInParent<Health>();
                targetMovement = target.GetComponentInParent<PlayerMovement>();
            }
        }

        void OnEnable() => health.Died += OnDied;

        void OnDisable()
        {
            health.Died -= OnDied;
            if (marker != null)
                marker.Hide();
        }

        public void ApplySlow(float speedFactor) => slow.Apply(speedFactor);

        /// <summary>Lo despierta aunque el jugador no se haya acercado (lo usan las pruebas).</summary>
        public void Engage()
        {
            if (state != State.Dormant)
                return;
            BeginStalk(attackCooldown);
            if (bossNameChannel != null)
                bossNameChannel.Raise(displayName);
        }

        void Update()
        {
            if (state == State.Defeated)
                return;

            var deltaTime = Time.deltaTime;
            timer -= deltaTime;
            var toTarget = target != null ? Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up) : Vector3.zero;
            var targetAlive = target != null && (targetHealth == null || !targetHealth.IsDead);

            switch (state)
            {
                case State.Dormant:
                    planarVelocity = Vector3.zero;
                    if (targetAlive && toTarget.sqrMagnitude <= engageRadius * engageRadius)
                        Engage();
                    break;
                case State.Stalk:
                    Stalk(toTarget, targetAlive);
                    break;
                case State.Windup:
                    planarVelocity = Vector3.zero;
                    if (timer <= 0f)
                        Release();
                    break;
                case State.Leaping:
                    Leap();
                    return; // sin gravedad ni colisiones durante el salto
                case State.Recover:
                    planarVelocity = Vector3.zero;
                    if (timer <= 0f)
                        BeginStalk(attackCooldown);
                    break;
            }

            if (state == State.Leaping)
                return; // el salto acaba de empezar: ya no camina
            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);
        }

        void Stalk(Vector3 toTarget, bool targetAlive)
        {
            if (!targetAlive)
            {
                planarVelocity = Vector3.zero;
                return;
            }

            var distance = toTarget.magnitude;
            if (distance > 0.001f)
                FacingDirection = toTarget / distance;
            planarVelocity = distance > swipeRange * 0.7f ? FacingDirection * (moveSpeed * slow.Multiplier) : Vector3.zero;
            if (timer > 0f)
                return;

            // Cada tercer ataque es un rugido (cada segundo por debajo de la mitad de la vida).
            var roarEvery = health.Current * 2 <= health.Max ? 2 : 3;
            if ((attacksDone + 1) % roarEvery == 0)
                BeginWindup(Attack.Roar, roarWindup, crouchPose, transform.position, roarRadius);
            else if (distance <= swipeRange)
                BeginWindup(Attack.Swipe, swipeWindup, swipePose, transform.position + FacingDirection * swipeReach, swipeRadius);
            else
                BeginWindup(Attack.Leap, leapWindup, crouchPose, target.position, leapRadius);
        }

        void BeginStalk(float wait)
        {
            state = State.Stalk;
            timer = wait;
            SetPose(stalkPose);
        }

        void BeginWindup(Attack kind, float seconds, DirectionalSpriteSet pose, Vector3 center, float radius)
        {
            attack = kind;
            state = State.Windup;
            timer = seconds;
            planarVelocity = Vector3.zero;
            SetPose(pose);
            marker.Show(new Vector3(center.x, transform.position.y, center.z), radius, seconds);
        }

        void Release()
        {
            attacksDone++;
            switch (attack)
            {
                case Attack.Swipe:
                    marker.Hide();
                    HitIfInside(swipeDamage);
                    EndAttack(swipeRecover);
                    break;
                case Attack.Roar:
                    marker.Hide();
                    if (HitIfInside(roarDamage) && targetMovement != null)
                    {
                        var away = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                        targetMovement.AddImpulse((away.sqrMagnitude > 0.0001f ? away.normalized : -FacingDirection) * roarKnockback);
                    }
                    EndAttack(roarRecover);
                    break;
                case Attack.Leap:
                    // El aviso sigue en el suelo mientras salta: es donde va a caer.
                    state = State.Leaping;
                    timer = leapTime;
                    leapFrom = transform.position;
                    var toLanding = Vector3.ProjectOnPlane(marker.Center - leapFrom, Vector3.up);
                    if (toLanding.sqrMagnitude > 0.0001f)
                        FacingDirection = toLanding.normalized;
                    controller.enabled = false;
                    SetPose(leapPose);
                    break;
            }
        }

        void Leap()
        {
            var t = 1f - Mathf.Clamp01(timer / leapTime);
            var landing = new Vector3(marker.Center.x, leapFrom.y, marker.Center.z);
            transform.position = Vector3.Lerp(leapFrom, landing, t) + Vector3.up * (4f * leapHeight * t * (1f - t));
            planarVelocity = (landing - leapFrom) / leapTime;
            if (t < 1f)
                return;

            transform.position = landing;
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalSpeed = 0f;
            marker.Hide();
            HitIfInside(leapDamage);
            EndAttack(leapRecover);
        }

        void EndAttack(float recover)
        {
            state = State.Recover;
            timer = recover;
            SetPose(stalkPose);
        }

        bool HitIfInside(int damage) =>
            targetHealth != null && !targetHealth.IsDead && marker.Contains(target.position) &&
            targetHealth.TryDamage(damage, transform.position);

        void SetPose(DirectionalSpriteSet pose)
        {
            if (spriteAnimator != null && pose != null)
                spriteAnimator.SpriteSet = pose;
        }

        void OnDied()
        {
            state = State.Defeated;
            planarVelocity = Vector3.zero;
            if (!controller.enabled)
                controller.enabled = true;
            marker.Hide();
            if (bossNameChannel != null)
                bossNameChannel.Raise(null);
            if (messageChannel != null && !string.IsNullOrEmpty(defeatText))
                messageChannel.Raise(defeatText);
        }
    }
}
