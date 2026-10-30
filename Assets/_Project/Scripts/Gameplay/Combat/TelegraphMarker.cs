using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Aviso de un ataque (GDD §10, Telegraph): una forma plana en el suelo, dibujada sobre el suelo y bajo los
    /// personajes, que parpadea cada vez más rápido hasta el golpe. Tres formas, cada una con su sprite:
    /// círculo (zarpazos, saltos, golpes de área), anillo (las ondas del Guardián: el centro queda a salvo) y franja (la
    /// embestida, desde el atacante en una dirección). Se desprende de su dueño al empezar para no seguirlo (el salto
    /// del Jaguar avisa dónde caerá); queda en la misma escena y se descarga con ella. Contains comprueba si algo está
    /// dentro.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TelegraphMarker : MonoBehaviour
    {
        public enum MarkerShape { Circle, Ring, Line }

        // El sprite del círculo y del anillo mide 2 m de diámetro a escala 1; el de la franja, 1 m de ancho y 2 de largo,
        // con el pivote en el centro de su borde corto.
        const float SpriteRadius = 1f;
        const float LineSpriteWidth = 1f;
        const float LineSpriteLength = 2f;
        /// <summary>Radio interior del anillo respecto al exterior (lo dibuja así EffectSpriteFactory).</summary>
        public const float RingInnerRatio = 0.5f;
        const float GroundOffset = 0.03f;

        [SerializeField] MarkerShape shape = MarkerShape.Circle;
        [SerializeField] Color color = new(1f, 0.45f, 0.25f, 1f);

        SpriteRenderer spriteRenderer;
        float duration;
        float elapsed;
        float radius;
        float length;
        Vector3 direction;

        public bool IsShowing => spriteRenderer != null && spriteRenderer.enabled;
        public MarkerShape Shape => shape;
        public float Radius => radius;
        public Vector3 Center { get; private set; }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
            transform.SetParent(null, true);
        }

        /// <summary>Muestra el círculo o el anillo centrado en <paramref name="center"/> durante <paramref name="seconds"/>.</summary>
        public void Show(Vector3 center, float outerRadius, float seconds)
        {
            Center = center;
            radius = outerRadius;
            Begin(Center, Quaternion.Euler(90f, 0f, 0f), Vector3.one * (outerRadius / SpriteRadius), seconds);
        }

        /// <summary>Muestra la franja desde <paramref name="origin"/> hacia <paramref name="towards"/>.</summary>
        public void ShowLine(Vector3 origin, Vector3 towards, float lineLength, float width, float seconds)
        {
            direction = Vector3.ProjectOnPlane(towards, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.5f)
                direction = Vector3.forward;
            Center = origin;
            length = lineLength;
            radius = width * 0.5f;
            var yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Begin(origin, Quaternion.Euler(90f, yaw, 0f), new Vector3(width / LineSpriteWidth, lineLength / LineSpriteLength, 1f), seconds);
        }

        public void Hide()
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
            enabled = false;
        }

        /// <summary>Si el punto está dentro de la forma, en el plano (la altura no cuenta).</summary>
        public bool Contains(Vector3 point)
        {
            var offset = point - Center;
            offset.y = 0f;
            switch (shape)
            {
                case MarkerShape.Ring:
                    var distance = offset.magnitude;
                    return distance <= radius && distance >= radius * RingInnerRatio;
                case MarkerShape.Line:
                    var along = Vector3.Dot(offset, direction);
                    var across = Vector3.Cross(direction, offset).y;
                    return along >= 0f && along <= length && Mathf.Abs(across) <= radius;
                default:
                    return offset.sqrMagnitude <= radius * radius;
            }
        }

        void Begin(Vector3 position, Quaternion rotation, Vector3 scale, float seconds)
        {
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            transform.SetPositionAndRotation(position + Vector3.up * GroundOffset, rotation);
            transform.localScale = scale;
            spriteRenderer.color = color;
            spriteRenderer.enabled = true;
            enabled = true;
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
