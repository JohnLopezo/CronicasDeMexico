using System;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Entrada de una cultura en el Museo de la Memoria (Docs/Sistemas/Sistema_Recompensas.md §13): resumen, la bandera
    /// que la descubre y sus Guardianes (con la bandera que marca cada uno como vencido o liberado). La sala física
    /// que evolucione con cada cultura podrá leer estos mismos datos.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Cultura del Museo", fileName = "CUL_")]
    public sealed class CultureRecord : ScriptableObject
    {
        [Serializable]
        public struct Guardian
        {
            public string name;
            public string defeatedFlag;
            [TextArea(2, 4)] public string memory;

            public Guardian(string name, string defeatedFlag, string memory)
            {
                this.name = name;
                this.defeatedFlag = defeatedFlag;
                this.memory = memory;
            }
        }

        [SerializeField] string id;
        [SerializeField] string title;
        [SerializeField, Tooltip("Bandera que la descubre (registro.cultura.<id>).")] string discoveredFlag;
        [SerializeField, TextArea(4, 10), Tooltip("Separar lo documentado (D), interpretado (I) y ficticio (F).")] string summary;
        [SerializeField] Guardian[] guardians = Array.Empty<Guardian>();

        public string Id => id;
        public string Title => title;
        public string DiscoveredFlag => discoveredFlag;
        public string Summary => summary;
        public Guardian[] Guardians => guardians;

        public void Configure(string cultureId, string cultureTitle, string flag, string text, Guardian[] cultureGuardians)
        {
            id = cultureId;
            title = cultureTitle;
            discoveredFlag = flag;
            summary = text;
            guardians = cultureGuardians ?? Array.Empty<Guardian>();
        }
    }
}
