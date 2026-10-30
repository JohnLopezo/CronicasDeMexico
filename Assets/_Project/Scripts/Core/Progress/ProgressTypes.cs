using System;
using System.Collections.Generic;

namespace CronicasDeMexico.Core.Progress
{
    // Tipos comunes del sistema de recompensas (Docs/Sistemas/Sistema_Recompensas.md). Las definiciones viven en Data
    // como ScriptableObjects; Core solo conoce estas interfaces, como con IQuestDefinition.

    public enum ItemCategory { Material, Consumible, Reliquia, Coleccionable, Narrativo, Moneda }

    public enum Rarity { Comun, PocoComun, Raro, Epico, Legendario }

    /// <summary>Si un objeto o dato cultural está documentado, es interpretación o es ficción del juego (Diseno.md §3).</summary>
    public enum Origin { Documentado, Interpretado, Ficticio }

    /// <summary>Atributos del Guardián (vida y energía máximas, ataque, defensa). Se suman: base + reliquias + Bendiciones.</summary>
    [Serializable]
    public struct CharacterStats
    {
        public int maxHealth;
        public int attack;
        public int defense;
        public int maxEnergy;

        public CharacterStats(int maxHealth, int attack, int defense, int maxEnergy)
        {
            this.maxHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.maxEnergy = maxEnergy;
        }

        public static CharacterStats operator +(CharacterStats a, CharacterStats b) =>
            new(a.maxHealth + b.maxHealth, a.attack + b.attack, a.defense + b.defense, a.maxEnergy + b.maxEnergy);

        public bool IsZero => maxHealth == 0 && attack == 0 && defense == 0 && maxEnergy == 0;

        public override string ToString() => $"vida {maxHealth}, ataque {attack}, defensa {defense}, energía {maxEnergy}";
    }

    /// <summary>Efecto de un consumible: vida y energía que recupera y defensa extra durante unos segundos.</summary>
    [Serializable]
    public struct ConsumableEffect
    {
        public int heal;
        public int energy;
        public int defenseBonus;
        public float duration;

        public bool IsEmpty => heal == 0 && energy == 0 && defenseBonus == 0;
    }

    public interface IItemInfo
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        ItemCategory Category { get; }
        Rarity Rarity { get; }
        Origin Origin { get; }
        int MaxStack { get; }
        /// <summary>Ecos que da el comerciante por una unidad (0 = no se compra).</summary>
        int Value { get; }
        /// <summary>Se puede vender o entregar en un trueque. Los objetos únicos y narrativos, nunca.</summary>
        bool Sellable { get; }
        /// <summary>Bonificación mientras está equipada (reliquias).</summary>
        CharacterStats Bonus { get; }
        /// <summary>Si no está vacío, la reliquia solo funciona con este Guardián (p. ej. "jaguar").</summary>
        string GuardianOnly { get; }
        ConsumableEffect Effect { get; }
        /// <summary>Entrada del Museo de la Memoria que desbloquea al obtenerlo (bandera "registro.id"), si tiene.</summary>
        string RecordEntry { get; }
    }

    public interface IItemCatalog
    {
        IItemInfo Find(string itemId);
        IReadOnlyList<IItemInfo> All { get; }
    }

    public enum BlessingSlot { Principal, Secundaria }

    public interface IBlessingInfo
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        BlessingSlot Slot { get; }
        /// <summary>Bonificación pasiva mientras está equipada.</summary>
        CharacterStats Bonus { get; }
        int EnergyCost { get; }
        float Cooldown { get; }
        string CombatEffect { get; }
        string ExplorationEffect { get; }
    }

    public interface IBlessingCatalog
    {
        IBlessingInfo Find(string blessingId);
        IReadOnlyList<IBlessingInfo> All { get; }
    }

    [Serializable]
    public struct ItemStack
    {
        public string id;
        public int count;

        public ItemStack(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }

    /// <summary>
    /// Lo que se entrega de una vez: experiencia, objetos, banderas y, si toca, una Bendición. Lo arman las tablas de
    /// botín y los lotes de recompensa de Data, y lo entrega RewardService.
    /// </summary>
    public sealed class RewardPackage
    {
        public int Experience;
        public readonly List<ItemStack> Items = new();
        public readonly List<string> Flags = new();
        public string Blessing;

        public bool IsEmpty => Experience <= 0 && Items.Count == 0 && Flags.Count == 0 && string.IsNullOrEmpty(Blessing);

        public RewardPackage Add(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
                return this;
            for (var i = 0; i < Items.Count; i++)
            {
                if (Items[i].id != itemId)
                    continue;
                Items[i] = new ItemStack(itemId, Items[i].count + count);
                return this;
            }
            Items.Add(new ItemStack(itemId, count));
            return this;
        }

        public void Merge(RewardPackage other)
        {
            if (other == null)
                return;
            Experience += other.Experience;
            foreach (var stack in other.Items)
                Add(stack.id, stack.count);
            Flags.AddRange(other.Flags);
            if (!string.IsNullOrEmpty(other.Blessing))
                Blessing = other.Blessing;
        }
    }

    /// <summary>Atributos de un Guardián según su nivel: cada animal crece a su manera (GuardianData).</summary>
    public interface IStatProfile
    {
        string GuardianId { get; }
        CharacterStats At(int level);
    }

    /// <summary>
    /// Reglas de la progresión (ConfigProgresion en Data): curva de experiencia, protección contra el farmeo y el
    /// Guardián en uso. Todos los números son configurables sin tocar código.
    /// </summary>
    public interface IProgressionConfig
    {
        IStatProfile Profile { get; }
        int MaxLevel { get; }
        /// <summary>Experiencia para pasar de <paramref name="level"/> al siguiente.</summary>
        int ExperienceToNext(int level);
        /// <summary>Lo que se pierde por cada nivel del jugador por encima del recomendado de la zona (0–1).</summary>
        float OverLevelPenalty { get; }
        /// <summary>Lo mínimo que se conserva por estar por encima del nivel de la zona (0–1).</summary>
        float OverLevelFloor { get; }
        /// <summary>Lo que se conserva cada vez que se vuelve a vencer al mismo enemigo (0–1).</summary>
        float RepeatFactor { get; }
        /// <summary>Por debajo de este multiplicador, un enemigo repetido ya no da experiencia.</summary>
        float RepeatCutoff { get; }
    }
}
