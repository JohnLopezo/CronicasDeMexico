using UnityEngine;

namespace CronicasDeMexico.Data.Narrative
{
    /// <summary>
    /// Identidad de un personaje: id persistente (para misiones, guardado y avisos "hablar:id"), nombre que se
    /// muestra y papel en su comunidad (Docs/Personajes).
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Narrativa/Personaje", fileName = "NPC_")]
    public sealed class NpcData : ScriptableObject
    {
        [SerializeField, Tooltip("Identificador estable, en minúsculas y sin espacios (p. ej. \"artesano_jade\").")]
        string id;

        [SerializeField] string displayName;

        [SerializeField, Tooltip("Papel en la comunidad (artesano, anciana...). Solo informativo.")]
        string role;

        public string Id => id;
        public string DisplayName => displayName;
        public string Role => role;

        public static NpcData Create(string id, string displayName, string role = null)
        {
            var npc = CreateInstance<NpcData>();
            npc.name = $"NPC_{id}";
            npc.id = id;
            npc.displayName = displayName;
            npc.role = role;
            return npc;
        }
    }
}
