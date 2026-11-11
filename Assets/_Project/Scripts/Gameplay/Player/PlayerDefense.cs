using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Defensa: mientras se mantiene pulsada, el Guardián se queda quieto, no ataca y recibe menos daño
    /// (Docs/Personajes/Auditoria_Animaciones.md). No se puede defender durante una esquiva.
    /// Bloqueo perfecto: un golpe que llega justo después de empezar a defender se anula del todo y aturde a quien lo
    /// dio (si es un IStaggerable; los proyectiles solo se anulan). Pulsar sin parar no sirve: la ventana tarda en
    /// rearmarse.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement))]
    public sealed class PlayerDefense : MonoBehaviour
    {
        const string DefenseActionName = "Defense";

        [SerializeField, Range(0.1f, 1f), Tooltip("Parte del daño que se recibe al defender.")]
        float damageFactor = 0.5f;

        [Header("Bloqueo perfecto")]
        [SerializeField, Min(0f), Tooltip("Segundos desde que empieza la defensa en los que un golpe se anula. 0 lo desactiva.")]
        float perfectWindow = 0.2f;

        [SerializeField, Min(0f), Tooltip("Segundos desde una pulsación hasta que la siguiente vuelve a tener ventana.")]
        float perfectRearm = 0.6f;

        [SerializeField, Min(0f), Tooltip("Segundos que queda aturdido quien golpeó.")]
        float staggerSeconds = 1.2f;

        [SerializeField, Min(0f), Tooltip("Distancia al origen del golpe a la que se busca al atacante.")]
        float attackerSearchRadius = 0.8f;

        const float PerfectTimeScale = 0.05f;
        const float PerfectFreeze = 0.1f;

        readonly Collider[] nearby = new Collider[8];
        Health health;
        PlayerMovement movement;
        PlayerAttack attack;
        DamageFlash flash;
        InputAction action;
        float perfectUntil;
        float nextPerfectWindow = float.NegativeInfinity;

        public bool IsDefending { get; private set; }

        /// <summary>Pedir la defensa sin la acción de entrada: las pruebas y, cuando exista, el botón táctil.</summary>
        public bool Held { get; set; }

        /// <summary>Un golpe que llegue ahora se anularía.</summary>
        public bool InPerfectWindow => IsDefending && Time.time <= perfectUntil;

        /// <summary>Se anuló un golpe con un bloqueo perfecto.</summary>
        public event System.Action PerfectBlocked;

        void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
            attack = GetComponent<PlayerAttack>();
            flash = GetComponent<DamageFlash>();
            action = movement.FindSiblingAction(DefenseActionName);
        }

        void OnEnable()
        {
            action?.Enable();
            health.Intercept = Absorb;
        }

        void OnDisable()
        {
            action?.Disable();
            Set(false);
            if (health.Intercept == Absorb)
                health.Intercept = null;
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
            if (!defending)
                return;
            if (Time.time >= nextPerfectWindow)
                perfectUntil = Time.time + perfectWindow;
            nextPerfectWindow = Time.time + perfectRearm;
        }

        bool Absorb(int amount, Vector3 source)
        {
            if (!InPerfectWindow)
                return false;

            if (attack != null)
            {
                var count = Physics.OverlapSphereNonAlloc(source + Vector3.up * 0.5f, attackerSearchRadius, nearby, attack.TargetLayers,
                    QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                    if (nearby[i].GetComponentInParent<IStaggerable>() is { } attacker)
                    {
                        attacker.Stagger(staggerSeconds);
                        break;
                    }
            }

            TimeDilation.Request(PerfectTimeScale, PerfectFreeze);
            ScreenShake.Request(0.1f, 0.15f);
            if (flash != null)
                flash.Flash();
            PerfectBlocked?.Invoke();
            return true;
        }
    }
}
