using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Golpe cuerpo a cuerpo hacia donde mira el jugador. Una esfera delante del personaje daña a cada Health
    /// de los layers objetivo una sola vez por golpe. Búfer fijo: no genera basura.
    /// Combo de tres golpes con avance (el tercero remata), pulsación guardada si llega un poco antes de tiempo,
    /// pausa breve al acertar (hitstop), sacudida de cámara y giro asistido hacia el enemigo más cercano.
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

        [Header("Combo")]
        [SerializeField, Min(0f), Tooltip("Segundos que se guarda una pulsación hecha antes de poder golpear.")]
        float inputBuffer = 0.15f;

        [SerializeField, Min(1), Tooltip("Golpes del combo; el último es el remate.")]
        int comboLength = 3;

        [SerializeField, Min(0f), Tooltip("Segundos tras un golpe en los que el siguiente continúa el combo.")]
        float comboWindow = 0.5f;

        [SerializeField, Min(1f), Tooltip("Daño del remate = daño × este factor.")]
        float finisherDamageFactor = 1.5f;

        [SerializeField, Min(0f), Tooltip("Espera tras el remate antes de volver a golpear.")]
        float finisherRecovery = 0.45f;

        [SerializeField, Min(0f), Tooltip("Impulso hacia delante de cada golpe en m/s (el remate, ×1,5).")]
        float lungeSpeed = 5f;

        [Header("Sensación del impacto")]
        [SerializeField, Min(0f), Tooltip("Segundos reales de pausa al acertar (el remate, ×1,6).")]
        float hitStop = 0.05f;

        [SerializeField, Min(0f), Tooltip("Amplitud en metros de la sacudida al acertar (el remate, ×2).")]
        float hitShake = 0.05f;

        [Header("Apuntado asistido")]
        [SerializeField, Min(0f), Tooltip("Distancia a la que el golpe gira hacia un enemigo. 0 lo desactiva.")]
        float aimAssistRange = 3f;

        [SerializeField, Range(0f, 180f), Tooltip("Grados a cada lado de la mirada en los que busca enemigo.")]
        float aimAssistAngle = 60f;

        const float HitStopScale = 0.05f;

        readonly Collider[] hits = new Collider[8];
        readonly Health[] struck = new Health[8];
        readonly Collider[] areaHits = new Collider[16];
        readonly Health[] areaStruck = new Health[16];
        PlayerMovement movement;
        Health ownHealth;
        PlayerDefense defense;
        readonly Collider[] aimHits = new Collider[16];
        float nextAttackTime;
        float bufferedUntil;
        float lastAttackTime = float.NegativeInfinity;
        float hitStopUntil;

        /// <summary>Golpe del combo lanzado el último: 0, 1… (el remate es ComboLength − 1).</summary>
        public int ComboStep { get; private set; }

        public int ComboLength => comboLength;

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
            defense = GetComponent<PlayerDefense>();
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
            EndHitStop();
        }

        void Update()
        {
            if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil)
                EndHitStop();

            // La esquiva corta el combo: el siguiente golpe vuelve a ser el primero.
            if (movement.IsDodging)
                lastAttackTime = float.NegativeInfinity;

            // En pausa (panel de opciones) o muerto no se ataca.
            if (attackAction == null || Time.timeScale == 0f || (ownHealth != null && ownHealth.IsDead))
                return;

            if (attackAction.action.WasPressedThisFrame())
                bufferedUntil = Time.time + inputBuffer;

            if (Time.time > bufferedUntil || Time.time < nextAttackTime || movement.IsDodging || (defense != null && defense.IsDefending))
                return;

            bufferedUntil = 0f;
            Attack();
        }

        /// <summary>
        /// Siguiente golpe del combo, como al pulsar Atacar: gira hacia el enemigo cercano, avanza y golpea, y fija
        /// la espera hasta el siguiente. Devuelve cuántos objetivos recibieron daño.
        /// </summary>
        public int Attack()
        {
            ComboStep = Time.time - lastAttackTime <= comboWindow && ComboStep < comboLength - 1 ? ComboStep + 1 : 0;
            var finisher = comboLength > 1 && ComboStep == comboLength - 1;
            lastAttackTime = Time.time;
            nextAttackTime = Time.time + (finisher ? finisherRecovery : cooldown);

            AimAssist();
            movement.AddImpulse(movement.FacingDirection * (lungeSpeed * (finisher ? 1.5f : 1f)));
            var damaged = Strike(finisher ? Mathf.RoundToInt(damage * finisherDamageFactor) : damage);
            if (damaged > 0)
            {
                StartHitStop(hitStop * (finisher ? 1.6f : 1f));
                ScreenShake.Request(hitShake * (finisher ? 2f : 1f), 0.12f);
            }
            return damaged;
        }

        /// <summary>Gira la mirada hacia el enemigo vivo más cercano dentro del cono de apuntado. Devuelve si encontró uno.</summary>
        public bool AimAssist()
        {
            if (aimAssistRange <= 0f)
                return false;

            var origin = transform.position;
            var facing = movement.FacingDirection;
            var minDot = Mathf.Cos(aimAssistAngle * Mathf.Deg2Rad);
            var count = Physics.OverlapSphereNonAlloc(origin + Vector3.up * height, aimAssistRange, aimHits, targetLayers, QueryTriggerInteraction.Ignore);
            var bestDistance = float.PositiveInfinity;
            var best = Vector3.zero;
            for (var i = 0; i < count; i++)
            {
                var target = aimHits[i].GetComponentInParent<Health>();
                if (target == null || target == ownHealth || target.IsDead)
                    continue;
                var toTarget = Vector3.ProjectOnPlane(target.transform.position - origin, Vector3.up);
                var distance = toTarget.magnitude;
                if (distance < 0.01f || distance >= bestDistance || Vector3.Dot(toTarget / distance, facing) < minDot)
                    continue;
                bestDistance = distance;
                best = toTarget / distance;
            }

            if (best == Vector3.zero)
                return false;
            movement.Face(best);
            return true;
        }

        // Escala casi nula y no 0: con 0 el juego se daría por pausado (controles táctiles, menús).
        void StartHitStop(float seconds)
        {
            if (seconds <= 0f || Time.timeScale != 1f)
                return;
            Time.timeScale = HitStopScale;
            hitStopUntil = Time.unscaledTime + seconds;
        }

        void EndHitStop()
        {
            // Si entretanto un menú pausó, la escala ya no es la del hitstop y no se toca.
            if (hitStopUntil > 0f && Mathf.Approximately(Time.timeScale, HitStopScale))
                Time.timeScale = 1f;
            hitStopUntil = 0f;
        }

        /// <summary>Se lanzó un golpe (para la animación del zarpazo).</summary>
        public event System.Action Struck;

        /// <summary>Daña una vez a cada enemigo en una esfera con centro a medio metro sobre el punto dado (habilidades).</summary>
        public void StrikeArea(Vector3 center, float areaRadius, int areaDamage)
        {
            var count = Physics.OverlapSphereNonAlloc(center + Vector3.up * 0.5f, areaRadius, areaHits, targetLayers, QueryTriggerInteraction.Ignore);
            var struckCount = 0;
            for (var i = 0; i < count && struckCount < areaStruck.Length; i++)
            {
                var target = areaHits[i].GetComponentInParent<Health>();
                if (target == null || target == ownHealth || System.Array.IndexOf(areaStruck, target, 0, struckCount) >= 0)
                    continue;
                areaStruck[struckCount++] = target;
                target.TryDamage(areaDamage, transform.position);
            }
            System.Array.Clear(areaStruck, 0, struckCount);
        }

        /// <summary>Lanza un golpe ahora, sin entrada ni enfriamiento. Devuelve cuántos objetivos recibieron daño.</summary>
        public int Strike() => Strike(damage);

        int Strike(int strikeDamage)
        {
            Struck?.Invoke();
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
                if (target.TryDamage(strikeDamage, transform.position))
                    damaged++;
            }

            System.Array.Clear(struck, 0, struckCount);
            return damaged;
        }
    }
}
