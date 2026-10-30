using CronicasDeMexico.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Golpe cuerpo a cuerpo hacia donde mira el jugador. Una esfera delante del personaje daña a cada Health
    /// de los layers objetivo una sola vez por golpe. Búfer fijo: no genera basura.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        [SerializeField, Tooltip("Acción de botón Atacar (Player/Attack).")]
        InputActionReference attackAction;

        [SerializeField, Tooltip("Layers que puede golpear.")]
        LayerMask targetLayers;

        [SerializeField, Min(1), Tooltip("Daño por golpe (escala ×10 desde M5). PlayerStats lo fija según el nivel.")]
        int damage = 10;

        [SerializeField, Min(0f), Tooltip("Distancia del centro del golpe delante del personaje.")]
        float reach = 0.8f;

        [SerializeField, Min(0.1f), Tooltip("Radio del golpe en metros.")]
        float radius = 0.7f;

        [SerializeField, Min(0f), Tooltip("Segundos mínimos entre golpes.")]
        float cooldown = 0.35f;

        [SerializeField, Tooltip("Altura del golpe sobre los pies.")]
        float height = 0.7f;

        readonly Collider[] hits = new Collider[8];
        readonly Health[] struck = new Health[8];
        PlayerMovement movement;
        Health ownHealth;
        float nextAttackTime;

        public int Damage
        {
            get => damage;
            set => damage = Mathf.Max(1, value);
        }

        public LayerMask TargetLayers => targetLayers;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            ownHealth = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (attackAction != null)
                attackAction.action.Enable();
        }

        void OnDisable()
        {
            if (attackAction != null)
                attackAction.action.Disable();
        }

        void Update()
        {
            // En pausa (panel de opciones) o muerto no se ataca.
            if (attackAction == null || Time.timeScale == 0f || (ownHealth != null && ownHealth.IsDead))
                return;

            if (!attackAction.action.WasPressedThisFrame() || Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + cooldown;
            Strike();
        }

        /// <summary>Lanza un golpe ahora, sin entrada ni enfriamiento. Devuelve cuántos objetivos recibieron daño.</summary>
        public int Strike()
        {
            var center = transform.position + Vector3.up * height + movement.FacingDirection * reach;
            var count = Physics.OverlapSphereNonAlloc(center, radius, hits, targetLayers, QueryTriggerInteraction.Ignore);

            var struckCount = 0;
            var damaged = 0;
            for (var i = 0; i < count && struckCount < struck.Length; i++)
            {
                var target = hits[i].GetComponentInParent<Health>();
                if (target == null || target == ownHealth || System.Array.IndexOf(struck, target, 0, struckCount) >= 0)
                    continue;

                struck[struckCount++] = target;
                if (target.TryDamage(damage, transform.position))
                    damaged++;
            }

            System.Array.Clear(struck, 0, struckCount);
            return damaged;
        }
    }
}
