namespace CronicasDeMexico.Core.Narrative
{
    /// <summary>
    /// Forma de los avisos y banderas que comparten diálogos, misiones e interacciones, para no escribirlos a mano.
    /// </summary>
    public static class StoryEvents
    {
        public static string Talk(string npcId) => $"hablar:{npcId}";
        public static string Examine(string id) => $"examinar:{id}";
        public static string Defeat(string enemyId) => $"vencer:{enemyId}";
        public static string Reach(string zoneId) => $"llegar:{zoneId}";

        public static string QuestActiveFlag(string questId) => $"mision.{questId}.activa";
        public static string QuestCompletedFlag(string questId) => $"mision.{questId}.completada";
    }
}
