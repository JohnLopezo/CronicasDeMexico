using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>Todos los objetos, para buscarlos por id (inventario, guardado, interfaz). Vive en Resources.</summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Catálogo de objetos", fileName = "CatalogoObjetos")]
    public sealed class ItemCatalog : ScriptableObject, IItemCatalog
    {
        public const string ResourcePath = "Recompensas/CatalogoObjetos";

        [SerializeField] ItemData[] items = Array.Empty<ItemData>();

        Dictionary<string, ItemData> byId;

        public IReadOnlyList<IItemInfo> All => items;
        public IReadOnlyList<ItemData> Items => items;

        public IItemInfo Find(string itemId) => Get(itemId);

        public ItemData Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            if (byId == null || byId.Count != items.Length)
                byId = items.Where(item => item != null).GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.First());
            return byId.TryGetValue(itemId, out var item) ? item : null;
        }

        public void SetItems(IEnumerable<ItemData> values)
        {
            items = values.ToArray();
            byId = null;
        }

        void OnEnable() => byId = null;
    }
}
