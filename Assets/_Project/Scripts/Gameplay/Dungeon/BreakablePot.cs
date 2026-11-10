using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Vasija rompible: tiene 1 de vida en el layer Enemigo, así que el ataque y el golpe de tierra la rompen como a un
    /// enemigo. Al romperse muestra los tiestos y deja de estorbar; el botín lo da EnemyReward (BOT_vasija). Vuelve al
    /// recargar el piso, como los enemigos de la mazmorra.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class BreakablePot : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite brokenSprite;

        Health health;

        public bool IsBroken => health != null && health.IsDead;

        public void Configure(SpriteRenderer renderer, Sprite broken)
        {
            spriteRenderer = renderer;
            brokenSprite = broken;
        }

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnBroken;

        void OnDisable() => health.Died -= OnBroken;

        void OnBroken()
        {
            if (spriteRenderer != null && brokenSprite != null)
                spriteRenderer.sprite = brokenSprite;
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }
    }
}
