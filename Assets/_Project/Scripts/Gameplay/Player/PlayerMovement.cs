using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Movimiento del jugador en ocho direcciones (WASD o stick), relativo a la cámara, con gravedad simple.
    /// Usa CharacterController: las colisiones con el escenario no requieren Rigidbody ni simulación física.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        // Mantiene al controlador pegado al suelo para que isGrounded no parpadee en rampas.
        const float GroundedVerticalSpeed = -2f;

        [SerializeField, Tooltip("Acción Vector2 de movimiento (Player/Move).")]
        InputActionReference moveAction;

        [SerializeField, Tooltip("Cámara de referencia para la dirección. Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        [SerializeField, Min(0f), Tooltip("Velocidad máxima en m/s.")]
        float moveSpeed = 4f;

        [SerializeField, Min(0f), Tooltip("Aceleración y frenado en m/s². Valores altos dan un control más inmediato.")]
        float acceleration = 40f;

        [SerializeField, Tooltip("Gravedad en m/s² (negativa).")]
        float gravity = -20f;

        CharacterController controller;
        Vector3 planarVelocity;
        float verticalSpeed;

        /// <summary>Velocidad horizontal actual en m/s.</summary>
        public Vector3 PlanarVelocity => planarVelocity;

        /// <summary>Última dirección horizontal con entrada (normalizada). La usa el sprite para orientarse.</summary>
        public Vector3 FacingDirection { get; private set; } = Vector3.back;

        void Awake() => controller = GetComponent<CharacterController>();

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void OnEnable()
        {
            if (moveAction != null)
                moveAction.action.Enable();
        }

        void OnDisable()
        {
            if (moveAction != null)
                moveAction.action.Disable();
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            var cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;

            var desiredVelocity = PlanarMovement.InputToWorldDirection(input, cameraYaw) * moveSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * deltaTime);

            if (desiredVelocity.sqrMagnitude > 0.0001f)
                FacingDirection = desiredVelocity.normalized;

            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = GroundedVerticalSpeed;
            verticalSpeed += gravity * deltaTime;

            var flags = controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);

            // Al chocar con un muro se descarta la velocidad acumulada para no "arrancar" de golpe al rodearlo.
            if ((flags & CollisionFlags.Sides) != 0)
                planarVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
        }
    }
}
