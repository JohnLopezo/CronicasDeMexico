using System;
using CronicasDeMexico.Core.Feedback;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Enemigo que puede esquivar de un salto (EnemyJumpDodge): dice cuándo puede y hace el salto vertical (a pie) o la
    /// subida (voladores). El empujón hacia atrás lo mueve EnemyJumpDodge, porque los jefes reescriben su velocidad cada frame.
    /// </summary>
    public interface IJumpDodger
    {
        /// <summary>False en la espera de su ataque, atacando, aturdido o muerto (los jefes, solo al acecho).</summary>
        bool CanDodgeJump { get; }

        bool Flies { get; }

        /// <summary>A pie: velocidad vertical del salto. Voladores: subir a su altura de defensa.</summary>
        void DodgeJump(float upSpeed);
    }

    /// <summary>
    /// Esquiva saltando, de la versión HTML (PORTAR_A_UNITY §1): a veces un golpe del jugador no entra; sale «Esquiva», no
    /// hay daño ni pausa de impacto (PlayerAttack solo la pide si algo recibió daño). Se engancha en Health.Dodge, que solo
    /// miran los golpes del jugador (ataque, especial, Tierra). No esquiva en la espera, en el aire ni con la ruptura rota.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class EnemyJumpDodge : MonoBehaviour
    {
        public const float WalkerChance = 0.25f;
        public const float FlyerChance = 0.35f;
        public const float Cooldown = 3f;
        public const float WalkerInvulnerable = 0.4f;
        public const float FlyerInvulnerable = 0.5f;
        public const float UpSpeed = 5f; // sube ~0,6 m con la gravedad de −20
        public const float PushSpeed = 6f;
        const float PushDeceleration = 30f; // 6 m/s → 0 en 0,2 s: unos 0,6 m

        /// <summary>Tirada de 0 a 1; esquiva si sale por debajo de la probabilidad. Las pruebas la fijan.</summary>
        public static Func<float> Roll = DefaultRoll;

        static float DefaultRoll() => UnityEngine.Random.value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Roll = DefaultRoll;

        IJumpDodger owner;
        CharacterController controller;
        Health health;
        float readyAt = float.NegativeInfinity;
        Vector3 push;

        /// <summary>Lo llama cada enemigo en su Awake (como BreakGauge.Ensure): no hay que tocar prefabs ni escenas.</summary>
        public static EnemyJumpDodge Ensure(GameObject enemy) =>
            enemy.TryGetComponent(out EnemyJumpDodge dodge) ? dodge : enemy.AddComponent<EnemyJumpDodge>();

        public bool IsCoolingDown => Time.time < readyAt;

        void Awake()
        {
            owner = GetComponent<IJumpDodger>();
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.Dodge = TryDodge;
        }

        void OnDestroy()
        {
            if (health != null)
                health.Dodge = null;
        }

        bool TryDodge(Vector3 source)
        {
            if (owner == null || IsCoolingDown || !owner.CanDodgeJump)
                return false;
            if (!owner.Flies && controller != null && !controller.isGrounded)
                return false;
            if (TryGetComponent(out BreakGauge gauge) && gauge.IsBroken)
                return false;
            if (Roll() >= (owner.Flies ? FlyerChance : WalkerChance))
                return false;

            readyAt = Time.time + Cooldown;
            health.GrantInvulnerability(owner.Flies ? FlyerInvulnerable : WalkerInvulnerable);
            owner.DodgeJump(UpSpeed);
            if (!owner.Flies)
            {
                var away = transform.position - source;
                away.y = 0f;
                push = (away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward) * PushSpeed;
            }
            DamageNumbers.Report(transform.position + Vector3.up * 1.4f, 0, DamageKind.Dodged);
            return true;
        }

        void Update()
        {
            if (push == Vector3.zero || controller == null || !controller.enabled)
                return;
            controller.Move(push * Time.deltaTime);
            push = Vector3.MoveTowards(push, Vector3.zero, PushDeceleration * Time.deltaTime);
        }
    }
}
