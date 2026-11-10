using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Trampa de pinchos de la mazmorra (Docs/Mazmorras/Olmeca.md, lámina «Mazmorras ancestrales»): bajada no hace nada;
    /// antes de subir avisa (la losa se tiñe y parpadea) y, levantada, daña a quien esté encima. Sube sola cada cierto
    /// tiempo (periódica) o cuando la activa una placa de presión (Trigger). El daño lo dosifica la invulnerabilidad del
    /// golpe; la presencia se mira con OnTriggerStay, no con salidas (como SlowZone: el teletransporte no las avisa).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SpikeTrap : MonoBehaviour
    {
        public enum State { Lowered, Warning, Raised }

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite loweredSprite;
        [SerializeField] Sprite raisedSprite;

        [SerializeField, Tooltip("Sube sola cada period segundos; si no, solo con Trigger (placa de presión).")]
        bool periodic = true;
        [SerializeField, Min(0.5f)] float period = 3.5f;
        [SerializeField, Min(0f), Tooltip("Desfase del ciclo, para que las trampas vecinas no suban a la vez.")] float offset;
        [SerializeField, Min(0f), Tooltip("Segundos de aviso antes de subir.")] float warning = 0.7f;
        [SerializeField, Min(0f)] float raisedTime = 1.1f;
        [SerializeField, Min(1)] int damage = 8;
        [SerializeField] Color warningTint = new(1f, 0.55f, 0.35f, 1f);

        float timer;

        public State Current { get; private set; }

        public void Configure(SpriteRenderer renderer, Sprite lowered, Sprite raised, bool isPeriodic, float cycleOffset)
        {
            spriteRenderer = renderer;
            loweredSprite = lowered;
            raisedSprite = raised;
            periodic = isPeriodic;
            offset = cycleOffset;
        }

        void OnEnable()
        {
            Lower();
            timer = periodic ? period - warning - raisedTime + offset : 0f;
        }

        /// <summary>Empieza el aviso si está bajada (placa de presión).</summary>
        public void Trigger()
        {
            if (Current == State.Lowered)
                BeginWarning();
        }

        void Update()
        {
            timer -= Time.deltaTime;
            switch (Current)
            {
                case State.Lowered:
                    if (periodic && timer <= 0f)
                        BeginWarning();
                    break;
                case State.Warning:
                    // Parpadeo del aviso: legible sin depender solo del color (cambia también la intensidad).
                    if (spriteRenderer != null)
                        spriteRenderer.color = Color.Lerp(Color.white, warningTint, Mathf.PingPong(Time.time * 6f, 1f));
                    if (timer <= 0f)
                        Raise();
                    break;
                case State.Raised:
                    if (timer <= 0f)
                    {
                        Lower();
                        timer = periodic ? period - warning - raisedTime : 0f;
                    }
                    break;
            }
        }

        void BeginWarning()
        {
            Current = State.Warning;
            timer = warning;
        }

        void Raise()
        {
            Current = State.Raised;
            timer = raisedTime;
            SetLook(raisedSprite);
        }

        void Lower()
        {
            Current = State.Lowered;
            SetLook(loweredSprite);
        }

        void SetLook(Sprite sprite)
        {
            if (spriteRenderer == null)
                return;
            if (sprite != null)
                spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
        }

        void OnTriggerStay(Collider other)
        {
            if (Current != State.Raised || !other.CompareTag("Player"))
                return;
            var health = other.GetComponentInParent<Health>();
            if (health != null)
                health.TryDamage(damage, transform.position);
        }
    }
}
