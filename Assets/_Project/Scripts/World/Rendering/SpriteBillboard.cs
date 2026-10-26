using UnityEngine;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Orienta un sprite hacia la cámara (personajes, NPC, vegetación en estilo HD-2D).
    /// Por defecto gira solo sobre el eje vertical: el sprite queda de pie, no se mete en los muros que tiene
    /// detrás y su sombra conserva la silueta.
    /// </summary>
    public sealed class SpriteBillboard : MonoBehaviour
    {
        public enum BillboardMode
        {
            [Tooltip("Gira solo en Y; el sprite queda vertical.")]
            VerticalAxis,
            [Tooltip("Copia la orientación de la cámara; el sprite se inclina con ella.")]
            CameraPlane
        }

        [SerializeField] BillboardMode mode = BillboardMode.VerticalAxis;

        [SerializeField, Tooltip("Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        Transform cachedTransform;

        public BillboardMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        void Awake() => cachedTransform = transform;

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        // LateUpdate: la cámara de Cinemachine ya se movió en este frame.
        void LateUpdate()
        {
            if (cameraTransform == null)
                return;

            var forward = cameraTransform.forward;

            if (mode == BillboardMode.CameraPlane)
            {
                cachedTransform.rotation = Quaternion.LookRotation(forward, cameraTransform.up);
                return;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                cachedTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
