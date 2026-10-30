using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>Receta de fabricación con el artesano (Docs/Sistemas/Sistema_Fabricacion.md).</summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Receta", fileName = "RCT_")]
    public sealed class RecipeData : ScriptableObject, ICraftRecipe
    {
        [SerializeField] string id;
        [SerializeField] ItemData result;
        [SerializeField, Min(1)] int resultCount = 1;
        [SerializeField] RewardBundle.Stack[] ingredients = Array.Empty<RewardBundle.Stack>();
        [SerializeField, Tooltip("Bandera que la desbloquea; vacía = disponible desde el principio.")] string unlockFlag;

        ItemStack[] stacks;

        public string Id => id;
        public ItemData Result => result;
        public string ResultId => result != null ? result.Id : null;
        public int ResultCount => resultCount;
        public string UnlockFlag => unlockFlag;

        public IReadOnlyList<ItemStack> Ingredients =>
            stacks ??= ingredients.Where(i => i.item != null).Select(i => new ItemStack(i.item.Id, i.count)).ToArray();

        public void Configure(string id, ItemData result, int resultCount, RewardBundle.Stack[] ingredients, string unlockFlag)
        {
            this.id = id;
            this.result = result;
            this.resultCount = resultCount;
            this.ingredients = ingredients ?? Array.Empty<RewardBundle.Stack>();
            this.unlockFlag = unlockFlag;
            stacks = null;
        }

        void OnValidate() => stacks = null;
    }
}
