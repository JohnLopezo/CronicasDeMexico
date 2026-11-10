using UnityEngine;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Cámara ortográfica fija de las escenas de pixel art prerenderizado: sigue al objetivo con suavizado, no sale
    /// de los límites del mapa, ajusta el tamaño ortográfico para que el arte se vea a escala entera y alinea su
    /// posición a la rejilla de píxeles de pantalla.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;

        [SerializeField, Min(0f), Tooltip("Tiempo de suavizado del seguimiento en segundos.")]
        float smoothTime = 0.12f;

        [SerializeField, Min(1f), Tooltip("Distancia de la cámara al punto que mira, en metros.")]
        float distance = 60f;

        [SerializeField, Tooltip("Límites del punto que mira en el plano XZ del mundo (mínimo).")]
        Vector2 boundsMin = new(-1000f, -1000f);

        [SerializeField, Tooltip("Límites del punto que mira en el plano XZ del mundo (máximo).")]
        Vector2 boundsMax = new(1000f, 1000f);

        Camera cachedCamera;
        PixelArtScreen pixelScreen;
        Vector3 focus;
        Vector3 velocity;
        int screenHeight;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            pixelScreen = GetComponent<PixelArtScreen>();
            cachedCamera.orthographic = true;
            transform.rotation = IsometricProjection.CameraRotation;
        }

        void Start()
        {
            if (target != null)
                focus = Clamp(target.position);
            Apply();
        }

        void LateUpdate()
        {
            if (target != null)
                focus = Vector3.SmoothDamp(focus, Clamp(target.position), ref velocity, smoothTime);
            Apply();
        }

        Vector3 Clamp(Vector3 position) => new(
            Mathf.Clamp(position.x, boundsMin.x, boundsMax.x),
            position.y,
            Mathf.Clamp(position.z, boundsMin.y, boundsMax.y));

        void Apply()
        {
            var desired = focus - transform.forward * distance;

            // Con PixelArtScreen la cámara dibuja a la resolución del arte: se alinea a sus píxeles y el resto lo
            // compensa la presentación en píxeles de pantalla. PixelArtScreen fija el tamaño ortográfico.
            if (pixelScreen != null && pixelScreen.isActiveAndEnabled)
            {
                const float texel = 1f / IsometricProjection.PixelsPerUnit;
                var snapped = IsometricProjection.SnapToPixelGrid(desired, texel);
                transform.position = snapped;
                var rest = desired - snapped;
                pixelScreen.SetSubpixelOffset(new Vector2(
                    Vector3.Dot(rest, transform.right) / texel, Vector3.Dot(rest, transform.up) / texel));
                return;
            }

            if (Screen.height != screenHeight)
            {
                screenHeight = Screen.height;
                cachedCamera.orthographicSize = IsometricProjection.OrthographicSize(screenHeight);
            }

            var pixelSize = 1f / (IsometricProjection.PixelsPerUnit * IsometricProjection.IntegerScale(screenHeight));
            transform.position = IsometricProjection.SnapToPixelGrid(desired, pixelSize);
        }
    }
}
