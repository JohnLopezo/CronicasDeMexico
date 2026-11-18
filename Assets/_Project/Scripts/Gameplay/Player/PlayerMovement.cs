using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Movimiento del jugador en ocho direcciones (WASD o stick), relativo a la cámara, con gravedad simple y esquiva:
    /// un desplazamiento corto e invulnerable hacia donde se mueve (o mira), con espera entre usos. El terreno lento
    /// (SlowZone) frena el paso, no la esquiva.
    /// Usa CharacterController: las colisiones con el escenario no requieren Rigidbody ni simulación física.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour, IPlanarMotion, ISlowable
    {
        const string DodgeActionName = "Dodge";
        const string HopActionName = "Hop"; // salto (K, cruceta arriba); Dodge es la antigua Jump

        /// <summary>Altura sobre una trampa a partir de la cual el jugador en el aire no la pisa (SpikeTrap, PressurePlate).</summary>
        public const float AirClearance = 0.3f;

        [SerializeField, Tooltip("Acción Vector2 de movimiento (Player/Move).")]
        InputActionReference moveAction;

        [SerializeField, Tooltip("Acción de esquiva (Player/Dodge). Si está vacía se busca en el mapa de la de movimiento.")]
        InputActionReference dodgeAction;

        [SerializeField, Tooltip("Cámara de referencia para la dirección. Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        [SerializeField, Min(0f), Tooltip("Velocidad máxima en m/s.")]
        float moveSpeed = 4f;

        [SerializeField, Min(0f), Tooltip("Aceleración y frenado en m/s². Valores altos dan un control más inmediato.")]
        float acceleration = 40f;

        [SerializeField, Tooltip("Gravedad en m/s² (negativa).")]
        float gravity = -20f;

        [SerializeField, Min(0f), Tooltip("Velocidad de la esquiva en m/s.")]
        float dodgeSpeed = 10f;

        [SerializeField, Min(0f), Tooltip("Segundos que dura el desplazamiento de la esquiva.")]
        float dodgeTime = 0.18f;

        [SerializeField, Min(0f), Tooltip("Segundos sin recibir daño desde que empieza la esquiva.")]
        float dodgeInvulnerability = 0.3f;

        [SerializeField, Min(0f), Tooltip("Segundos entre esquivas.")]
        float dodgeCooldown = 0.6f;

        [SerializeField, Min(1), Tooltip("Esquivas seguidas antes de la espera (cada una en cuanto acaba la anterior).")]
        int dodgeCharges = 2;

        [SerializeField, Min(0f), Tooltip("Segundos que se guarda una pulsación de esquiva hecha antes de poder esquivar.")]
        float dodgeBuffer = 0.15f;

        [Header("Salto")]
        // shortcut: 5,8 y no los 6 de la versión HTML, que integra a pasos y sube 0,83 m; aquí 6 daría 0,9 m. Si se
        // cambia la gravedad, recalcular: altura = v² / (2 × |gravedad|).
        [SerializeField, Min(0f), Tooltip("Velocidad inicial del salto en m/s. Con la gravedad de −20, 5,8 sube ~0,84 m y dura ~0,6 s.")]
        float hopSpeed = 5.8f;

        [SerializeField, Min(0f), Tooltip("En el aire no avanza hacia suelo más alto que esto sobre el despegue: no sube terrazas ni rocas saltando.")]
        float hopMaxStepUp = 0.35f;

        [Header("Esquiva perfecta")]
        [SerializeField, Min(0f), Tooltip("Segundos desde que empieza la esquiva en los que esquivar un golpe es perfecto. 0 lo desactiva.")]
        float perfectDodgeWindow = 0.2f;

        [SerializeField, Range(0.05f, 1f), Tooltip("Escala de tiempo de la cámara lenta.")]
        float perfectDodgeTimeScale = 0.35f;

        [SerializeField, Min(0f), Tooltip("Segundos reales de cámara lenta.")]
        float perfectDodgeSlowMotion = 0.45f;

        [SerializeField, Min(0f), Tooltip("Segundos mínimos entre dos esquivas perfectas.")]
        float perfectDodgeCooldown = 1.5f;

        CharacterController controller;
        Health health;
        InputAction dodge;
        InputAction hop;
        const float LedgeSlideStep = 0.15f; // m por cuadro al apartarse de una arista alta tras un salto
        bool hopping;
        Vector3 hopStart;
        float hopGroundY;
        readonly RaycastHit[] airHits = new RaycastHit[8];
        Vector3 planarVelocity;
        Vector3 dodgeDirection;
        float verticalSpeed;
        float dodgeTimer;
        float nextDodgeTime;
        int dodgesUsed;
        float dodgeBufferedUntil;
        float dodgeStartTime = float.NegativeInfinity;
        float nextPerfectDodge = float.NegativeInfinity;
        DamageFlash flash;
        SlowTracker slow;

        /// <summary>Velocidad horizontal actual en m/s.</summary>
        public Vector3 PlanarVelocity => planarVelocity;
        public float CruiseSpeed => moveSpeed;

        /// <summary>Última dirección horizontal con entrada (normalizada). La usa el sprite para orientarse.</summary>
        public Vector3 FacingDirection { get; private set; } = Vector3.back;

        public bool IsDodging => dodgeTimer > 0f;

        /// <summary>En el aire por un salto (hasta volver a tocar el suelo). Se dibuja con el cuadro de correr.</summary>
        public bool IsHopping => hopping;

        /// <summary>
        /// Más de AirClearance por encima de <paramref name="groundY"/> (una trampa del suelo): ahí solo se está en el aire,
        /// así que no la pisa. Por altura y no por isGrounded, que no se actualiza hasta el siguiente Move.
        /// </summary>
        public bool IsAirborneAbove(float groundY) => transform.position.y > groundY + AirClearance;

        /// <summary>Multiplicador de la velocidad por el terreno lento (1 fuera de él).</summary>
        public float SpeedMultiplier => slow.Multiplier;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            flash = GetComponent<DamageFlash>();
            dodge =dodgeAction != null ? dodgeAction.action : FindSiblingAction(DodgeActionName);
            hop = FindSiblingAction(HopActionName);
        }

        /// <summary>
        /// Una acción del mismo mapa que la de movimiento (Player/Dodge, Player/Ability...): así los componentes del
        /// jugador encuentran sus acciones sin tocar el prefab.
        /// </summary>
        public InputAction FindSiblingAction(string actionName) =>
            moveAction != null ? moveAction.action.actionMap?.FindAction(actionName) : null;

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void OnEnable()
        {
            if (moveAction != null)
                moveAction.action.Enable();
            dodge?.Enable();
            hop?.Enable();
            if (health != null)
                health.Evaded += OnEvaded;
        }

        void OnDisable()
        {
            if (moveAction != null)
                moveAction.action.Disable();
            dodge?.Disable();
            hop?.Disable();
            if (health != null)
                health.Evaded -= OnEvaded;

            // Desactivado (p. ej. al morir) queda quieto: el sprite no debe seguir caminando.
            planarVelocity = Vector3.zero;
            dodgeTimer = 0f;
            hopping = false;
        }

        /// <summary>
        /// Salta si está en el suelo y no defiende ni esquiva (sin doble salto). Devuelve si saltó. En el aire no sube
        /// terrazas ni obstáculos de más de hopMaxStepUp sobre el despegue (BlockedInAir).
        /// </summary>
        public bool TryHop()
        {
            if (!enabled || Rooted || IsDodging || hopping || !controller.isGrounded || (health != null && health.IsDead))
                return false;
            hopping = true;
            hopStart = transform.position;
            hopGroundY = GroundBelowCenter();
            verticalSpeed = hopSpeed;
            return true;
        }

        // En el aire, el paso horizontal sin entrar en suelo más alto que hopMaxStepUp sobre el despegue (terraza, roca),
        // como puedePisar en la versión HTML. Si el paso entero no cabe, prueba solo en X o solo en Z (se desliza junto al
        // borde). Sin esto el CharacterController se sube al borde como a un escalón.
        Vector3 AirStep(Vector3 step)
        {
            if (!BlockedInAir(step))
                return step;
            var alongX = new Vector3(step.x, 0f, 0f);
            if (!BlockedInAir(alongX))
                return alongX;
            var alongZ = new Vector3(0f, 0f, step.z);
            return BlockedInAir(alongZ) ? Vector3.zero : alongZ;
        }

        // Altura del suelo bajo el centro (no la del jugador): apoyado en una arista, el CharacterController se da por
        // «en el suelo» a media altura, y despegar desde ahí dejaría subir a lo que tiene al lado.
        float GroundBelowCenter()
        {
            var origin = transform.position + Vector3.up * 0.3f; // dentro de la cápsula: el rayo no la toca a ella
            return Physics.Raycast(origin, Vector3.down, out var hit, 1.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? Mathf.Min(hit.point.y, transform.position.y)
                : transform.position.y;
        }

        // Una esfera del radio de la cápsula (más su piel) baja desde arriba sobre el punto de destino.
        bool BlockedInAir(Vector3 step)
        {
            const float castFrom = 4f; // por encima de los muros de 3 m: SphereCast ignora lo que ya toca al empezar
            var radius = controller.radius + controller.skinWidth;
            var origin = new Vector3(transform.position.x + step.x, hopGroundY + castFrom, transform.position.z + step.z);
            var count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, airHits, castFrom - hopMaxStepUp - radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
                if (airHits[i].collider != controller)
                    return true;
            return false;
        }

        /// <summary>
        /// Suma una velocidad horizontal instantánea (retroceso al recibir daño). La aceleración normal la frena.
        /// </summary>
        public void AddImpulse(Vector3 velocity) => planarVelocity += Vector3.ProjectOnPlane(velocity, Vector3.up);

        /// <summary>
        /// Esquiva hacia la dirección dada (o hacia donde mira, si es cero) si ya pasó la espera. Devuelve si esquivó.
        /// </summary>
        public bool TryDodge(Vector3 direction)
        {
            if (!enabled || Rooted || IsDodging || (health != null && health.IsDead))
                return false;
            // Pasada la espera desde la última esquiva se recuperan todas las cargas.
            if (Time.time >= nextDodgeTime)
                dodgesUsed = 0;
            if (dodgesUsed >= dodgeCharges)
                return false;
            dodgesUsed++;
            dodgeStartTime = Time.time;

            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            dodgeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : FacingDirection;
            FacingDirection = dodgeDirection;
            dodgeTimer = dodgeTime;
            nextDodgeTime = Time.time + dodgeCooldown;
            if (health != null)
                health.GrantInvulnerability(dodgeInvulnerability);
            return true;
        }

        /// <summary>Se esquivó un golpe justo a tiempo: cámara lenta y esquivas recargadas.</summary>
        public event System.Action PerfectDodged;

        // Golpe que no contó por la invulnerabilidad: es perfecto si llegó al principio de una esquiva.
        void OnEvaded(Vector3 source)
        {
            if (!IsDodging || Time.time - dodgeStartTime > perfectDodgeWindow || Time.time < nextPerfectDodge)
                return;
            nextPerfectDodge = Time.time + perfectDodgeCooldown;
            dodgesUsed = 0;
            TimeDilation.Request(perfectDodgeTimeScale, perfectDodgeSlowMotion);
            if (flash != null)
                flash.Flash();
            PerfectDodged?.Invoke();
        }

        /// <summary>Gira la mirada sin moverse (apuntado asistido). Ignora direcciones nulas o verticales.</summary>
        public void Face(Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude > 0.0001f)
                FacingDirection = direction.normalized;
        }

        public void ApplySlow(float speedFactor) => slow.Apply(speedFactor);

        /// <summary>Quieto en el sitio y sin esquivar, pero con gravedad (al defender).</summary>
        public bool Rooted { get; set; }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            var cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            var inputDirection = PlanarMovement.InputToWorldDirection(input, cameraYaw);

            if (dodge != null && Time.timeScale > 0f && dodge.WasPressedThisFrame())
                dodgeBufferedUntil = Time.time + dodgeBuffer;
            if (Time.time <= dodgeBufferedUntil && TryDodge(inputDirection))
                dodgeBufferedUntil = 0f;
            if (hop != null && Time.timeScale > 0f && hop.WasPressedThisFrame())
                TryHop();

            if (dodgeTimer > 0f)
            {
                dodgeTimer -= deltaTime;
                planarVelocity = dodgeDirection * dodgeSpeed;
                if (dodgeTimer <= 0f)
                    planarVelocity = dodgeDirection * (moveSpeed * slow.Multiplier);
            }
            else
            {
                var desiredVelocity = Rooted ? Vector3.zero : inputDirection * (moveSpeed * slow.Multiplier);
                planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * deltaTime);

                if (desiredVelocity.sqrMagnitude > 0.0001f)
                    FacingDirection = desiredVelocity.normalized;
            }

            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, deltaTime);

            var step = planarVelocity * deltaTime;
            if (hopping && step.sqrMagnitude > 0f)
            {
                var allowed = AirStep(step);
                if (allowed != step && deltaTime > 0f)
                    planarVelocity = allowed / deltaTime;
                step = allowed;
            }
            var flags = controller.Move(step + Vector3.up * (verticalSpeed * deltaTime));

            // Al chocar con un muro se descarta la velocidad acumulada para no "arrancar" de golpe al rodearlo.
            if ((flags & CollisionFlags.Sides) != 0 && dodgeTimer <= 0f)
                planarVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);

            if (hopping && controller.isGrounded && verticalSpeed <= 0f)
            {
                // Posado en la arista de algo más alto (saltó pegado a ello): se aparta hacia el despegue y sigue cayendo.
                if (transform.position.y > hopGroundY + hopMaxStepUp)
                {
                    var back = hopStart - transform.position;
                    back.y = 0f;
                    var away = back.sqrMagnitude > 0.0001f ? back.normalized : -FacingDirection;
                    controller.Move(away * Mathf.Min(back.magnitude + controller.radius, LedgeSlideStep));
                }
                else
                {
                    hopping = false;
                }
            }
        }
    }
}
