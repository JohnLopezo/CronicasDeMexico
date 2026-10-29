using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Pone el sprite en blanco un instante al recibir daño. Usa la propiedad _FlashAmount del shader
    /// HD2D Sprite Lit con un MaterialPropertyBlock: no crea materiales nuevos.
    /// </summary>
    public sealed class DamageFlash : MonoBehaviour
    {
        static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        [SerializeField] Health health;
        [SerializeField] SpriteRenderer spriteRenderer;

        [SerializeField, Min(0.01f), Tooltip("Duración del destello en segundos.")]
        float duration = 0.12f;

        MaterialPropertyBlock block;
        float timer;

        void Awake() => block = new MaterialPropertyBlock();

        void OnEnable()
        {
            if (health != null)
                health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            timer = 0f;
            SetFlash(0f);
        }

        void Update()
        {
            if (timer <= 0f)
                return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
                SetFlash(0f);
        }

        void OnDamaged(Vector3 sourcePosition)
        {
            timer = duration;
            SetFlash(1f);
        }

        void SetFlash(float amount)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.GetPropertyBlock(block);
            block.SetFloat(FlashAmountId, amount);
            spriteRenderer.SetPropertyBlock(block);
        }
    }
}
