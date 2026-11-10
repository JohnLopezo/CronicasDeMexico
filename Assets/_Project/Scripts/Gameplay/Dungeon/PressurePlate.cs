using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Placa de presión: al pisarla el jugador se hunde, se enciende su glifo y activa las trampas enlazadas (SpikeTrap).
    /// Vuelve a subir un momento después de que nadie la pise. Como SpikeTrap, la presencia se mira con OnTriggerStay.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PressurePlate : MonoBehaviour
    {
        const float ReleaseDelay = 0.4f;

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite upSprite;
        [SerializeField] Sprite pressedSprite;
        [SerializeField] SpikeTrap[] traps;

        float lastStay = float.NegativeInfinity;

        public bool IsPressed { get; private set; }

        public void Configure(SpriteRenderer renderer, Sprite up, Sprite pressed, SpikeTrap[] linkedTraps)
        {
            spriteRenderer = renderer;
            upSprite = up;
            pressedSprite = pressed;
            traps = linkedTraps;
        }

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            lastStay = Time.time;
            if (IsPressed)
                return;
            IsPressed = true;
            SetSprite(pressedSprite);
            if (traps != null)
                foreach (var trap in traps)
                    if (trap != null)
                        trap.Trigger();
        }

        void Update()
        {
            if (IsPressed && Time.time - lastStay > ReleaseDelay)
            {
                IsPressed = false;
                SetSprite(upSprite);
            }
        }

        void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null && sprite != null)
                spriteRenderer.sprite = sprite;
        }
    }
}
