using CronicasDeMexico.Core.Feedback;
using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Números de daño del HUD (DamageNumbers): un grupo fijo de etiquetas que se reutilizan. Cada una sigue su punto
    /// del mundo en la pantalla, sube y se desvanece. Con más golpes a la vez que etiquetas, el más viejo se reutiliza.
    /// </summary>
    public sealed class DamageNumberLayer
    {
        const int PoolSize = 12;
        const float Lifetime = 0.7f;
        const float RiseMetres = 0.8f;
        const string BrokenClass = "hud-damage--broken";
        const string TakenClass = "hud-damage--taken";

        readonly VisualElement layer;
        readonly Label[] labels = new Label[PoolSize];
        readonly Vector3[] origins = new Vector3[PoolSize];
        readonly float[] ages = new float[PoolSize];
        int next;

        public DamageNumberLayer(VisualElement root)
        {
            layer = root.Q("damage-numbers");
            if (layer == null)
                return;
            for (var i = 0; i < PoolSize; i++)
            {
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("hud-damage");
                label.style.display = DisplayStyle.None;
                layer.Add(label);
                labels[i] = label;
                ages[i] = Lifetime;
            }
            DamageNumbers.Reported += OnReported;
        }

        /// <summary>Números a la vista ahora. Para las pruebas.</summary>
        public int ActiveCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < PoolSize; i++)
                    if (labels[i] != null && ages[i] < Lifetime)
                        count++;
                return count;
            }
        }

        public void Dispose()
        {
            DamageNumbers.Reported -= OnReported;
            for (var i = 0; i < PoolSize; i++)
                if (labels[i] != null)
                {
                    ages[i] = Lifetime;
                    labels[i].style.display = DisplayStyle.None;
                }
        }

        void OnReported(Vector3 worldPosition, int amount, DamageKind kind)
        {
            var label = labels[next];
            label.text = amount.ToString();
            label.EnableInClassList(BrokenClass, kind == DamageKind.Broken);
            label.EnableInClassList(TakenClass, kind == DamageKind.Taken);
            origins[next] = worldPosition;
            ages[next] = 0f;
            Place(next, Camera.main);
            next = (next + 1) % PoolSize;
        }

        public void Tick(float deltaTime)
        {
            if (layer == null)
                return;
            var camera = Camera.main;
            for (var i = 0; i < PoolSize; i++)
            {
                if (ages[i] >= Lifetime)
                    continue;
                ages[i] += deltaTime;
                if (ages[i] >= Lifetime)
                    labels[i].style.display = DisplayStyle.None;
                else
                    Place(i, camera);
            }
        }

        /// <summary>
        /// Punto del mundo en coordenadas del panel de <paramref name="element"/>. Pasa por la vista (0–1) y no por
        /// WorldToScreenPoint: con PixelArtScreen la cámara dibuja en una textura pequeña. False si queda detrás.
        /// shortcut: se ignora el píxel de margen de esa textura (error de ~1 píxel del arte); usar su tamaño si se nota.
        /// </summary>
        public static bool TryProject(VisualElement element, Camera camera, Vector3 world, out Vector2 position)
        {
            position = default;
            if (camera == null || element?.panel == null)
                return false;
            var viewport = camera.WorldToViewportPoint(world);
            if (viewport.z < 0f)
                return false;
            position = RuntimePanelUtils.ScreenToPanel(element.panel, new Vector2(viewport.x * Screen.width, (1f - viewport.y) * Screen.height));
            return true;
        }

        void Place(int index, Camera camera)
        {
            var label = labels[index];
            if (camera == null || layer.panel == null)
            {
                label.style.display = DisplayStyle.None;
                return;
            }
            var t = ages[index] / Lifetime;
            if (!TryProject(layer, camera, origins[index] + Vector3.up * (RiseMetres * t), out var position))
            {
                label.style.display = DisplayStyle.None;
                return;
            }
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.opacity = 1f - t * t;
            label.style.display = DisplayStyle.Flex;
        }
    }
}
