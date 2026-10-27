using UnityEngine;

namespace CronicasDeMexico.Gameplay.Movement
{
    /// <summary>
    /// Cálculos de movimiento sobre el plano XZ, sin estado, para poder reutilizarlos (jugador, NPC) y probarlos.
    /// </summary>
    public static class PlanarMovement
    {
        /// <summary>
        /// Velocidad vertical en el suelo: mantiene al CharacterController pegado para que isGrounded no parpadee
        /// en rampas.
        /// </summary>
        public const float GroundedVerticalSpeed = -2f;

        /// <summary>Integra la gravedad sobre la velocidad vertical de un CharacterController.</summary>
        public static float ApplyGravity(float verticalSpeed, bool grounded, float gravity, float deltaTime)
        {
            if (grounded && verticalSpeed < 0f)
                verticalSpeed = GroundedVerticalSpeed;
            return verticalSpeed + gravity * deltaTime;
        }

        /// <summary>
        /// Convierte la entrada (x = derecha, y = adelante) en una dirección del mundo relativa al giro horizontal
        /// de la cámara. La magnitud se limita a 1: las diagonales no son más rápidas y el stick analógico conserva
        /// su intensidad parcial.
        /// </summary>
        public static Vector3 InputToWorldDirection(Vector2 input, float cameraYawDegrees)
        {
            var direction = Quaternion.Euler(0f, cameraYawDegrees, 0f) * new Vector3(input.x, 0f, input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }
    }
}
