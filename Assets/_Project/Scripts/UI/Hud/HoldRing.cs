using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Anillo que se llena mientras se mantiene pulsado Interactuar (acción de senda, HoldProgress). Se dibuja con
    /// Painter2D: un aro tenue de fondo y un arco claro desde arriba, en el sentido de las agujas del reloj.
    /// </summary>
    public sealed class HoldRing : VisualElement
    {
        const float LineWidth = 6f;
        static readonly Color Track = new(0.94f, 0.91f, 0.84f, 0.25f);
        static readonly Color Fill = new(0.94f, 0.82f, 0.38f, 1f);

        float progress;

        public HoldRing()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("hud-hold__ring");
            generateVisualContent += Draw;
        }

        /// <summary>De 0 a 1.</summary>
        public float Progress
        {
            get => progress;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, progress))
                    return;
                progress = value;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f - LineWidth;
            if (radius <= 0f)
                return;
            var center = rect.center;
            var painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;

            painter.strokeColor = Track;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            if (progress <= 0f)
                return;
            painter.strokeColor = Fill;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * progress));
            painter.Stroke();
        }
    }
}
