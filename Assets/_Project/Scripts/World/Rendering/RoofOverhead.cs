using UnityEngine;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Techo bajo el que se puede pasar (la enramada). Como los demás sprites, se ordena por la profundidad de su
    /// pivote, en el centro de la huella: con el objetivo debajo y por delante del centro, el objetivo se dibujaría
    /// encima del techo. Mientras está bajo la huella, el sprite se adelanta hasta quedar justo delante de él. Se mueve a
    /// lo largo de la mirada de la cámara, así que en pantalla no cambia de sitio ni sale de la rejilla de píxeles.
    /// </summary>
    public sealed class RoofOverhead : MonoBehaviour
    {
        const float Margin = 0.05f;

        [SerializeField] Transform target;
        [Tooltip("Media huella del techo en x y z del mundo, desde la posición del sprite.")]
        [SerializeField] Vector2 halfExtents = Vector2.one;

        Transform cachedTransform;
        Vector3 restPosition;

        public Transform Target => target;
        public Vector3 RestPosition => restPosition;

        void Awake()
        {
            cachedTransform = transform;
            restPosition = cachedTransform.position;
        }

        void LateUpdate()
        {
            if (target == null)
                return;
            var forward = IsometricProjection.CameraRotation * Vector3.forward;
            var offset = target.position - restPosition;
            var underneath = Mathf.Abs(offset.x) < halfExtents.x && Mathf.Abs(offset.z) < halfExtents.y;
            var depth = underneath ? Mathf.Min(0f, Vector3.Dot(offset, forward) - Margin) : 0f;
            cachedTransform.position = restPosition + forward * depth;
        }
    }
}
