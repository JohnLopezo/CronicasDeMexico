namespace CronicasDeMexico.Core.Progress
{
    /// <summary>
    /// Pantallas del menú del juego (UI, GameMenuController). El mundo pide abrir una por el canal EVT_Menu_Abrir:
    /// el santuario de Memoria abre el Museo, el artesano la fabricación, el comerciante el comercio y la ofrenda final
    /// la elección de Bendición y los resultados.
    /// </summary>
    public static class MenuTabs
    {
        public const string Inventory = "bolsa";
        public const string Blessings = "bendiciones";
        public const string Crafting = "fabricar";
        public const string Trading = "comerciar";
        public const string Museum = "museo";
        public const string BlessingChoice = "eleccion";
        public const string Results = "resultados";
    }
}
