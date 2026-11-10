using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>Culturas que muestra el Museo de la Memoria, en orden. Vive en Resources.</summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Catálogo del Museo", fileName = "CatalogoMuseo")]
    public sealed class MuseumCatalog : ScriptableObject
    {
        public const string ResourcePath = "Recompensas/CatalogoMuseo";

        [SerializeField] CultureRecord[] cultures = Array.Empty<CultureRecord>();

        public IReadOnlyList<CultureRecord> Cultures => cultures;

        public void SetCultures(IEnumerable<CultureRecord> values) => cultures = values.ToArray();
    }
}
