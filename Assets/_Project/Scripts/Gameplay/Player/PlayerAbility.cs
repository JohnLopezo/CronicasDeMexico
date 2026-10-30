using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Habilidad de la Bendición principal (Docs/Contenido/Bendiciones.md). La del arco olmeca es el golpe de tierra:
    /// un golpe en área alrededor del Guardián que daña a los enemigos (combate) y rompe el basalto agrietado
    /// (exploración, BreakableBasalt). Gasta energía, que no se regenera sola, y tiene un tiempo de espera. Sin la
    /// Bendición desbloqueada no hace nada.
    /// </summary>
    [RequireComponent(typeof(PlayerAttack))]
    public sealed class PlayerAbility : MonoBehaviour
    {
        const string AbilityActionName = "Ability";
        public const string EarthBlessingId = "tierra";

        [SerializeField, Tooltip("Acción de habilidad; si está vacía se busca junto a la de movimiento.")]
        InputActionReference abilityAction;

        [SerializeField, Tooltip("Bendición que da la habilidad.")] string blessingId = EarthBlessingId;
        [SerializeField, Min(0.5f)] float radius = 2.5f;
        [SerializeField, Min(0f), Tooltip("Daño = ataque × este factor.")] float damageFactor = 2f;
        [SerializeField, Min(0), Tooltip("Si la Bendición no lo indica.")] int defaultEnergyCost = 30;
        [SerializeField, Min(0f), Tooltip("Si la Bendición no lo indica.")] float defaultCooldown = 4f;
        [SerializeField, Tooltip("Círculo que se ve un instante al golpear.")] TelegraphMarker visual;
        [SerializeField, Tooltip("Para avisar si falta energía.")] StringEventChannel messageChannel;

        readonly Collider[] hits = new Collider[16];
        readonly Health[] struck = new Health[16];
        PlayerAttack attack;
        Health ownHealth;
        InputAction ability;
        float nextUse;
        float visualUntil;

        public bool IsReady => Time.time >= nextUse;

        void Awake()
        {
            attack = GetComponent<PlayerAttack>();
            ownHealth = GetComponent<Health>();
            ability = abilityAction != null ? abilityAction.action
                : GetComponent<PlayerMovement>() is { } movement ? movement.FindSiblingAction(AbilityActionName) : null;
        }

        void OnEnable() => ability?.Enable();

        void OnDisable() => ability?.Disable();

        void Update()
        {
            if (visual != null && visualUntil > 0f && Time.time >= visualUntil)
            {
                visual.Hide();
                visualUntil = 0f;
            }
            if (ability != null && Time.timeScale > 0f && ability.WasPressedThisFrame())
                TryUse();
        }

        /// <summary>Usa la habilidad si está desbloqueada, lista y con energía. Devuelve si se usó.</summary>
        public bool TryUse()
        {
            if (!ServiceLocator.TryGet(out Progression progression) || progression.PrimaryBlessing != blessingId ||
                !IsReady || (ownHealth != null && ownHealth.IsDead))
                return false;

            var info = progression.BlessingCatalog?.Find(blessingId);
            var cost = info?.EnergyCost ?? defaultEnergyCost;
            if (!progression.SpendEnergy(cost))
            {
                if (messageChannel != null)
                    messageChannel.Raise("No te queda energía para el golpe de tierra. Descansa en un santuario de Memoria.");
                return false;
            }

            nextUse = Time.time + (info?.Cooldown ?? defaultCooldown);
            Strike();
            return true;
        }

        void Strike()
        {
            var center = transform.position;
            var damage = Mathf.RoundToInt(attack.Damage * damageFactor);

            // Enemigos en el área.
            var count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, radius, hits, attack.TargetLayers,
                QueryTriggerInteraction.Ignore);
            var struckCount = 0;
            for (var i = 0; i < count && struckCount < struck.Length; i++)
            {
                var target = hits[i].GetComponentInParent<Health>();
                if (target == null || target == ownHealth || System.Array.IndexOf(struck, target, 0, struckCount) >= 0)
                    continue;
                struck[struckCount++] = target;
                target.TryDamage(damage, center);
            }
            System.Array.Clear(struck, 0, struckCount);

            // Basalto agrietado en el área (exploración).
            count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, radius, hits, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
                if (hits[i].GetComponentInParent<BreakableBasalt>() is { } basalt)
                    basalt.Break();

            if (visual != null)
            {
                visual.Show(center, radius, 0.3f);
                visualUntil = Time.time + 0.3f;
            }
        }
    }
}
