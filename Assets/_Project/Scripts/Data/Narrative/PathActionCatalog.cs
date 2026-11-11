using System;
using CronicasDeMexico.Data.Characters;
using UnityEngine;

namespace CronicasDeMexico.Data.Narrative
{
    /// <summary>
    /// Lo que responde cada habitante a cada acción de senda (Docs/Sistemas/Acciones_Senda.md): unas líneas y, a
    /// veces, objetos, experiencia o banderas (recetas). Se da una sola vez por partida. Vive en Resources
    /// (<see cref="ResourcePath"/>) y lo crea PathActionAssets solo si falta; se edita en el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Narrativa/Acciones de senda", fileName = "AccionesSenda")]
    public sealed class PathActionCatalog : ScriptableObject
    {
        public const string ResourcePath = "Narrativa/AccionesSenda";

        [Serializable]
        public sealed class Item
        {
            public string id;
            [Min(1)] public int count = 1;
        }

        [Serializable]
        public sealed class Response
        {
            [Tooltip("Id del habitante (NpcData.Id).")] public string npcId;
            public PathAction action;
            [TextArea(2, 5), Tooltip("Lo que dice. Borrador de Claude hasta que lo revise el autor.")] public string[] lines = Array.Empty<string>();
            public Item[] items = Array.Empty<Item>();
            [Min(0)] public int experience;
            [Tooltip("Banderas que pone, p. ej. una receta (receta.<id>).")] public string[] flags = Array.Empty<string>();

            /// <summary>Clave de "una sola vez" en RewardService y bandera de hecho en StoryState.</summary>
            public string DoneKey => $"senda.{npcId}.{action.ToString().ToLowerInvariant()}";
        }

        [SerializeField] Response[] responses = Array.Empty<Response>();

        public Response[] Responses => responses;

        public void SetResponses(Response[] values) => responses = values ?? Array.Empty<Response>();

        public Response Find(string npcId, PathAction action)
        {
            if (string.IsNullOrEmpty(npcId))
                return null;
            foreach (var response in responses)
                if (response != null && response.npcId == npcId && response.action == action)
                    return response;
            return null;
        }

        static PathActionCatalog loaded;
        static bool searched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = null;
            searched = false;
        }

        /// <summary>El catálogo de Resources, cargado una vez; null si no existe.</summary>
        public static PathActionCatalog Load()
        {
            if (!searched)
            {
                loaded = Resources.Load<PathActionCatalog>(ResourcePath);
                searched = true;
            }
            return loaded;
        }
    }
}
