using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Data.Characters;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Sistema de Ruptura (Docs/Sistemas/Ruptura.md). El enemigo tiene un escudo de puntos y debilidades de afinidad.
    /// Cada golpe del jugador con una afinidad débil quita un punto, y un bloqueo perfecto también (Chip). Sin
    /// escudo queda roto: aturdido (si es IStaggerable) y recibiendo más daño unos segundos; después el escudo
    /// vuelve entero. El HUD muestra el del último enemigo golpeado (BreakReadout).
    /// Se añade en tiempo de ejecución (Ensure) desde cada enemigo, así no hay que regenerar escenas ni prefabs.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class BreakGauge : MonoBehaviour
    {
        const float ReadoutSeconds = 4f;

        string displayName;
        Affinity[] weaknesses = System.Array.Empty<Affinity>();
        string[] weaknessNames = System.Array.Empty<string>();
        int maxShield = 1;
        float breakSeconds = 2.5f;
        float brokenDamageFactor = 1.5f;

        Health health;
        IStaggerable staggerable;
        int shield;
        float brokenUntil;
        float readoutUntil;

        public int Shield => shield;
        public int MaxShield => maxShield;
        public bool IsBroken => brokenUntil > 0f;
        public string DisplayName => displayName;

        /// <summary>Se rompió el escudo.</summary>
        public event System.Action Broke;

        /// <summary>Añade (o reconfigura) el medidor de ruptura del enemigo. Sin debilidades o con escudo 0 no hace nada.</summary>
        public static BreakGauge Ensure(GameObject enemy, string name, int shieldPoints, float brokenSeconds, params Affinity[] weakTo)
        {
            if (shieldPoints <= 0 || weakTo == null || weakTo.Length == 0)
                return null;
            var gauge = enemy.TryGetComponent(out BreakGauge existing) ? existing : enemy.AddComponent<BreakGauge>();
            gauge.Configure(name, shieldPoints, brokenSeconds, weakTo);
            return gauge;
        }

        public void Configure(string name, int shieldPoints, float brokenSeconds, Affinity[] weakTo)
        {
            displayName = name;
            maxShield = Mathf.Max(1, shieldPoints);
            breakSeconds = Mathf.Max(0.1f, brokenSeconds);
            weaknesses = weakTo;
            weaknessNames = new string[weakTo.Length];
            for (var i = 0; i < weakTo.Length; i++)
                weaknessNames[i] = weakTo[i].ToString();
            shield = maxShield;
        }

        void Awake()
        {
            health = GetComponent<Health>();
            staggerable = GetComponent<IStaggerable>();
            shield = maxShield;
        }

        void OnDisable()
        {
            Recover();
            BreakReadout.Hide(this);
        }

        public bool IsWeakTo(Affinity affinity) => System.Array.IndexOf(weaknesses, affinity) >= 0;

        /// <summary>
        /// El jugador acertó un golpe con esta afinidad (null: sin afinidad). Quita un punto si es una debilidad y
        /// muestra el escudo en el HUD. Devuelve si quitó un punto.
        /// </summary>
        public bool RegisterPlayerHit(Affinity? element)
        {
            var chipped = element.HasValue && IsWeakTo(element.Value) && Chip(1);
            if (!chipped)
                Publish();
            return chipped;
        }

        /// <summary>Muestra su escudo en el HUD sin golpearlo (al fijarlo como objetivo).</summary>
        public void ShowReadout() => Publish();

        /// <summary>Quita puntos del escudo (golpe débil, bloqueo perfecto). Devuelve si quitó alguno.</summary>
        public bool Chip(int points)
        {
            if (points <= 0 || IsBroken || health.IsDead)
                return false;
            shield = Mathf.Max(0, shield - points);
            if (shield == 0)
                Break();
            Publish();
            return true;
        }

        void Break()
        {
            brokenUntil = Time.time + breakSeconds;
            health.Vulnerability = brokenDamageFactor;
            staggerable?.Stagger(breakSeconds);
            TimeDilation.Request(0.05f, 0.12f);
            ScreenShake.Request(0.15f, 0.2f);
            Broke?.Invoke();
        }

        void Recover()
        {
            brokenUntil = 0f;
            shield = maxShield;
            if (health != null)
                health.Vulnerability = 1f;
        }

        void Update()
        {
            if (IsBroken && Time.time >= brokenUntil)
            {
                Recover();
                if (BreakReadout.IsShowing(this))
                    Publish();
            }

            // Fijado como objetivo, el panel no caduca.
            if (BreakReadout.IsShowing(this) && (health.IsDead || (Time.time >= readoutUntil && LockOnMarker.Target != transform)))
                BreakReadout.Hide(this);
        }

        void Publish()
        {
            if (health.IsDead)
            {
                BreakReadout.Hide(this);
                return;
            }
            readoutUntil = Time.time + ReadoutSeconds;
            BreakReadout.Show(this, new BreakInfo(displayName, shield, maxShield, weaknessNames, IsBroken));
        }
    }
}
