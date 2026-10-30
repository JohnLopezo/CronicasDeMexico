using System;
using CronicasDeMexico.Core.Events;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Vida de un personaje, con invulnerabilidad breve tras cada golpe.
    /// Otros componentes reaccionan a sus eventos (retroceso, destello, muerte); este no mueve ni oculta nada.
    /// No hay regeneración: estando vivo, la vida solo sube con Heal y una HealSource explícita (Diseno.md §9).
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] int maxHealth = 50;

        [SerializeField, Min(0f), Tooltip("Segundos sin recibir daño tras un golpe.")]
        float invulnerabilityTime = 0.5f;

        [SerializeField, Tooltip("Opcional: publica cada cambio (p. ej. para el HUD).")]
        HealthEventChannel changedChannel;

        int current;
        float invulnerableUntil = float.NegativeInfinity;

        public int Current => current;
        public int Max => maxHealth;
        public bool IsDead => current <= 0;

        /// <summary>Resta a cada golpe (reliquias, Bendiciones, consumibles). Un golpe siempre quita al menos 1.</summary>
        public int Defense { get; set; }

        /// <summary>Recibió daño; el argumento es la posición de quien lo causó.</summary>
        public event Action<Vector3> Damaged;

        public event Action Died;

        /// <summary>Recuperó vida: cantidad efectiva y origen.</summary>
        public event Action<int, HealSource> Healed;

        void Awake() => current = maxHealth;

        // En Start y no en Awake: los oyentes ya se suscribieron en su OnEnable.
        void Start() => Publish();

        /// <summary>Aplica daño si no está muerto ni es invulnerable. Devuelve true si el golpe contó.</summary>
        public bool TryDamage(int amount, Vector3 sourcePosition)
        {
            if (amount <= 0 || IsDead || Time.time < invulnerableUntil)
                return false;

            current = Mathf.Max(0, current - Mathf.Max(1, amount - Defense));
            invulnerableUntil = Time.time + invulnerabilityTime;
            Publish();
            Damaged?.Invoke(sourcePosition);
            if (IsDead)
                Died?.Invoke();
            return true;
        }

        /// <summary>
        /// Cura sin pasar del máximo. No revive: muerto no hace nada. Devuelve la vida realmente recuperada.
        /// </summary>
        public int Heal(int amount, HealSource source)
        {
            if (amount <= 0 || IsDead)
                return 0;

            var healed = Mathf.Min(amount, maxHealth - current);
            if (healed == 0)
                return 0;

            current += healed;
            Publish();
            Healed?.Invoke(healed, source);
            return healed;
        }

        /// <summary>
        /// Devuelve la vida completa a un personaje muerto, al reaparecer. No es una curación: con el personaje vivo
        /// no hace nada. La vida con la que reaparece el jugador sigue pendiente de diseño (Diseno.md §9 y §19).
        /// </summary>
        public void ReviveFull()
        {
            if (!IsDead)
            {
                Debug.LogWarning($"[{nameof(Health)}] ReviveFull en {name}, que no está muerto: se ignora. Para curar usa Heal.", this);
                return;
            }

            current = maxHealth;
            invulnerableUntil = float.NegativeInfinity;
            Publish();
        }

        /// <summary>
        /// Al llegar de otra escena, toma la vida con la que salió. Nunca la sube: el personaje nuevo empieza con la
        /// vida completa y esto solo la baja a la que traía (al menos 1).
        /// </summary>
        public void CarryOver(int carried)
        {
            var value = Mathf.Clamp(carried, 1, current);
            if (value == current)
                return;

            current = value;
            Publish();
        }

        /// <summary>
        /// Cambia la vida máxima (sube de nivel, equipa una reliquia). La vida actual se mueve lo mismo que el máximo:
        /// subir de nivel no cura, solo añade la vida nueva. Nunca mata ni revive.
        /// </summary>
        public void SetMaxHealth(int value)
        {
            value = Mathf.Max(1, value);
            if (value == maxHealth)
                return;
            if (!IsDead)
                current = Mathf.Clamp(current + value - maxHealth, 1, value);
            maxHealth = value;
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
