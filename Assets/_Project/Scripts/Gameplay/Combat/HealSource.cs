namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Origen de una curación. Regla del diseño (Docs/Diseno.md §9): la vida nunca se recupera sola, ni al terminar
    /// un combate ni al cambiar de piso; solo por estas vías.
    /// </summary>
    public enum HealSource
    {
        /// <summary>Santuario de Vida u otra zona de descanso.</summary>
        RestShrine,
        Consumable,
        Ability,
        Blessing,
        /// <summary>NPC o evento narrativo específico.</summary>
        NarrativeEvent
    }
}
