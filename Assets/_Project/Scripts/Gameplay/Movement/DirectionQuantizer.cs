using UnityEngine;

namespace CronicasDeMexico.Gameplay.Movement
{
    /// <summary>Ocho direcciones vistas desde la cámara. Norte = alejándose de la cámara.</summary>
    public enum ScreenDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    /// <summary>
    /// Reduce una dirección del mundo a una de las ocho direcciones de pantalla, sin estado para poder probarlo.
    /// </summary>
    public static class DirectionQuantizer
    {
        const float SectorDegrees = 360f / 8f;

        /// <summary>
        /// Dirección de pantalla de <paramref name="worldDirection"/> (plano XZ) vista por una cámara con ese giro
        /// horizontal. Un vector nulo devuelve North.
        /// </summary>
        public static ScreenDirection FromWorld(Vector3 worldDirection, float cameraYawDegrees)
        {
            var local = Quaternion.Euler(0f, -cameraYawDegrees, 0f) * worldDirection;
            var angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg; // 0 = norte, 90 = este
            var sector = Mathf.RoundToInt(angle / SectorDegrees);
            return (ScreenDirection)(((sector % 8) + 8) % 8);
        }

        /// <summary>True para las tres direcciones que miran al oeste.</summary>
        public static bool IsWestward(ScreenDirection direction) =>
            direction is ScreenDirection.SouthWest or ScreenDirection.West or ScreenDirection.NorthWest;

        /// <summary>Refleja la dirección de izquierda a derecha (West ↔ East); North y South no cambian.</summary>
        public static ScreenDirection Mirror(ScreenDirection direction) => (ScreenDirection)((8 - (int)direction) % 8);
    }
}
