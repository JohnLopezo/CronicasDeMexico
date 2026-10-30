using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Reacción del jugador al daño: retroceso al recibir un golpe y, al morir, reaparición en el punto de inicio
    /// con la vida completa y unos segundos de invulnerabilidad.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(Health), typeof(CharacterController))]
    public sealed class PlayerDamageResponse : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Velocidad del retroceso en m/s.")]
        float knockbackSpeed = 6f;

        [SerializeField, Min(0f), Tooltip("Segundos tumbado antes de reaparecer.")]
        float respawnDelay = 1.5f;

        [SerializeField, Min(0f), Tooltip("Segundos de invulnerabilidad al reaparecer.")]
        float respawnInvulnerability = 2f;

        PlayerMovement movement;
        Health health;
        CharacterController controller;
        Vector3 spawnPoint;
        float respawnTimer;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            health = GetComponent<Health>();
            controller = GetComponent<CharacterController>();
            spawnPoint = transform.position;
        }

        /// <summary>Dónde reaparece al morir (por defecto, donde empezó la escena).</summary>
        public void SetRespawnPoint(Vector3 position) => spawnPoint = position;

        /// <summary>
        /// Si es false, al morir no reaparece aquí: se encarga otro componente (DefeatReturn lo lleva al santuario de
        /// Memoria). Si vuelve a true estando muerto, reaparece en cuanto pase el tiempo de espera.
        /// </summary>
        public bool RespawnInPlace { get; set; } = true;

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
            if (!health.IsDead)
                return;

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f && RespawnInPlace)
                Respawn();
        }

        void OnDamaged(Vector3 sourcePosition)
        {
            if (health.IsDead)
                return;

            var away = Vector3.ProjectOnPlane(transform.position - sourcePosition, Vector3.up);
            if (away.sqrMagnitude > 0.0001f)
                movement.AddImpulse(away.normalized * knockbackSpeed);
        }

        void OnDied()
        {
            movement.enabled = false;
            respawnTimer = respawnDelay;
        }

        void Respawn()
        {
            // El CharacterController sobrescribe la posición si sigue activo al moverlo.
            controller.enabled = false;
            transform.position = spawnPoint;
            controller.enabled = true;

            health.ReviveFull();
            health.GrantInvulnerability(respawnInvulnerability);
            movement.enabled = true;
        }
    }
}
