using System.Collections.Generic;
using CronicasDeMexico.Core.Narrative;

namespace CronicasDeMexico.Core.Progress
{
    /// <summary>Receta de fabricación (Docs/Sistemas/Sistema_Fabricacion.md). La implementa RecipeData, en Data.</summary>
    public interface ICraftRecipe
    {
        string Id { get; }
        string ResultId { get; }
        int ResultCount { get; }
        IReadOnlyList<ItemStack> Ingredients { get; }
        /// <summary>Bandera que la desbloquea (p. ej. "receta.amuleto_jade"); vacía = disponible desde el principio.</summary>
        string UnlockFlag { get; }
    }

    /// <summary>
    /// Oferta de un comerciante: se da <see cref="PriceCount"/> de <see cref="PriceId"/> (ecos o un material, en
    /// trueque) a cambio de <see cref="ItemCount"/> de <see cref="ItemId"/>.
    /// </summary>
    public interface ITradeOffer
    {
        string Id { get; }
        string ItemId { get; }
        int ItemCount { get; }
        string PriceId { get; }
        int PriceCount { get; }
        /// <summary>Bandera que la desbloquea; vacía = disponible desde el principio.</summary>
        string UnlockFlag { get; }
    }

    /// <summary>
    /// Reglas de fabricación y comercio sobre el inventario, sin estado propio: lo desbloqueado son banderas de la
    /// historia y lo que se tiene, el inventario. No se fabrica sin todos los materiales, y los objetos únicos o
    /// narrativos no se venden ni se entregan en trueque.
    /// </summary>
    public static class Economy
    {
        public static bool IsUnlocked(string unlockFlag, StoryState story) =>
            string.IsNullOrEmpty(unlockFlag) || (story != null && story.HasFlag(unlockFlag));

        /// <summary>Lo que falta de cada material (vacío si se puede fabricar).</summary>
        public static List<ItemStack> Missing(ICraftRecipe recipe, Inventory inventory)
        {
            var missing = new List<ItemStack>();
            foreach (var ingredient in recipe.Ingredients)
            {
                var lacking = ingredient.count - inventory.Count(ingredient.id);
                if (lacking > 0)
                    missing.Add(new ItemStack(ingredient.id, lacking));
            }
            return missing;
        }

        public static bool CanCraft(ICraftRecipe recipe, Inventory inventory, StoryState story)
        {
            if (recipe == null || !IsUnlocked(recipe.UnlockFlag, story))
                return false;
            foreach (var ingredient in recipe.Ingredients)
                if (!inventory.Has(ingredient.id, ingredient.count))
                    return false;
            return inventory.Count(recipe.ResultId) + recipe.ResultCount <= inventory.MaxStack(recipe.ResultId);
        }

        /// <summary>Gasta los materiales y añade el resultado. Sin materiales (o sin sitio) no hace nada.</summary>
        public static bool Craft(ICraftRecipe recipe, Inventory inventory, StoryState story)
        {
            if (!CanCraft(recipe, inventory, story))
                return false;
            foreach (var ingredient in recipe.Ingredients)
                inventory.Remove(ingredient.id, ingredient.count);
            inventory.Add(recipe.ResultId, recipe.ResultCount);
            return true;
        }

        public static bool CanBuy(ITradeOffer offer, Inventory inventory, StoryState story) =>
            offer != null && IsUnlocked(offer.UnlockFlag, story) && inventory.Has(offer.PriceId, offer.PriceCount) &&
            CanGive(offer.PriceId, inventory) &&
            inventory.Count(offer.ItemId) + offer.ItemCount <= inventory.MaxStack(offer.ItemId);

        public static bool Buy(ITradeOffer offer, Inventory inventory, StoryState story)
        {
            if (!CanBuy(offer, inventory, story))
                return false;
            inventory.Remove(offer.PriceId, offer.PriceCount);
            inventory.Add(offer.ItemId, offer.ItemCount);
            return true;
        }

        /// <summary>Si se puede vender o entregar en trueque (la moneda siempre; lo único o narrativo, nunca).</summary>
        public static bool CanGive(string itemId, Inventory inventory)
        {
            if (itemId == Inventory.Currency)
                return true;
            var info = inventory.Catalog?.Find(itemId);
            return info != null && info.Sellable && info.Category != ItemCategory.Narrativo && !inventory.IsEquipped(itemId);
        }

        public static bool CanSell(string itemId, Inventory inventory) =>
            itemId != Inventory.Currency && CanGive(itemId, inventory) && inventory.Has(itemId) &&
            (inventory.Catalog?.Find(itemId)?.Value ?? 0) > 0;

        /// <summary>Vende una unidad a cambio de su valor en ecos.</summary>
        public static bool Sell(string itemId, Inventory inventory)
        {
            if (!CanSell(itemId, inventory))
                return false;
            var value = inventory.Catalog.Find(itemId).Value;
            inventory.Remove(itemId, 1);
            inventory.Add(Inventory.Currency, value);
            return true;
        }
    }
}
