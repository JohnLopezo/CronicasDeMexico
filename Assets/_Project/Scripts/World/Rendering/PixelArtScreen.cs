using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// La cámara del escenario dibuja en una RenderTexture a la resolución del arte (~640×360) y un panel de UI Toolkit,
    /// por debajo del HUD, la muestra ampliada por un entero con filtro Point. Así el pixel art sale nítido en todos los
    /// niveles de calidad (el Render Scale de URP, 0,75 en Bajo, no se aplica a cámaras que dibujan en una textura) y se
    /// dibujan nueve veces menos píxeles a 1080 p. La textura tiene un píxel de margen por lado: la cámara se mueve de
    /// píxel del arte en píxel del arte y el resto se compensa desplazando la imagen en píxeles de pantalla, para que el
    /// scroll sea suave.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PixelArtScreen : MonoBehaviour
    {
        /// <summary>
        /// Renderer sin efectos (Renderer_Rendimiento) en los tres URP Assets. Lo usa la cámara que limpia la pantalla
        /// bajo el panel, que no dibuja nada y no debe pagar el SSAO ni el HDR del nivel Alto a resolución completa.
        /// </summary>
        public const int ClearRendererIndex = 1;

        [SerializeField, Tooltip("Documento a pantalla completa, con orden menor que el HUD.")]
        UIDocument presentation;

        Camera cachedCamera;
        RenderTexture target;
        VisualElement image;
        int screenWidth, screenHeight, width, height, scale;
        Vector2 subpixelOffset;

        public RenderTexture Target => target;
        public int Scale => scale;

        public UIDocument Presentation
        {
            get => presentation;
            set => presentation = value;
        }

        void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            image = new VisualElement { name = "pixel-art-screen", pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute;
        }

        void OnEnable() => Resize();

        void OnDisable()
        {
            image.RemoveFromHierarchy();
            if (cachedCamera != null)
                cachedCamera.targetTexture = null;
            Release();
            screenWidth = screenHeight = 0;
        }

        void LateUpdate()
        {
            if (Screen.width != screenWidth || Screen.height != screenHeight)
                Resize();

            // El documento crea su árbol en su propio OnEnable, que puede ejecutarse después del nuestro.
            if (image.parent == null && presentation != null && presentation.rootVisualElement != null)
                presentation.rootVisualElement.Add(image);
        }

        /// <summary>Resto del alineado de la cámara, en píxeles del arte (x a la derecha, y hacia arriba).</summary>
        public void SetSubpixelOffset(Vector2 texels)
        {
            subpixelOffset = texels;
            ApplyPosition();
        }

        void Resize()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            scale = IsometricProjection.IntegerScale(screenHeight);
            width = Mathf.CeilToInt(Mathf.Max(1, screenWidth) / (float)scale) + 2;
            height = Mathf.CeilToInt(Mathf.Max(1, screenHeight) / (float)scale) + 2;

            Release();
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "PixelArtScreen",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            target.Create();
            cachedCamera.targetTexture = target;
            cachedCamera.orthographicSize = height / (2f * IsometricProjection.PixelsPerUnit);

            image.style.backgroundImage = Background.FromRenderTexture(target);
            image.style.width = width * scale;
            image.style.height = height * scale;
            ApplyPosition();
        }

        void ApplyPosition()
        {
            image.style.left = Mathf.Round((screenWidth - width * scale) * 0.5f - subpixelOffset.x * scale);
            image.style.top = Mathf.Round((screenHeight - height * scale) * 0.5f + subpixelOffset.y * scale);
        }

        void Release()
        {
            if (target == null)
                return;
            target.Release();
            Destroy(target);
            target = null;
        }
    }
}
