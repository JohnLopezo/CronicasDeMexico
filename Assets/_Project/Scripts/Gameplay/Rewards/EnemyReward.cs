using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Rewards
{
    /// <summary>
    /// Botín de un enemigo (Docs/Contenido/Tabla_Recompensas_Olmeca.md): al caer, tira su tabla (experiencia y
    /// materiales) y la entrega con RewardService. La experiencia se reduce si el jugador está por encima del nivel de
    /// la zona y, como cada enemigo concreto lleva su clave, vencerlo otra vez al reaparecer da cada vez menos hasta no
    /// dar nada (protección contra el farmeo). La fauna neutral no lleva este componente.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class EnemyReward : MonoBehaviour
    {
        [SerializeField] LootTable table;

        [SerializeField, Min(0), Tooltip("Nivel recomendado de la zona (0 = sin reducción por nivel).")]
        int zoneLevel;

        Health health;

        public LootTable Table
        {
            get => table;
            set => table = value;
        }

        public int ZoneLevel
        {
            get => zoneLevel;
            set => zoneLevel = value;
        }

        /// <summary>Identifica a este enemigo concreto (escena y nombre), estable entre recargas.</summary>
        public string RepeatKey => $"{gameObject.scene.name}/{name}";

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            if (table == null || !ServiceLocator.TryGet(out RewardService rewards))
                return;
            rewards.Grant(table.Roll(rewards.Random), null, zoneLevel, RepeatKey);
        }
    }
}
