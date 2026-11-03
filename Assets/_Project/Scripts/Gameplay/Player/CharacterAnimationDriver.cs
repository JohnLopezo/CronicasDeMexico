using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Traduce lo que pasa en el juego a animaciones de acción de DirectionalSpriteAnimator (caminar y reposo los elige
    /// el animador solo): zarpazo al atacar (PlayerAttack), galope al esquivar (PlayerMovement), agazapado y salto
    /// del jaguar corrupto (EnemyChaser), golpe al recibir daño y caída al morir (Health). Las animaciones que falten
    /// se omiten, así que sirve para cualquier personaje.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        [SerializeField] DirectionalSpriteAnimator animator;
        [SerializeField, Tooltip("Al atacar (PlayerAttack).")] DirectionalSpriteSet attack;
        [SerializeField, Tooltip("Mientras esquiva (PlayerMovement).")] DirectionalSpriteSet run;
        [SerializeField, Tooltip("Mientras avisa el salto (EnemyChaser).")] DirectionalSpriteSet crouch;
        [SerializeField, Tooltip("Mientras salta (EnemyChaser).")] DirectionalSpriteSet leap;
        [SerializeField, Tooltip("Al recibir daño.")] DirectionalSpriteSet hit;
        [SerializeField, Tooltip("Al morir; se queda en el último cuadro.")] DirectionalSpriteSet death;

        Health health;
        PlayerAttack playerAttack;
        PlayerMovement playerMovement;
        EnemyChaser chaser;
        bool wasDead;

        public void Configure(DirectionalSpriteAnimator spriteAnimator, DirectionalSpriteSet attackSet, DirectionalSpriteSet runSet,
            DirectionalSpriteSet crouchSet, DirectionalSpriteSet leapSet, DirectionalSpriteSet hitSet, DirectionalSpriteSet deathSet)
        {
            animator = spriteAnimator;
            attack = attackSet;
            run = runSet;
            crouch = crouchSet;
            leap = leapSet;
            hit = hitSet;
            death = deathSet;
        }

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<DirectionalSpriteAnimator>();
            health = GetComponent<Health>();
            playerAttack = GetComponent<PlayerAttack>();
            playerMovement = GetComponent<PlayerMovement>();
            chaser = GetComponent<EnemyChaser>();
        }

        void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
            if (playerAttack != null)
                playerAttack.Struck += OnStruck;
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
            if (playerAttack != null)
                playerAttack.Struck -= OnStruck;
        }

        void Update()
        {
            if (animator == null)
                return;
            var dead = health != null && health.IsDead;
            if (wasDead && !dead)
                animator.Stop(); // revivió (reaparición o santuario)
            wasDead = dead;
            if (dead)
                return;

            Hold(run, playerMovement != null && playerMovement.IsDodging);
            Hold(crouch, chaser != null && chaser.Lunge == EnemyChaser.LungePhase.Windup);
            Hold(leap, chaser != null && chaser.Lunge == EnemyChaser.LungePhase.Leap);
        }

        // Animación en bucle mientras dure el estado.
        void Hold(DirectionalSpriteSet set, bool active)
        {
            if (set == null)
                return;
            if (active)
                animator.Play(set, loop: true);
            else
                animator.Stop(set);
        }

        void OnStruck()
        {
            if (animator != null && (health == null || !health.IsDead))
                animator.Play(attack);
        }

        void OnDamaged(Vector3 source)
        {
            if (animator != null && !health.IsDead)
                animator.Play(hit);
        }

        void OnDied()
        {
            if (animator != null)
                animator.Play(death, hold: true);
        }
    }
}
