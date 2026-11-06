using UnityEngine;

namespace CronicasDeMexico.Gameplay.Movement
{
    /// <summary>
    /// Personaje que se mueve sobre el plano XZ (jugador, NPC, enemigos). Lo lee DirectionalSpriteAnimator.
    /// </summary>
    public interface IPlanarMotion
    {
        /// <summary>Velocidad horizontal actual en m/s.</summary>
        Vector3 PlanarVelocity { get; }

        /// <summary>Hacia dónde mira el personaje (horizontal y normalizada).</summary>
        Vector3 FacingDirection { get; }
    }
}
