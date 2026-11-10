using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Oferta del comerciante: se paga con ecos (la unidad ficticia del juego) o con un material, en trueque.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Oferta", fileName = "OFE_")]
    public sealed class TradeOfferData : ScriptableObject, ITradeOffer
    {
        [SerializeField] string id;
        [SerializeField] ItemData item;
        [SerializeField, Min(1)] int itemCount = 1;
        [SerializeField, Tooltip("Ecos o un material.")] ItemData price;
        [SerializeField, Min(1)] int priceCount = 1;
        [SerializeField, Tooltip("Bandera que la desbloquea; vacía = disponible desde el principio.")] string unlockFlag;

        public string Id => id;
        public ItemData Item => item;
        public ItemData Price => price;
        public string ItemId => item != null ? item.Id : null;
        public int ItemCount => itemCount;
        public string PriceId => price != null ? price.Id : null;
        public int PriceCount => priceCount;
        public string UnlockFlag => unlockFlag;

        public void Configure(string id, ItemData item, int itemCount, ItemData price, int priceCount, string unlockFlag = null)
        {
            this.id = id;
            this.item = item;
            this.itemCount = itemCount;
            this.price = price;
            this.priceCount = priceCount;
            this.unlockFlag = unlockFlag;
        }
    }
}
