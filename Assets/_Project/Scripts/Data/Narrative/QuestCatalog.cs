using System;
using CronicasDeMexico.Core.Narrative;
using UnityEngine;

namespace CronicasDeMexico.Data.Narrative
{
    /// <summary>
    /// Todas las misiones del juego, para recuperarlas por id al cargar una partida. GameBootstrapper lo carga de
    /// Resources (<see cref="ResourcePath"/>): Core no puede referenciar a Data, así que lo trata como IQuestCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Narrativa/Catálogo de misiones", fileName = "CatalogoMisiones")]
    public sealed class QuestCatalog : ScriptableObject, IQuestCatalog
    {
        /// <summary>Ruta dentro de una carpeta Resources (Assets/_Project/Resources/Narrativa/CatalogoMisiones.asset).</summary>
        public const string ResourcePath = "Narrativa/CatalogoMisiones";

        [SerializeField] QuestData[] quests = Array.Empty<QuestData>();

        public QuestData[] Quests => quests;

        public IQuestDefinition Find(string questId)
        {
            foreach (var quest in quests)
                if (quest != null && quest.Id == questId)
                    return quest;
            return null;
        }

        public static QuestCatalog Create(params QuestData[] quests)
        {
            var catalog = CreateInstance<QuestCatalog>();
            catalog.name = "CatalogoMisiones";
            catalog.quests = quests ?? Array.Empty<QuestData>();
            return catalog;
        }
    }
}
