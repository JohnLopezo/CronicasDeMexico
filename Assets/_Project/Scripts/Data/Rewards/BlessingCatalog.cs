using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>Todas las Bendiciones, y cuáles se ofrecen a elegir al completar cada mazmorra. Vive en Resources.</summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Catálogo de Bendiciones", fileName = "CatalogoBendiciones")]
    public sealed class BlessingCatalog : ScriptableObject, IBlessingCatalog
    {
        public const string ResourcePath = "Recompensas/CatalogoBendiciones";

        [SerializeField] BlessingData[] blessings = Array.Empty<BlessingData>();

        public IReadOnlyList<IBlessingInfo> All => blessings;
        public IReadOnlyList<BlessingData> Blessings => blessings;

        public IBlessingInfo Find(string blessingId) => Get(blessingId);

        public BlessingData Get(string blessingId) =>
            string.IsNullOrEmpty(blessingId) ? null : blessings.FirstOrDefault(b => b != null && b.Id == blessingId);

        /// <summary>Las secundarias de una cultura: las tres que se ofrecen al completar su mazmorra.</summary>
        public IEnumerable<BlessingData> SecondaryChoices(string culture) =>
            blessings.Where(b => b != null && b.Slot == BlessingSlot.Secundaria && b.Culture == culture);

        public void SetBlessings(IEnumerable<BlessingData> values) => blessings = values.ToArray();
    }
}
