using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Habilidad especial del Guardián, desde el inicio y con energía: se queda quieto mientras la lanza y daña el área de
    /// enfrente. Solo la tiene quien ya tiene su arte (GuardianData.SpecialSet): hoy, el piloto, Balam.
    /// shortcut: los números son iguales para todos; pasarlos a GuardianData cuando haya un segundo guerrero con habilidad.
    /// </summary>
    [RequireComponent(typeof(PlayerAttack), typeof(PlayerMovement))]
    public sealed class PlayerSpecial : MonoBehaviour
    {
        const string SpecialActionName = "Special";

        [SerializeField, Min(0), Tooltip("Energía por uso.")] int energyCost = 25;
        [SerializeField, Min(0f), Tooltip("Segundos de espera entre usos.")] float cooldown = 5f;
        [SerializeField, Min(0f), Tooltip("Daño = ataque × este factor.")] float damageFactor = 2.5f;
        [SerializeField, Min(0f), Tooltip("Distancia del centro del área delante del Guardián.")] float reach = 1.2f;
        [SerializeField, Min(0.5f)] float radius = 1.8f;
        [SerializeField, Tooltip("Para avisar si falta energía.")] StringEventChannel messageChannel;

        PlayerAttack attack;
        PlayerMovement movement;
        PlayerDefense defense;
        Health health;
        InputAction action;
        float nextUse;
        float rootedUntil;

        /// <summary>Se lanzó la habilidad (para la animación).</summary>
        public event System.Action Used;

        public bool IsReady => Time.time >= nextUse;

        void Awake()
        {
            attack = GetComponent<PlayerAttack>();
            movement = GetComponent<PlayerMovement>();
            defense = GetComponent<PlayerDefense>();
            health = GetComponent<Health>();
            action = movement.FindSiblingAction(SpecialActionName);
        }

        void OnEnable() => action?.Enable();

        void OnDisable()
        {
            action?.Disable();
            Release();
        }

        void Update()
        {
            if (rootedUntil > 0f && Time.time >= rootedUntil)
                Release();
            if (action != null && Time.timeScale > 0f && action.WasPressedThisFrame())
                TryUse();
        }

        void Release()
        {
            if (rootedUntil <= 0f)
                return;
            rootedUntil = 0f;
            if (defense == null || !defense.IsDefending)
                movement.Rooted = false;
        }

        /// <summary>Lanza la habilidad si el Guardián la tiene, está lista y hay energía. Devuelve si se usó.</summary>
        public bool TryUse()
        {
            if (!ServiceLocator.TryGet(out Progression progression) || progression.Profile is not GuardianData { SpecialSet: { } set } ||
                !IsReady || (health != null && health.IsDead) || (defense != null && defense.IsDefending) || movement.IsDodging)
                return false;

            if (!progression.SpendEnergy(energyCost))
            {
                if (messageChannel != null)
                    messageChannel.Raise("No te queda energía para la habilidad. Descansa en un santuario de Memoria.");
                return false;
            }

            nextUse = Time.time + cooldown;
            attack.AimAssist();
            rootedUntil = Time.time + set.Duration;
            movement.Rooted = true;
            Used?.Invoke();
            attack.StrikeArea(transform.position + movement.FacingDirection * reach, radius, Mathf.RoundToInt(attack.Damage * damageFactor));
            return true;
        }
    }
}
