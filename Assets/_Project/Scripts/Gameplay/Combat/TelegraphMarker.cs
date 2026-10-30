using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Aviso de un ataque (GDD §10, Telegraph): un círculo plano en el suelo, dibujado sobre el suelo y bajo los
    /// personajes, que parpadea cada vez más rápido hasta el golpe. Se desprende de su dueño al empezar para no seguirlo
    /// (el salto del Jaguar avisa dónde caerá); queda en la misma escena y se descarga con ella. Contains comprueba si
    /// algo está dentro del círculo.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TelegraphMarker : MonoBehaviour
    {
        // El sprite del aviso mide 2 m de diámetro a escala 1.
        const float SpriteRadius = 1f;
        const float GroundOffset = 0.03f;

        [SerializeField] Color color = new(1f, 0.45f, 0.25f, 1f);

        SpriteRenderer spriteRenderer;
        float duration;
        float elapsed;
        float radius;

        public bool IsShowing => spriteRenderer.enabled;
        public float Radius => radius;
        public Vector3 Center { get; private set; }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
            transform.SetParent(null, true);
        }

        /// <summary>Muestra el aviso centrado en <paramref name="center"/> durante <paramref name="seconds"/>.</summary>
        public void Show(Vector3 center, float circleRadius, float seconds)
        {
            Center = center;
            radius = circleRadius;
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            transform.SetPositionAndRotation(Center + Vector3.up * GroundOffset, Quaternion.Euler(90f, 0f, 0f));
            transform.localScale = Vector3.one * (circleRadius / SpriteRadius);
            spriteRenderer.color = color;
            spriteRenderer.enabled = true;
            enabled = true;
        }

        public void Hide()
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
            enabled = false;
        }

        /// <summary>Si el punto está dentro del círculo, en el plano (la altura no cuenta).</summary>
        public bool Contains(Vector3 point)
        {
            var offset = point - Center;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            // Parpadeo que se acelera: de 4 a 14 cambios por segundo.
            var progress = Mathf.Clamp01(elapsed / duration);
            var frequency = Mathf.Lerp(4f, 14f, progress);
            var bright = Mathf.Repeat(elapsed * frequency, 1f) < 0.5f;
            spriteRenderer.color = bright ? Color.Lerp(color, Color.white, 0.35f) : color;
        }
    }
}
