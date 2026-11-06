using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Rewards;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Rewards
{
    /// <summary>
    /// Recompensa por llegar a un sitio la primera vez (descubrir un piso, una zona oculta): explorar da más que repetir
    /// combates. Se entrega una sola vez en toda la partida.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RewardOnEnter : MonoBehaviour
    {
        [SerializeField, Tooltip("Clave única de la recompensa.")] string rewardKey;
        [SerializeField] RewardBundle bundle;

        public void Configure(string key, RewardBundle reward)
        {
            rewardKey = key;
            bundle = reward;
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (bundle == null || !other.CompareTag("Player") || !ServiceLocator.TryGet(out RewardService rewards))
                return;
            rewards.Grant(bundle.ToPackage(rewards.Random), "descubrir." + rewardKey);
        }
    }
}
