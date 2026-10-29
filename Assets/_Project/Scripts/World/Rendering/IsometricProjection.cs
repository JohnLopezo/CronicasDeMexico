using UnityEngine;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Proyección de la cámara isométrica 2:1 de los sprites prerenderizados (Docs/Arte_Olmeca_Demo.md §3–§5):
    /// rotación fija (30°, 45°, 0°), 45,2548 px por metro y 360 px de alto de referencia escalados por un entero.
    /// </summary>
    public static class IsometricProjection
    {
        public const float PixelsPerUnit = 45.254834f; // 32·√2: rombo de 64×32 px por metro
        public const int ReferenceHeight = 360;

        public static readonly Quaternion CameraRotation = Quaternion.Euler(30f, 45f, 0f);

        /// <summary>
        /// Convierte coordenadas del flujo Blender (x, y en el suelo, z arriba; Tools/ArtPipeline) a Unity.
        /// +X del flujo va a la derecha-abajo en pantalla y +Y a la derecha-arriba.
        /// </summary>
        public static Vector3 FromPipeline(float x, float y, float z) => new(y, z, -x);

        /// <summary>
        /// Mueve la posición sobre el plano de la cámara hasta la rejilla de píxeles de tamaño pixelSize (en metros),
        /// sin cambiar su profundidad. Con todo alineado, cada píxel del arte ocupa siempre los mismos píxeles de
        /// pantalla y no tiembla.
        /// </summary>
        public static Vector3 SnapToPixelGrid(Vector3 position, float pixelSize)
        {
            var right = CameraRotation * Vector3.right;
            var up = CameraRotation * Vector3.up;
            var r = Vector3.Dot(position, right) / pixelSize;
            var u = Vector3.Dot(position, up) / pixelSize;
            return position + right * ((Mathf.Round(r) - r) * pixelSize) + up * ((Mathf.Round(u) - u) * pixelSize);
        }

        /// <summary>Factor entero de escalado del arte (×2 a 720 p, ×3 a 1080 p…), como mínimo 1.</summary>
        public static int IntegerScale(int screenHeight) => Mathf.Max(1, screenHeight / ReferenceHeight);

        /// <summary>Tamaño ortográfico que muestra el arte a escala entera: 3,977 a 1080 p.</summary>
        public static float OrthographicSize(int screenHeight) =>
            Mathf.Max(1, screenHeight) / (2f * PixelsPerUnit * IntegerScale(screenHeight));
    }
}
