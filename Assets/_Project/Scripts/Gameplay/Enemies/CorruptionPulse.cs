using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Señal de corrupción de la Sombra del Olvido (Docs/Enemigos/Olmeca.md): el sprite late hacia un color ajeno al
    /// entorno con un ritmo irregular (dos ondas que no coinciden). Cambia SpriteRenderer.color, que el shader HD2D
    /// multiplica, así que no crea materiales ni choca con el destello de DamageFlash (_FlashAmount).
    /// </summary>
    public sealed class CorruptionPulse : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        [SerializeField] Color tint = new(0.78f, 0.62f, 1f);

        [SerializeField, Min(0.01f), Tooltip("Pulsos por segundo de la onda principal.")]
        float frequency = 0.9f;

        float phase;

        public SpriteRenderer SpriteRenderer
        {
            get => spriteRenderer;
            set => spriteRenderer = value;
        }

        // Cada enemigo empieza en otro punto del ciclo para que no laten a la vez.
        void Awake() => phase = Random.value * 10f;

        void OnDisable()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
        }

        void Update()
        {
            if (spriteRenderer == null)
                return;

            var t = Time.time * frequency + phase;
            var wave = 0.5f + 0.35f * Mathf.Sin(t * 2f * Mathf.PI) + 0.15f * Mathf.Sin(t * 5.3f + 1.7f);
            spriteRenderer.color = Color.Lerp(Color.white, tint, Mathf.Clamp01(wave));
        }
    }
}
