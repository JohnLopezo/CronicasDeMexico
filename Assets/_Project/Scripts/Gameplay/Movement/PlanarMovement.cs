using UnityEngine;

namespace CronicasDeMexico.Gameplay.Movement
{
    /// <summary>
    /// Cálculos de movimiento sobre el plano XZ, sin estado, para poder reutilizarlos (jugador, NPC) y probarlos.
    /// </summary>
    public static class PlanarMovement
    {
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
