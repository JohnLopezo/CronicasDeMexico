using UnityEngine;

namespace CronicasDeMexico.UI.Touch
{
    /// <summary>
    /// Ajusta su RectTransform a Screen.safeArea: los controles táctiles no quedan bajo la muesca ni las esquinas
    /// redondeadas del móvil (GDD §14, zonas seguras). Solo recalcula cuando cambia la zona o la resolución.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void Update()
        {
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (area == applied && size == screen || size.x <= 0 || size.y <= 0)
                return;
            applied = area;
            screen = size;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            rect.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
