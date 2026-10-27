using System;
using CronicasDeMexico.Core.Events;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Vida de un personaje, con invulnerabilidad breve tras cada golpe.
    /// Otros componentes reaccionan a sus eventos (retroceso, destello, muerte); este no mueve ni oculta nada.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] int maxHealth = 5;

        [SerializeField, Min(0f), Tooltip("Segundos sin recibir daño tras un golpe.")]
        float invulnerabilityTime = 0.5f;

        [SerializeField, Tooltip("Opcional: publica cada cambio (p. ej. para el HUD).")]
        HealthEventChannel changedChannel;

        int current;
        float invulnerableUntil = float.NegativeInfinity;

        public int Current => current;
        public int Max => maxHealth;
        public bool IsDead => current <= 0;

        /// <summary>Recibió daño; el argumento es la posición de quien lo causó.</summary>
        public event Action<Vector3> Damaged;

        public event Action Died;

        void Awake() => current = maxHealth;

        // En Start y no en Awake: los oyentes ya se suscribieron en su OnEnable.
        void Start() => Publish();

        /// <summary>Aplica daño si no está muerto ni es invulnerable. Devuelve true si el golpe contó.</summary>
        public bool TryDamage(int amount, Vector3 sourcePosition)
        {
            if (amount <= 0 || IsDead || Time.time < invulnerableUntil)
                return false;

            current = Mathf.Max(0, current - amount);
            invulnerableUntil = Time.time + invulnerabilityTime;
            Publish();
            Damaged?.Invoke(sourcePosition);
            if (IsDead)
                Died?.Invoke();
            return true;
        }

        /// <summary>Recupera toda la vida (p. ej. al reaparecer).</summary>
        public void Restore()
        {
            current = maxHealth;
            invulnerableUntil = float.NegativeInfinity;
            Publish();
        }

        public void GrantInvulnerability(float seconds) =>
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        void Publish()
        {
            if (changedChannel != null)
                changedChannel.Raise(new HealthChange(current, maxHealth));
        }
    }
}
