using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Movimiento del jugador en ocho direcciones (WASD o stick), relativo a la cámara, con gravedad simple y esquiva:
    /// un desplazamiento corto e invulnerable hacia donde se mueve (o mira), con espera entre usos. El terreno lento
    /// (SlowZone) frena el paso, no la esquiva.
    /// Usa CharacterController: las colisiones con el escenario no requieren Rigidbody ni simulación física.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour, IPlanarMotion, ISlowable
    {
        const string DodgeActionName = "Dodge";

        [SerializeField, Tooltip("Acción Vector2 de movimiento (Player/Move).")]
        InputActionReference moveAction;

        [SerializeField, Tooltip("Acción de esquiva (Player/Dodge). Si está vacía se busca en el mapa de la de movimiento.")]
        InputActionReference dodgeAction;

        [SerializeField, Tooltip("Cámara de referencia para la dirección. Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        [SerializeField, Min(0f), Tooltip("Velocidad máxima en m/s.")]
        float moveSpeed = 4f;

        [SerializeField, Min(0f), Tooltip("Aceleración y frenado en m/s². Valores altos dan un control más inmediato.")]
        float acceleration = 40f;

        [SerializeField, Tooltip("Gravedad en m/s² (negativa).")]
        float gravity = -20f;

        [SerializeField, Min(0f), Tooltip("Velocidad de la esquiva en m/s.")]
        float dodgeSpeed = 10f;

        [SerializeField, Min(0f), Tooltip("Segundos que dura el desplazamiento de la esquiva.")]
        float dodgeTime = 0.18f;

        [SerializeField, Min(0f), Tooltip("Segundos sin recibir daño desde que empieza la esquiva.")]
        float dodgeInvulnerability = 0.3f;

        [SerializeField, Min(0f), Tooltip("Segundos entre esquivas.")]
        float dodgeCooldown = 0.6f;

        CharacterController controller;
        Health health;
        InputAction dodge;
        Vector3 planarVelocity;
        Vector3 dodgeDirection;
        float verticalSpeed;
        float dodgeTimer;
        float nextDodgeTime;
        SlowTracker slow;

        /// <summary>Velocidad horizontal actual en m/s.</summary>
        public Vector3 PlanarVelocity => planarVelocity;

        /// <summary>Última dirección horizontal con entrada (normalizada). La usa el sprite para orientarse.</summary>
        public Vector3 FacingDirection { get; private set; } = Vector3.back;

        public bool IsDodging => dodgeTimer > 0f;

        /// <summary>Multiplicador de la velocidad por el terreno lento (1 fuera de él).</summary>
        public float SpeedMultiplier => slow.Multiplier;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            dodge = dodgeAction != null ? dodgeAction.action
                : moveAction != null ? moveAction.action.actionMap?.FindAction(DodgeActionName) : null;
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void OnEnable()
        {
            if (moveAction != null)
                moveAction.action.Enable();
            dodge?.Enable();
        }

        void OnDisable()
        {
            if (moveAction != null)
                moveAction.action.Disable();
            dodge?.Disable();

            // Desactivado (p. ej. al morir) queda quieto: el sprite no debe seguir caminando.
            planarVelocity = Vector3.zero;
            dodgeTimer = 0f;
        }

        /// <summary>
        /// Suma una velocidad horizontal instantánea (retroceso al recibir daño). La aceleración normal la frena.
        /// </summary>
        public void AddImpulse(Vector3 velocity) => planarVelocity += Vector3.ProjectOnPlane(velocity, Vector3.up);

        /// <summary>
        /// Esquiva hacia la dirección dada (o hacia donde mira, si es cero) si ya pasó la espera. Devuelve si esquivó.
        /// </summary>
        public bool TryDodge(Vector3 direction)
        {
            if (!enabled || Time.time < nextDodgeTime || (health != null && health.IsDead))
                return false;

            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            dodgeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : FacingDirection;
            FacingDirection = dodgeDirection;
            dodgeTimer = dodgeTime;
            nextDodgeTime = Time.time + dodgeCooldown;
            if (health != null)
                health.GrantInvulnerability(dodgeInvulnerability);
            return true;
        }

        public void ApplySlow(float speedFactor) => slow.Apply(speedFactor);

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            var cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            var inputDirection = PlanarMovement.InputToWorldDirection(input, cameraYaw);

            if (dodge != null && Time.timeScale > 0f && dodge.WasPressedThisFrame())
                TryDodge(inputDirection);

            if (dodgeTimer > 0f)
            {
                dodgeTimer -= deltaTime;
                planarVelocity = dodgeDirection * dodgeSpeed;
                if (dodgeTimer <= 0f)
                    planarVelocity = dodgeDirection * (moveSpeed * slow.Multiplier);
            }
            else
            {
                var desiredVelocity = inputDirection * (moveSpeed * slow.Multiplier);
                planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * deltaTime);

                if (desiredVelocity.sqrMagnitude > 0.0001f)
                    FacingDirection = desiredVelocity.normalized;
            }

            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);

            var flags = controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);

            // Al chocar con un muro se descarta la velocidad acumulada para no "arrancar" de golpe al rodearlo.
            if ((flags & CollisionFlags.Sides) != 0 && dodgeTimer <= 0f)
                planarVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
        }
    }
}
