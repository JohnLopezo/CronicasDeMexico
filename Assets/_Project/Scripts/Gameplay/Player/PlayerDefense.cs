using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Defensa: mientras se mantiene pulsada, el Guardián se queda quieto, no ataca y recibe menos daño
    /// (Docs/Personajes/Auditoria_Animaciones.md). No se puede defender durante una esquiva.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement))]
    public sealed class PlayerDefense : MonoBehaviour
    {
        const string DefenseActionName = "Defense";

        [SerializeField, Range(0.1f, 1f), Tooltip("Parte del daño que se recibe al defender.")]
        float damageFactor = 0.5f;

        Health health;
        PlayerMovement movement;
        InputAction action;

        public bool IsDefending { get; private set; }

        /// <summary>Pedir la defensa sin la acción de entrada: las pruebas y, cuando exista, el botón táctil.</summary>
        public bool Held { get; set; }

        void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
            action = movement.FindSiblingAction(DefenseActionName);
        }

        void OnEnable() => action?.Enable();

        void OnDisable()
        {
            action?.Disable();
            Set(false);
        }

        void Update() =>
            Set((Held || (action != null && action.IsPressed())) && Time.timeScale > 0f && !health.IsDead && !movement.IsDodging);

        void Set(bool defending)
        {
            if (defending == IsDefending)
                return;
            IsDefending = defending;
            health.DamageFactor = defending ? damageFactor : 1f;
            movement.Rooted = defending;
        }
    }
}
