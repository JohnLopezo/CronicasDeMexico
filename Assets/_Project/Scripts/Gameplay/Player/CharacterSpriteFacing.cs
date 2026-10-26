using UnityEngine;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Voltea el sprite según hacia qué lado de la pantalla se mueve el personaje.
    /// Al moverse solo hacia arriba o abajo conserva el último lado.
    /// Voltea con la escala X del objeto y no con SpriteRenderer.flipX: URP no entrega flipX a las pasadas de
    /// sombra y profundidad, así que la sombra no se voltearía.
    /// </summary>
    public sealed class CharacterSpriteFacing : MonoBehaviour
    {
        // Evita parpadeos cuando la dirección es casi vertical en pantalla.
        const float SideDeadZone = 0.1f;

        [SerializeField] PlayerMovement movement;
        [SerializeField] SpriteRenderer spriteRenderer;

        [SerializeField, Tooltip("Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        [SerializeField, Tooltip("Activar si el dibujo original del sprite mira a la izquierda.")]
        bool artFacesLeft;

        Transform spriteTransform;

        void Awake()
        {
            if (spriteRenderer != null)
                spriteTransform = spriteRenderer.transform;
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (movement == null || spriteTransform == null)
                return;

            var screenRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            var side = Vector3.Dot(movement.FacingDirection, screenRight);
            if (Mathf.Abs(side) < SideDeadZone)
                return;

            var scaleX = (side < 0f) != artFacesLeft ? -1f : 1f;
            var scale = spriteTransform.localScale;
            if (!Mathf.Approximately(scale.x, scaleX))
                spriteTransform.localScale = new Vector3(scaleX, scale.y, scale.z);
        }
    }
}
