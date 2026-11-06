using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>Recetas del artesano y ofertas del comerciante de una cultura. Vive en Resources.</summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Catálogo de economía", fileName = "CatalogoEconomia")]
    public sealed class EconomyCatalog : ScriptableObject
    {
        public const string ResourcePath = "Recompensas/CatalogoEconomia";

        [SerializeField] RecipeData[] recipes = Array.Empty<RecipeData>();
        [SerializeField] TradeOfferData[] offers = Array.Empty<TradeOfferData>();

        public IReadOnlyList<RecipeData> Recipes => recipes;
        public IReadOnlyList<TradeOfferData> Offers => offers;

        public void Configure(IEnumerable<RecipeData> recipeValues, IEnumerable<TradeOfferData> offerValues)
        {
            recipes = recipeValues.ToArray();
            offers = offerValues.ToArray();
        }
    }
}
