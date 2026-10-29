using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Npc
{
    /// <summary>
    /// NPC que recorre una lista de puntos en bucle y espera en cada uno. Camina en línea recta con
    /// CharacterController (sin NavMesh): los puntos deben tener el camino libre entre ellos.
    /// Si algo lo bloquea un rato (p. ej. el jugador), pasa al siguiente punto.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class NpcPatrol : MonoBehaviour, IPlanarMotion
    {
        const float ArriveDistance = 0.15f;
        const float StuckTime = 1.5f;

        [SerializeField, Tooltip("Puntos de patrulla en coordenadas del mundo, en orden de recorrido.")]
        Vector3[] patrolPoints;

        [SerializeField, Min(0f), Tooltip("Velocidad al caminar en m/s.")]
        float moveSpeed = 1.5f;

        [SerializeField, Min(0f), Tooltip("Aceleración y frenado en m/s².")]
        float acceleration = 10f;

        [SerializeField, Min(0f), Tooltip("Segundos de espera en cada punto.")]
        float waitTime = 2f;

        [SerializeField, Tooltip("Gravedad en m/s² (negativa).")]
        float gravity = -20f;

        CharacterController controller;
        Vector3 planarVelocity;
        float verticalSpeed;
        int targetIndex;
        float waitTimer;
        float stuckTimer;

        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;

        /// <summary>True mientras está detenido por una conversación.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>Índice del punto hacia el que camina.</summary>
        public int TargetIndex => targetIndex;

        void Awake() => controller = GetComponent<CharacterController>();

        /// <summary>Se detiene y mira hacia <paramref name="lookAtPosition"/> hasta llamar a Release.</summary>
        public void Hold(Vector3 lookAtPosition)
        {
            IsHeld = true;
            var toLook = Vector3.ProjectOnPlane(lookAtPosition - transform.position, Vector3.up);
            if (toLook.sqrMagnitude > 0.0001f)
                FacingDirection = toLook.normalized;
        }

        public void Release() => IsHeld = false;

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var desiredVelocity = IsHeld ? Vector3.zero : PatrolVelocity(deltaTime);

            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * deltaTime);
            if (desiredVelocity.sqrMagnitude > 0.0001f)
                FacingDirection = desiredVelocity.normalized;

            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);
            var flags = controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);
            if ((flags & CollisionFlags.Sides) != 0)
                planarVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);

            UpdateStuck(desiredVelocity, deltaTime);
        }

        Vector3 PatrolVelocity(float deltaTime)
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return Vector3.zero;

            if (waitTimer > 0f)
            {
                waitTimer -= deltaTime;
                return Vector3.zero;
            }

            var toTarget = Vector3.ProjectOnPlane(patrolPoints[targetIndex] - transform.position, Vector3.up);
            if (toTarget.magnitude > ArriveDistance)
                return toTarget.normalized * moveSpeed;

            AdvanceTarget();
            waitTimer = waitTime;
            return Vector3.zero;
        }

        // Avanza menos de la mitad de lo esperado durante StuckTime: se considera bloqueado.
        void UpdateStuck(Vector3 desiredVelocity, float deltaTime)
        {
            var blocked = desiredVelocity.sqrMagnitude > 0.0001f &&
                          Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude < moveSpeed * 0.5f;
            stuckTimer = blocked ? stuckTimer + deltaTime : 0f;
            if (stuckTimer < StuckTime)
                return;

            stuckTimer = 0f;
            AdvanceTarget();
        }

        void AdvanceTarget() => targetIndex = (targetIndex + 1) % patrolPoints.Length;
    }
}
