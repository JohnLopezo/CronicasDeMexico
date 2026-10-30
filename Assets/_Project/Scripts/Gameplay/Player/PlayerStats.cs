using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Lleva los atributos de la progresión al jugador (Docs/Sistemas/Sistema_Progresion.md): vida máxima y defensa en
    /// Health, ataque en PlayerAttack. Subir de nivel no cura: solo añade la vida nueva. También usa consumibles: la
    /// acción UseItem toma el que más falta hace (curar si hay vida perdida, si no energía) y el inventario puede pedir
    /// uno concreto (Inventory.UseRequested). Actúa antes que SpawnPoint, para que la vida que trae el jugador se aplique
    /// sobre el máximo de su nivel.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Health), typeof(PlayerAttack))]
    public sealed class PlayerStats : MonoBehaviour
    {
        const string UseItemActionName = "UseItem";

        [SerializeField, Tooltip("Acción de usar objeto; si está vacía se busca junto a la de movimiento.")]
        InputActionReference useItemAction;

        Health health;
        PlayerAttack attack;
        InputAction useItem;
        Progression progression;
        Inventory inventory;
        int temporaryDefense;
        float temporaryUntil;

        void Awake()
        {
            health = GetComponent<Health>();
            attack = GetComponent<PlayerAttack>();
            useItem = useItemAction != null ? useItemAction.action
                : GetComponent<PlayerMovement>() is { } movement ? movement.FindSiblingAction(UseItemActionName) : null;
            Connect();
        }

        void OnEnable() => useItem?.Enable();

        void OnDisable() => useItem?.Disable();

        void OnDestroy()
        {
            if (progression != null)
                progression.StatsChanged -= Apply;
            if (inventory != null)
                inventory.UseRequested -= OnUseRequested;
        }

        // Al pulsar Play desde una escena de juego, Boot (que registra la progresión) se carga después: se espera a ella.
        void Connect()
        {
            if (progression != null || !ServiceLocator.TryGet(out progression))
                return;
            ServiceLocator.TryGet(out inventory);
            progression.StatsChanged += Apply;
            if (inventory != null)
                inventory.UseRequested += OnUseRequested;
            Apply();
        }

        void Update()
        {
            if (progression == null)
            {
                Connect();
                return;
            }

            if (temporaryDefense != 0 && Time.time >= temporaryUntil)
            {
                temporaryDefense = 0;
                Apply();
            }

            if (useItem != null && Time.timeScale > 0f && useItem.WasPressedThisFrame())
                UseBest();
        }

        void Apply()
        {
            var stats = progression.Stats;
            health.SetMaxHealth(stats.maxHealth);
            health.Defense = stats.defense + temporaryDefense;
            attack.Damage = stats.attack;
        }

        /// <summary>Usa el consumible que más falta hace. Devuelve si usó alguno.</summary>
        public bool UseBest()
        {
            if (inventory?.Catalog == null || health.IsDead)
                return false;
            string best = null;
            var bestScore = 0;
            foreach (var id in inventory.Items)
            {
                var info = inventory.Catalog.Find(id);
                if (info == null || info.Category != ItemCategory.Consumible)
                    continue;
                // Lo que de verdad recuperaría ahora; la vida cuenta el doble que la energía.
                var score = 2 * Mathf.Min(info.Effect.heal, health.Max - health.Current) +
                            Mathf.Min(info.Effect.energy, progression.Stats.maxEnergy - progression.Energy);
                if (score > bestScore)
                {
                    best = id;
                    bestScore = score;
                }
            }
            return best != null && Use(best);
        }

        /// <summary>Usa un consumible concreto si sirve de algo ahora (no se gasta para nada).</summary>
        public bool Use(string itemId)
        {
            var info = inventory?.Catalog?.Find(itemId);
            if (info == null || info.Category != ItemCategory.Consumible || !inventory.Has(itemId) || health.IsDead)
                return false;

            var effect = info.Effect;
            var useful = (effect.heal > 0 && health.Current < health.Max) ||
                         (effect.energy > 0 && progression.Energy < progression.Stats.maxEnergy) ||
                         effect.defenseBonus > 0;
            if (!useful || !inventory.Remove(itemId, 1))
                return false;

            if (effect.heal > 0)
                health.Heal(effect.heal, HealSource.Consumable);
            if (effect.energy > 0)
                progression.RestoreEnergy(effect.energy);
            if (effect.defenseBonus > 0)
            {
                temporaryDefense = effect.defenseBonus;
                temporaryUntil = Time.time + Mathf.Max(1f, effect.duration);
                Apply();
            }
            return true;
        }

        void OnUseRequested(string itemId) => Use(itemId);
    }
}
