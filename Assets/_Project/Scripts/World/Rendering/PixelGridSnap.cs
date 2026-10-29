using UnityEngine;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Para el sprite hijo de un personaje que se mueve: cada frame lo coloca en la posición de su padre alineada a
    /// la rejilla de píxeles del arte. Así sus píxeles coinciden con los del escenario en vez de partirse.
    /// </summary>
    public sealed class PixelGridSnap : MonoBehaviour
    {
        Transform cachedTransform;
        Transform parent;

        void Awake()
        {
            cachedTransform = transform;
            parent = cachedTransform.parent;
        }

        void LateUpdate()
        {
            var origin = parent != null ? parent.position : cachedTransform.position;
            cachedTransform.position = IsometricProjection.SnapToPixelGrid(origin, 1f / IsometricProjection.PixelsPerUnit);
        }
    }
}
