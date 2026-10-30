using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Bendición (Docs/Contenido/Bendiciones.md). La principal la da la historia (no se sustituye); la secundaria se
    /// elige entre tres. Una Bendición con coste de energía es una habilidad activa.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Bendición", fileName = "BEN_")]
    public sealed class BlessingData : ScriptableObject, IBlessingInfo
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField, TextArea(2, 5)] string description;
        [SerializeField] BlessingSlot slot;
        [SerializeField] CharacterStats bonus;
        [SerializeField, Min(0)] int energyCost;
        [SerializeField, Min(0f)] float cooldown;
        [SerializeField, TextArea(2, 3)] string combatEffect;
        [SerializeField, TextArea(2, 3)] string explorationEffect;
        [SerializeField, TextArea(2, 3)] string unlockCondition;
        [SerializeField] string culture;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public BlessingSlot Slot => slot;
        public CharacterStats Bonus => bonus;
        public int EnergyCost => energyCost;
        public float Cooldown => cooldown;
        public string CombatEffect => combatEffect;
        public string ExplorationEffect => explorationEffect;
        public string UnlockCondition => unlockCondition;
        public string Culture => culture;

        public void Configure(string id, string displayName, string description, BlessingSlot slot, CharacterStats bonus,
            int energyCost, float cooldown, string combatEffect, string explorationEffect, string unlockCondition, string culture)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.slot = slot;
            this.bonus = bonus;
            this.energyCost = energyCost;
            this.cooldown = cooldown;
            this.combatEffect = combatEffect;
            this.explorationEffect = explorationEffect;
            this.unlockCondition = unlockCondition;
            this.culture = culture;
        }
    }
}
