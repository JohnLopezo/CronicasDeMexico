namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Banderas de la mazmorra. Las de la incursión (prefijo <see cref="IncursionPrefix"/>) se borran al salir de la
    /// mazmorra (IncursionReset); las demás se conservan.
    /// </summary>
    public static class DungeonFlags
    {
        public const string IncursionPrefix = "incursion.";

        /// <summary>El santuario de Vida ya curó en esta incursión.</summary>
        public static string LifeShrineUsed(string shrineId) => $"{IncursionPrefix}santuario_vida.{shrineId}";

        /// <summary>Se descansó alguna vez en este santuario de Memoria (su jade brilla).</summary>
        public static string MemoryShrineAwake(string shrineId) => $"santuario_memoria.{shrineId}";
    }
}
