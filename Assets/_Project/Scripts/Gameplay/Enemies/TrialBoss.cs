using System.Collections;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Jefe de la arena de pruebas guiado por BossData (Docs/Enemigos/Jefes_Prueba.md): duerme hasta que el jugador se
    /// acerca; despierto, camina hacia él y usa sus habilidades (BossAbilityKind) según la fase, la distancia y su espera.
    /// Las fases dependen de la vida; al cambiar queda invulnerable un momento. Todo golpe se avisa en el suelo con
    /// TelegraphMarker, como el Guardián de la Piedra Viva, y los golpes del jugador no lo interrumpen. Con
    /// resurrectFraction, la primera vez que cae se levanta con esa parte de la vida.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class TrialBoss : MonoBehaviour, IPlanarMotion
    {
        const float Gravity = -20f;
        const float Acceleration = 10f;
        const float StopDistance = 1.4f;
        const float ChargeSpeed = 9f;
        const float RecoverTime = 0.6f;
        const float PhaseInvulnerability = 1f;
        const float LeapAirTime = 0.35f;
        const float RainSpread = 2.5f;
        const float SummonDistance = 1.6f;
        const float ResurrectDelay = 1.5f;
        const float DefeatLinger = 1.2f;
        const float FallenAlpha = 0.45f;

        [SerializeField] BossData data;

        [SerializeField, Tooltip("Si está vacío se usa el objeto con la etiqueta Player al iniciar.")]
        Transform target;

        [SerializeField, Tooltip("Círculos de aviso: el primero para golpes y ondas; todos para la lluvia.")]
        TelegraphMarker[] circles;

        [SerializeField] TelegraphMarker line;
        [SerializeField] Projectile[] projectiles;
        [SerializeField, Tooltip("Reserva inactiva para Summon (enemigos de la lámina).")] GameObject[] minions;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] DirectionalSpriteAnimator spriteAnimator;
        [SerializeField, Tooltip("Nombre en la barra del jefe (null la oculta).")] StringEventChannel bossNameChannel;

        CharacterController controller;
        Health health;
        Health targetHealth;
        ISlowable targetSlowable;
        float[] cooldowns;
        Vector3 planarVelocity;
        float verticalSpeed;
        float slowTimer;
        float slowFactor = 1f;
        bool resurrected;

        public BossData Data
        {
            get => data;
            set => data = value;
        }

        public int Phase { get; private set; } = 1;
        public bool IsEngaged { get; private set; }
        public bool IsDefeated { get; private set; }
        public bool IsBusy { get; private set; }

        /// <summary>Habilidad en curso (null si ninguna).</summary>
        public BossAbility Current { get; private set; }

        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 FacingDirection { get; private set; } = Vector3.back;
        public float CruiseSpeed => data != null ? data.moveSpeed : 0f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            if (data != null)
            {
                health.SetMaxHealth(data.maxHealth);
                cooldowns = new float[data.abilities.Length];
            }
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
            StopAllCoroutines();
            HideMarkers();
            IsBusy = false;
            Current = null;
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
                targetSlowable = target.GetComponentInParent<ISlowable>();
            }
        }

        /// <summary>Lo despierta aunque el jugador no se haya acercado (selector de la arena y pruebas).</summary>
        public void Engage()
        {
            if (IsEngaged || IsDefeated || data == null)
                return;
            IsEngaged = true;
            if (bossNameChannel != null)
                bossNameChannel.Raise(data.displayName);
        }

        /// <summary>Usa ya la habilidad dada, si no está en otra (pruebas).</summary>
        public bool Force(int ability)
        {
            if (IsBusy || IsDefeated || data == null || ability < 0 || ability >= data.abilities.Length)
                return false;
            Engage();
            StartCoroutine(Run(ability));
            return true;
        }

        /// <summary>Vuelve a empezar el combate en <paramref name="position"/>, dormido y con la vida completa.</summary>
        public void ResetBoss(Vector3 position)
        {
            StopAllCoroutines();
            HideMarkers();
            if (health.IsDead)
                health.ReviveFull();
            else
                health.Heal(health.Max - health.Current, HealSource.Ability); // la arena de pruebas lo restaura entero
            health.DamageFactor = 1f;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            Phase = 1;
            IsEngaged = IsDefeated = IsBusy = resurrected = false;
            Current = null;
            slowTimer = 0f;
            if (cooldowns != null)
                System.Array.Clear(cooldowns, 0, cooldowns.Length);
            SetAlpha(1f);
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            if (spriteAnimator != null)
                spriteAnimator.Stop();
            if (minions != null)
                foreach (var minion in minions)
                    if (minion != null)
                        minion.SetActive(false);
        }

        void Update()
        {
            if (data == null || IsDefeated || health.IsDead)
                return;
            var deltaTime = Time.deltaTime;
            if (!IsEngaged)
            {
                if (target != null && Planar(target.position - transform.position).magnitude <= data.engageRadius)
                    Engage();
                return;
            }

            if (slowTimer > 0f)
            {
                slowTimer -= deltaTime;
                targetSlowable?.ApplySlow(slowFactor);
            }
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] -= deltaTime;

            var desired = Vector3.zero;
            if (!IsBusy && target != null && (targetHealth == null || !targetHealth.IsDead))
            {
                var toTarget = Planar(target.position - transform.position);
                var distance = toTarget.magnitude;
                if (distance > 0.001f)
                    FacingDirection = toTarget / distance;
                var chosen = Choose(distance);
                if (chosen >= 0)
                    StartCoroutine(Run(chosen));
                else if (distance > StopDistance)
                    desired = FacingDirection * data.moveSpeed;
            }
            if (!IsBusy || Current is not { kind: BossAbilityKind.Charge })
                planarVelocity = Vector3.MoveTowards(planarVelocity, desired, Acceleration * deltaTime);
            verticalSpeed = PlanarMovement.ApplyGravity(verticalSpeed, controller.isGrounded, Gravity, deltaTime);
            if (controller.enabled)
                controller.Move((planarVelocity + Vector3.up * verticalSpeed) * deltaTime);
        }

        // La primera que pueda usar, empezando por una al azar: así no repite siempre el mismo orden.
        int Choose(float distance)
        {
            var count = data.abilities.Length;
            if (count == 0)
                return -1;
            var start = Random.Range(0, count);
            for (var k = 0; k < count; k++)
            {
                var i = (start + k) % count;
                var ability = data.abilities[i];
                if (ability.minPhase <= Phase && cooldowns[i] <= 0f && distance <= ability.range)
                    return i;
            }
            return -1;
        }

        IEnumerator Run(int index)
        {
            var ability = data.abilities[index];
            IsBusy = true;
            Current = ability;
            planarVelocity = Vector3.zero;
            var pose = ability.kind is BossAbilityKind.Rain or BossAbilityKind.Nova or BossAbilityKind.Summon or BossAbilityKind.Shield
                ? data.special : data.attack;
            Play(pose);

            switch (ability.kind)
            {
                case BossAbilityKind.Swipe:
                    for (var k = 0; k < ability.count; k++)
                    {
                        Aim();
                        var center = transform.position + FacingDirection * (ability.radius * 0.8f);
                        ShowCircle(0, center, ability.radius, ability.windup);
                        yield return new WaitForSeconds(ability.windup);
                        HitIfWithin(center, ability.radius, ability.damage);
                    }
                    break;
                case BossAbilityKind.Beam:
                {
                    Aim();
                    var origin = transform.position;
                    var direction = FacingDirection;
                    if (line != null)
                        line.ShowLine(origin, direction, ability.length, ability.radius, ability.windup);
                    yield return new WaitForSeconds(ability.windup);
                    HideLine();
                    if (InLine(origin, direction, ability.length, ability.radius * 0.5f))
                        Hit(ability.damage);
                    break;
                }
                case BossAbilityKind.Charge:
                {
                    Aim();
                    var direction = FacingDirection;
                    if (line != null)
                        line.ShowLine(transform.position, direction, ability.length, ability.radius, ability.windup);
                    yield return new WaitForSeconds(ability.windup);
                    HideLine();
                    var hit = false;
                    for (var t = 0f; t < ability.length / ChargeSpeed; t += Time.deltaTime)
                    {
                        planarVelocity = direction * ChargeSpeed;
                        if (!hit && target != null && Planar(target.position - transform.position).magnitude <= data.bodyRadius + ability.radius * 0.5f)
                            hit = Hit(ability.damage);
                        yield return null;
                    }
                    planarVelocity = Vector3.zero;
                    break;
                }
                case BossAbilityKind.Leap:
                {
                    var landing = target != null ? target.position : transform.position;
                    ShowCircle(0, landing, ability.radius, ability.windup);
                    yield return new WaitForSeconds(ability.windup);
                    // En el aire no se le puede golpear: no hay sprite de vuelo, así que se desvanece y cae.
                    health.GrantInvulnerability(LeapAirTime + 0.2f);
                    SetAlpha(FallenAlpha);
                    yield return new WaitForSeconds(LeapAirTime);
                    MoveTo(landing);
                    SetAlpha(1f);
                    HitIfWithin(landing, ability.radius, ability.damage);
                    break;
                }
                case BossAbilityKind.Rain:
                {
                    var drops = Mathf.Min(ability.count, circles?.Length ?? 0);
                    var centers = new Vector3[drops];
                    for (var k = 0; k < drops; k++)
                    {
                        var around = target != null ? target.position : transform.position;
                        // La primera cae donde está el objetivo: obliga a moverse.
                        centers[k] = k == 0 ? around : around + Planar(Random.insideUnitSphere) * RainSpread;
                        ShowCircle(k, centers[k], ability.radius, ability.windup);
                    }
                    yield return new WaitForSeconds(ability.windup);
                    foreach (var center in centers)
                        if (HitIfWithin(center, ability.radius, ability.damage))
                            break; // una gota por lluvia: el jugador es invulnerable tras el golpe
                    break;
                }
                case BossAbilityKind.Nova:
                    ShowCircle(0, transform.position, ability.radius, ability.windup);
                    yield return new WaitForSeconds(ability.windup);
                    if (HitIfWithin(transform.position, ability.radius, ability.damage) && ability.factor < 1f)
                    {
                        slowFactor = ability.factor;
                        slowTimer = ability.seconds;
                    }
                    break;
                case BossAbilityKind.Volley:
                    Aim();
                    yield return new WaitForSeconds(ability.windup);
                    Volley(ability);
                    break;
                case BossAbilityKind.Summon:
                    yield return new WaitForSeconds(ability.windup);
                    Summon(ability.count);
                    break;
                case BossAbilityKind.Shield:
                    health.DamageFactor = ability.factor;
                    if (ability.factor <= 0f)
                        health.GrantInvulnerability(ability.seconds);
                    SetAlpha(0.7f);
                    yield return new WaitForSeconds(ability.seconds);
                    health.DamageFactor = 1f;
                    SetAlpha(1f);
                    break;
            }

            cooldowns[index] = ability.cooldown;
            Stop(pose);
            yield return new WaitForSeconds(RecoverTime);
            IsBusy = false;
            Current = null;
        }

        void Aim()
        {
            if (target == null)
                return;
            var toTarget = Planar(target.position - transform.position);
            if (toTarget.sqrMagnitude > 0.0001f)
                FacingDirection = toTarget.normalized;
        }

        void Volley(BossAbility ability)
        {
            if (projectiles == null)
                return;
            var full = ability.spread >= 359f;
            var step = ability.count > 1 ? (full ? 360f / ability.count : ability.spread / (ability.count - 1)) : 0f;
            var first = full ? 0f : -ability.spread * 0.5f;
            var launched = 0;
            foreach (var projectile in projectiles)
            {
                if (launched >= ability.count)
                    break;
                if (projectile == null || projectile.IsFlying)
                    continue;
                var direction = Quaternion.Euler(0f, first + step * launched, 0f) * FacingDirection;
                var speed = Mathf.Max(0.1f, ability.length);
                projectile.Launch(transform.position + Vector3.up * 0.8f, direction * speed, ability.damage, targetHealth, (ability.range + 3f) / speed);
                launched++;
            }
        }

        void Summon(int count)
        {
            if (minions == null)
                return;
            var summoned = 0;
            foreach (var minion in minions)
            {
                if (summoned >= count)
                    break;
                if (minion == null || minion.activeSelf)
                    continue;
                var angle = 360f * summoned / Mathf.Max(1, count);
                minion.transform.position = transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * SummonDistance;
                minion.SetActive(true);
                summoned++;
            }
        }

        // ---------- Golpes ----------

        bool Hit(int damage) => damage > 0 && targetHealth != null && targetHealth.TryDamage(damage, transform.position);

        bool HitIfWithin(Vector3 center, float radius, int damage) =>
            target != null && Planar(target.position - center).magnitude <= radius && Hit(damage);

        bool InLine(Vector3 origin, Vector3 direction, float length, float halfWidth)
        {
            if (target == null)
                return false;
            var offset = Planar(target.position - origin);
            var along = Vector3.Dot(offset, direction);
            return along >= 0f && along <= length && (offset - direction * along).magnitude <= halfWidth;
        }

        // ---------- Vida, fases y caída ----------

        void OnDamaged(Vector3 source)
        {
            if (data == null || health.IsDead)
                return;
            Engage();
            var fraction = (float)health.Current / health.Max;
            var phase = 1;
            foreach (var threshold in data.phaseThresholds)
                if (fraction <= threshold)
                    phase++;
            if (phase <= Phase)
                return;
            Phase = phase;
            health.GrantInvulnerability(PhaseInvulnerability);
        }

        void OnDied()
        {
            StopAllCoroutines();
            HideMarkers();
            IsBusy = false;
            Current = null;
            health.DamageFactor = 1f;
            planarVelocity = Vector3.zero;
            if (data != null && data.resurrectFraction > 0f && !resurrected)
            {
                resurrected = true;
                StartCoroutine(Resurrect());
                return;
            }
            IsDefeated = true;
            controller.enabled = false;
            if (bossNameChannel != null)
                bossNameChannel.Raise(null);
            StartCoroutine(Vanish());
        }

        IEnumerator Resurrect()
        {
            SetAlpha(FallenAlpha);
            yield return new WaitForSeconds(ResurrectDelay);
            health.ReviveFull();
            health.CarryOver(Mathf.CeilToInt(health.Max * data.resurrectFraction));
            health.GrantInvulnerability(PhaseInvulnerability);
            Phase = 1 + data.phaseThresholds.Length; // vuelve en su última fase
            SetAlpha(1f);
        }

        IEnumerator Vanish()
        {
            SetAlpha(FallenAlpha);
            yield return new WaitForSeconds(DefeatLinger);
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
        }

        // ---------- Utilidades ----------

        void MoveTo(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }

        void ShowCircle(int index, Vector3 center, float radius, float seconds)
        {
            if (circles != null && index < circles.Length && circles[index] != null)
                circles[index].Show(center, radius, seconds);
        }

        void HideLine()
        {
            if (line != null)
                line.Hide();
        }

        void HideMarkers()
        {
            HideLine();
            if (circles != null)
                foreach (var circle in circles)
                    if (circle != null)
                        circle.Hide();
        }

        void SetAlpha(float alpha)
        {
            if (spriteRenderer == null)
                return;
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        void Play(DirectionalSpriteSet set)
        {
            if (spriteAnimator != null && set != null)
                spriteAnimator.Play(set, hold: true);
        }

        void Stop(DirectionalSpriteSet set)
        {
            if (spriteAnimator != null && set != null)
                spriteAnimator.Stop(set);
        }

        static Vector3 Planar(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);
    }
}
