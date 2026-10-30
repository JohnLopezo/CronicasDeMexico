using System.Collections;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>Ataques del Guardián de la Piedra Viva (para elegirlos a mano en las pruebas).</summary>
    public enum GuardianAttack { Swipe, Charge, Slam, Waves, Shards, Roar, Fury }

    /// <summary>
    /// Guardián de la Piedra Viva, jefe del arco olmeca (Docs/Enemigos/Olmeca.md): figura original de piedra, no una
    /// deidad documentada. Espera sentado en el centro de su arena hasta que el jugador se acerca. Tres fases según su
    /// vida, todas con ataques avisados en el suelo (TelegraphMarker) y una pose:
    /// - Fase 1: zarpazo (círculo delante), embestida (franja; si choca con algo queda aturdido) y golpe de tierra
    ///   (círculo grande a su alrededor).
    /// - Fase 2 (≤ 60 %): sube el agua del borde de la arena (frena, SlowZone), ondas (dos anillos seguidos), fragmentos
    ///   en abanico (Projectile) y rugido que empuja.
    /// - Fase 3 (≤ 25 %): todo más rápido y la Furia de la Primera Piedra (dos tandas de zonas de impacto).
    /// Al cambiar de fase ruge y queda invulnerable un momento. No hace daño por contacto salvo en la embestida. Al caer
    /// avisa "vencer:guardian_piedra_viva" (ReportOnDefeat); su bandera lo retira y muestra la figura liberada.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class StoneGuardian : MonoBehaviour, IPlanarMotion, ISlowable
    {
        const float PhaseTwoFraction = 0.6f;
        const float PhaseThreeFraction = 0.25f;

        [SerializeField, Tooltip("Si está vacío se usa el objeto con la etiqueta Player al iniciar.")]
        Transform target;

        [SerializeField] DirectionalSpriteAnimator spriteAnimator;

        [Header("Avisos")]
        [SerializeField] TelegraphMarker circleMarker;
        [SerializeField] TelegraphMarker lineMarker;
        [SerializeField] TelegraphMarker ringMarker;
        [SerializeField, Tooltip("Zonas de la Furia (la mitad en cada tanda).")] TelegraphMarker[] furyMarkers = System.Array.Empty<TelegraphMarker>();

        [Header("Poses")]
        [SerializeField, Tooltip("Sentado: dormido.")] DirectionalSpriteSet seatedPose;
        [SerializeField, Tooltip("Erguido: quieto y caminando.")] DirectionalSpriteSet standPose;
        [SerializeField] DirectionalSpriteSet chargePose;
        [SerializeField, Tooltip("Alzado: zarpazo y golpe de tierra.")] DirectionalSpriteSet slamPose;
        [SerializeField, Tooltip("Rugido: ondas, fragmentos, rugido, Furia y cambio de fase.")] DirectionalSpriteSet roarPose;

        [Header("Escena")]
        [SerializeField, Tooltip("Agua del borde de la arena: aparece en la fase 2.")] GameObject flood;
        [SerializeField, Tooltip("Fragmentos de piedra que lanza en abanico (hijos inactivos).")] Projectile[] shards = System.Array.Empty<Projectile>();

        [Header("HUD")]
        [SerializeField] string displayName = "Guardián de la Piedra Viva";
        [SerializeField, Tooltip("Nombre en la barra del jefe (vacío la oculta).")] StringEventChannel bossNameChannel;
        [SerializeField] StringEventChannel messageChannel;
        [SerializeField, TextArea(3, 8)] string defeatText;

        [Header("Ritmo")]
        [SerializeField, Min(0f), Tooltip("Distancia a la que despierta.")] float engageRadius = 6f;
        [SerializeField, Min(0f)] float moveSpeed = 1.6f;
        [SerializeField, Min(0f), Tooltip("Segundos entre ataques (en la fase 3, menos).")] float attackCooldown = 1f;
        [SerializeField, Min(0f), Tooltip("Segundos de aviso del zarpazo; los demás ataques escalan desde aquí.")] float baseWindup = 0.6f;
        [SerializeField] float gravity = -20f;

        [Header("Daño")]
        [SerializeField, Min(1)] int heavyDamage = 20;
        [SerializeField, Min(1)] int lightDamage = 10;
        [SerializeField, Min(0f)] float chargeSpeed = 11f;
        [SerializeField, Min(0f)] float chargeLength = 8f;
        [SerializeField, Min(0f), Tooltip("Segundos aturdido si la embestida choca con algo.")] float chargeStun = 1.6f;
        [SerializeField, Min(0f)] float roarKnockback = 9f;
        [SerializeField, Min(0f)] float shardSpeed = 6f;

        CharacterController controller;
        Health health;
        Health targetHealth;
        PlayerMovement targetMovement;
        Vector3 planarVelocity;
        float verticalSpeed;
        float timer;
        int attacksDone;
        bool busy;
        bool charging; // la embestida mueve al Guardián por su cuenta
        SlowTracker slow;

        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public int Phase { get; private set; } = 1;
        public bool IsEngaged { get; private set; }
        public bool IsDefeated { get; private set; }
        public bool IsBusy => busy;
        public bool IsStunned { get; private set; }
        public TelegraphMarker CircleMarker => circleMarker;
        public TelegraphMarker LineMarker => lineMarker;
        public TelegraphMarker RingMarker => ringMarker;
        public TelegraphMarker[] FuryMarkers => furyMarkers;

        float WindupScale => Phase >= 3 ? 0.75f : 1f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            foreach (var shard in shards)
                shard.transform.SetParent(null, true); // vuelan por su cuenta
        }

        void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
            if (target != null)
            {
                targetHealth = target.GetComponentInParent<Health>();
                targetMovement = target.GetComponentInParent<PlayerMovement>();
            }
            SetPose(seatedPose);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            HideMarkers();
        }

        public void ApplySlow(float speedFactor) => slow.Apply(speedFactor);

        /// <summary>Lo despierta aunque el jugador no se haya acercado (lo usan las pruebas).</summary>
        public void Engage()
        {
            if (IsEngaged || IsDefeated)
                return;
            IsEngaged = true;
            timer = attackCooldown;
            SetPose(standPose);
            if (bossNameChannel != null)
                bossNameChannel.Raise(displayName);
        }

        /// <summary>Empieza ya el ataque dado, si no está en otro (lo usan las pruebas).</summary>
        public bool ForceAttack(GuardianAttack attack)
        {
            if (!IsEngaged || IsDefeated || busy)
                return false;
            StartCoroutine(Run(AttackRoutine(attack)));
            return true;
        }

        void Update()
        {
            if (IsDefeated)
                return;

            var toTarget = target != null ? Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up) : Vector3.zero;
            var targetAlive = target != null && (targetHealth == null || !targetHealth.IsDead);

            if (!IsEngaged)
            {
                if (targetAlive && toTarget.sqrMagnitude <= engageRadius * engageRadius)
                    Engage();
            }
            else if (!busy)
            {
                Stalk(toTarget, targetAlive);
            }

            if (!controller.enabled || charging)
                return;
            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, gravity, Time.deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        void Stalk(Vector3 toTarget, bool targetAlive)
        {
            if (!targetAlive)
            {
                planarVelocity = Vector3.zero;
                return;
            }

            var distance = toTarget.magnitude;
            if (distance > 0.001f)
                FacingDirection = toTarget / distance;
            planarVelocity = distance > 2.2f ? FacingDirection * (moveSpeed * slow.Multiplier) : Vector3.zero;
            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            StartCoroutine(Run(AttackRoutine(ChooseAttack(distance))));
        }

        GuardianAttack ChooseAttack(float distance)
        {
            var n = attacksDone + 1;
            if (Phase >= 3 && n % 4 == 0)
                return GuardianAttack.Fury;
            if (Phase >= 2 && n % 3 == 0)
                return n / 3 % 2 == 0 ? GuardianAttack.Shards : GuardianAttack.Waves;
            if (n % 5 == 0)
                return GuardianAttack.Roar;
            if (distance < 3f)
                return n % 2 == 0 ? GuardianAttack.Slam : GuardianAttack.Swipe;
            return Phase >= 2 && n % 2 == 0 ? GuardianAttack.Shards : GuardianAttack.Charge;
        }

        IEnumerator Run(IEnumerator routine)
        {
            busy = true;
            planarVelocity = Vector3.zero;
            yield return routine;
            attacksDone++;
            busy = false;
            timer = attackCooldown * (Phase >= 3 ? 0.7f : 1f);
            SetPose(standPose);
        }

        IEnumerator AttackRoutine(GuardianAttack attack) => attack switch
        {
            GuardianAttack.Swipe => Swipe(),
            GuardianAttack.Charge => Charge(),
            GuardianAttack.Slam => Slam(),
            GuardianAttack.Waves => Waves(),
            GuardianAttack.Shards => Shards(),
            GuardianAttack.Roar => Roar(),
            _ => Fury(),
        };

        // ---------- Ataques ----------

        IEnumerator Swipe()
        {
            FaceTarget();
            SetPose(slamPose);
            var windup = baseWindup * WindupScale;
            circleMarker.Show(Ground(transform.position + FacingDirection * 1.6f), 1.6f, windup);
            yield return new WaitForSeconds(windup);
            circleMarker.Hide();
            Hit(circleMarker, heavyDamage);
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Slam()
        {
            SetPose(slamPose);
            var windup = baseWindup * 1.7f * WindupScale;
            circleMarker.Show(Ground(transform.position), 3.5f, windup);
            yield return new WaitForSeconds(windup);
            circleMarker.Hide();
            Hit(circleMarker, heavyDamage);
            yield return new WaitForSeconds(0.7f);
        }

        IEnumerator Charge()
        {
            FaceTarget();
            SetPose(chargePose);
            var windup = baseWindup * 1.5f * WindupScale;
            lineMarker.ShowLine(Ground(transform.position), FacingDirection, chargeLength, 1.6f, windup);
            yield return new WaitForSeconds(windup);
            lineMarker.Hide();

            var hit = false;
            var travelled = 0f;
            charging = true;
            while (travelled < chargeLength)
            {
                var step = chargeSpeed * Time.deltaTime;
                var flags = controller.Move(FacingDirection * step + Vector3.down * 0.5f * Time.deltaTime);
                travelled += step;
                planarVelocity = FacingDirection * chargeSpeed;
                if (!hit && targetHealth != null && PlanarDistance(target.position) < 1.4f)
                    hit = targetHealth.TryDamage(heavyDamage, transform.position);
                if ((flags & CollisionFlags.Sides) != 0)
                {
                    // Contra una columna o el borde: queda aturdido, el momento de atacarlo.
                    planarVelocity = Vector3.zero;
                    charging = false;
                    IsStunned = true;
                    SetPose(seatedPose);
                    yield return new WaitForSeconds(chargeStun);
                    IsStunned = false;
                    yield break;
                }
                yield return null;
            }
            charging = false;
            planarVelocity = Vector3.zero;
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator Waves()
        {
            SetPose(roarPose);
            var windup = baseWindup * 1.3f * WindupScale;
            // Primero el anillo cercano y luego el lejano: hay que salir de uno sin entrar en el otro.
            foreach (var outer in new[] { 3.5f, 7f })
            {
                ringMarker.Show(Ground(transform.position), outer, windup);
                yield return new WaitForSeconds(windup);
                ringMarker.Hide();
                Hit(ringMarker, lightDamage);
                windup *= 0.85f;
            }
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Shards()
        {
            FaceTarget();
            SetPose(roarPose);
            yield return new WaitForSeconds(baseWindup * 1.2f * WindupScale);
            // Cinco fragmentos en abanico de 60° hacia el jugador.
            const int count = 5;
            var fired = 0;
            foreach (var shard in shards)
            {
                if (fired == count)
                    break;
                if (shard.IsFlying)
                    continue;
                var angle = -30f + 15f * fired;
                var direction = Quaternion.Euler(0f, angle, 0f) * FacingDirection;
                shard.Launch(transform.position + Vector3.up * 1f + direction * 0.8f, direction * shardSpeed, lightDamage, targetHealth, 3f);
                fired++;
            }
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator Roar()
        {
            SetPose(roarPose);
            var windup = baseWindup * 1.5f * WindupScale;
            circleMarker.Show(Ground(transform.position), 3f, windup);
            yield return new WaitForSeconds(windup);
            circleMarker.Hide();
            if (Hit(circleMarker, lightDamage) && targetMovement != null)
            {
                var away = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                targetMovement.AddImpulse((away.sqrMagnitude > 0.0001f ? away.normalized : -FacingDirection) * roarKnockback);
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Furia de la Primera Piedra: dos tandas de zonas de impacto, cada una con una sobre el jugador y las demás
        // alrededor, para que esquivar a ciegas no baste.
        IEnumerator Fury()
        {
            SetPose(roarPose);
            var perWave = Mathf.Max(1, furyMarkers.Length / 2);
            var windup = baseWindup * 1.7f;
            for (var wave = 0; wave < 2; wave++)
            {
                for (var i = 0; i < perWave; i++)
                {
                    var marker = furyMarkers[wave * perWave + i];
                    var angle = Random.value * 360f;
                    var offset = i == 0 ? Vector3.zero : Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(1.8f, 3.2f);
                    marker.Show(Ground(target.position + offset), 1.3f, windup);
                }
                yield return new WaitForSeconds(windup);
                for (var i = 0; i < perWave; i++)
                {
                    var marker = furyMarkers[wave * perWave + i];
                    marker.Hide();
                    Hit(marker, heavyDamage);
                }
                windup *= 0.85f;
            }
            yield return new WaitForSeconds(0.6f);
        }

        // ---------- Fases y derrota ----------

        void OnDamaged(Vector3 sourcePosition)
        {
            if (health.IsDead)
                return;
            var fraction = (float)health.Current / health.Max;
            var phase = fraction <= PhaseThreeFraction ? 3 : fraction <= PhaseTwoFraction ? 2 : 1;
            if (phase <= Phase)
                return;

            Phase = phase;
            if (!IsEngaged)
                Engage();
            StopAllCoroutines();
            HideMarkers();
            IsStunned = false;
            charging = false;
            StartCoroutine(Run(PhaseShift()));
        }

        IEnumerator PhaseShift()
        {
            SetPose(roarPose);
            health.GrantInvulnerability(1.2f);
            if (Phase >= 2 && flood != null)
                flood.SetActive(true);
            yield return new WaitForSeconds(1.2f);
        }

        void OnDied()
        {
            IsDefeated = true;
            StopAllCoroutines();
            busy = false;
            charging = false;
            planarVelocity = Vector3.zero;
            HideMarkers();
            if (flood != null)
                flood.SetActive(false);
            if (bossNameChannel != null)
                bossNameChannel.Raise(null);
            if (messageChannel != null && !string.IsNullOrEmpty(defeatText))
                messageChannel.Raise(defeatText);
        }

        // ---------- Utilidades ----------

        bool Hit(TelegraphMarker marker, int damage) =>
            targetHealth != null && !targetHealth.IsDead && marker.Contains(target.position) &&
            targetHealth.TryDamage(damage, transform.position);

        void FaceTarget()
        {
            if (target == null)
                return;
            var toTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
            if (toTarget.sqrMagnitude > 0.0001f)
                FacingDirection = toTarget.normalized;
        }

        float PlanarDistance(Vector3 point)
        {
            var offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        Vector3 Ground(Vector3 point) => new(point.x, transform.position.y, point.z);

        void HideMarkers()
        {
            if (circleMarker != null)
                circleMarker.Hide();
            if (lineMarker != null)
                lineMarker.Hide();
            if (ringMarker != null)
                ringMarker.Hide();
            foreach (var marker in furyMarkers)
                if (marker != null)
                    marker.Hide();
        }

        void SetPose(DirectionalSpriteSet pose)
        {
            if (spriteAnimator != null && pose != null)
                spriteAnimator.SpriteSet = pose;
        }
    }
}
