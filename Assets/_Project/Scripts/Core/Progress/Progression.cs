using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Save;
using UnityEngine;

namespace CronicasDeMexico.Core.Progress
{
    /// <summary>
    /// Progresión del Guardián (Docs/Sistemas/Sistema_Progresion.md): experiencia y nivel, atributos (los del animal
    /// según su nivel, más reliquias equipadas y Bendiciones), energía y Bendiciones desbloqueadas y equipadas.
    /// - La energía NO se regenera sola: se recupera en los santuarios de Memoria, con consumibles y con algunas
    ///   recompensas.
    /// - Experiencia sin farmeo: vencer otra vez al mismo enemigo (reaparece al recargar el piso) da cada vez menos y,
    ///   por debajo de un umbral, nada; estar por encima del nivel de la zona también la reduce.
    /// - Dos ranuras de Bendición: la principal, que da la historia y no se sustituye, y la secundaria, a elegir.
    /// </summary>
    public sealed class Progression : ISaveParticipant
    {
        readonly Inventory inventory;
        readonly IBlessingCatalog blessings;
        readonly Dictionary<string, int> defeats = new();
        readonly List<string> unlocked = new();

        public Progression(IProgressionConfig config = null, Inventory inventory = null, IBlessingCatalog blessings = null)
        {
            Config = config ?? DefaultProgressionConfig.Instance;
            this.inventory = inventory;
            this.blessings = blessings;
            if (inventory != null)
                inventory.RelicsChanged += RaiseStats;
            Level = 1;
            Energy = Stats.maxEnergy;
        }

        public IProgressionConfig Config { get; }
        public IBlessingCatalog BlessingCatalog => blessings;
        public int Level { get; private set; }
        /// <summary>Experiencia acumulada dentro del nivel actual.</summary>
        public int Experience { get; private set; }
        public int ExperienceToNext => Level >= Config.MaxLevel ? 0 : Config.ExperienceToNext(Level);
        public int Energy { get; private set; }
        public string PrimaryBlessing { get; private set; }
        public string SecondaryBlessing { get; private set; }
        public IReadOnlyList<string> UnlockedBlessings => unlocked;

        public event Action<int> ExperienceGained;
        public event Action<int> LevelChanged;
        public event Action StatsChanged;
        public event Action EnergyChanged;
        public event Action BlessingsChanged;

        /// <summary>Atributos actuales: los del animal en su nivel, más reliquias equipadas y Bendiciones.</summary>
        public CharacterStats Stats
        {
            get
            {
                var stats = Config.Profile.At(Level);
                if (inventory?.Catalog != null)
                    foreach (var relic in inventory.EquippedRelics)
                    {
                        var info = inventory.Catalog.Find(relic);
                        if (info != null && (string.IsNullOrEmpty(info.GuardianOnly) || info.GuardianOnly == Config.Profile.GuardianId))
                            stats += info.Bonus;
                    }
                stats += BlessingBonus(PrimaryBlessing) + BlessingBonus(SecondaryBlessing);
                return stats;
            }
        }

        // ---------- Experiencia ----------

        /// <summary>
        /// Suma experiencia. <paramref name="zoneLevel"/> (0 = sin zona) reduce lo que da estar por encima del nivel
        /// recomendado; <paramref name="repeatKey"/> identifica a un enemigo concreto para no dar lo mismo cada vez que
        /// reaparece. Devuelve la experiencia que se sumó de verdad.
        /// </summary>
        public int AddExperience(int amount, int zoneLevel = 0, string repeatKey = null)
        {
            if (amount <= 0)
                return 0;

            var multiplier = 1f;
            if (zoneLevel > 0 && Level > zoneLevel)
                multiplier *= Mathf.Max(Config.OverLevelFloor, 1f - Config.OverLevelPenalty * (Level - zoneLevel));
            if (!string.IsNullOrEmpty(repeatKey))
            {
                defeats.TryGetValue(repeatKey, out var times);
                defeats[repeatKey] = times + 1;
                var repeat = Mathf.Pow(Config.RepeatFactor, times);
                multiplier *= repeat < Config.RepeatCutoff ? 0f : repeat;
            }

            var granted = Mathf.RoundToInt(amount * multiplier);
            if (granted <= 0)
                return 0;

            Experience += granted;
            ExperienceGained?.Invoke(granted);
            var leveled = false;
            while (Level < Config.MaxLevel && Experience >= Config.ExperienceToNext(Level))
            {
                Experience -= Config.ExperienceToNext(Level);
                Level++;
                leveled = true;
            }
            if (Level >= Config.MaxLevel)
                Experience = 0;
            if (leveled)
            {
                LevelChanged?.Invoke(Level);
                RaiseStats();
            }
            return granted;
        }

        /// <summary>Veces que se venció ya a este enemigo concreto.</summary>
        public int DefeatCount(string repeatKey) => repeatKey != null && defeats.TryGetValue(repeatKey, out var n) ? n : 0;

        // ---------- Energía ----------

        public bool SpendEnergy(int amount)
        {
            if (amount < 0 || Energy < amount)
                return false;
            Energy -= amount;
            EnergyChanged?.Invoke();
            return true;
        }

        /// <summary>Recupera energía sin pasar del máximo. Devuelve lo recuperado.</summary>
        public int RestoreEnergy(int amount)
        {
            var restored = Mathf.Clamp(amount, 0, Stats.maxEnergy - Energy);
            if (restored <= 0)
                return 0;
            Energy += restored;
            EnergyChanged?.Invoke();
            return restored;
        }

        public void FillEnergy() => RestoreEnergy(Stats.maxEnergy);

        // ---------- Bendiciones ----------

        public bool HasBlessing(string blessingId) => unlocked.Contains(blessingId);

        /// <summary>
        /// Desbloquea una Bendición. Si es principal y la ranura está vacía, la ocupa (y ya no se sustituye); si es
        /// secundaria y no hay ninguna equipada, se equipa.
        /// </summary>
        public bool UnlockBlessing(string blessingId)
        {
            if (string.IsNullOrEmpty(blessingId) || unlocked.Contains(blessingId))
                return false;
            unlocked.Add(blessingId);
            var info = blessings?.Find(blessingId);
            var slot = info?.Slot ?? BlessingSlot.Secundaria;
            if (slot == BlessingSlot.Principal && string.IsNullOrEmpty(PrimaryBlessing))
                PrimaryBlessing = blessingId;
            else if (slot == BlessingSlot.Secundaria && string.IsNullOrEmpty(SecondaryBlessing))
                SecondaryBlessing = blessingId;
            BlessingsChanged?.Invoke();
            RaiseStats();
            return true;
        }

        /// <summary>Cambia la Bendición secundaria por otra desbloqueada. La principal no se toca.</summary>
        public bool EquipSecondary(string blessingId)
        {
            if (!unlocked.Contains(blessingId) || blessingId == PrimaryBlessing)
                return false;
            if (blessings?.Find(blessingId) is { Slot: BlessingSlot.Principal })
                return false;
            SecondaryBlessing = blessingId;
            BlessingsChanged?.Invoke();
            RaiseStats();
            return true;
        }

        CharacterStats BlessingBonus(string blessingId) =>
            string.IsNullOrEmpty(blessingId) || blessings == null ? default : blessings.Find(blessingId)?.Bonus ?? default;

        void RaiseStats()
        {
            // La energía nunca queda por encima del máximo nuevo (p. ej. al quitar una reliquia).
            var max = Stats.maxEnergy;
            if (Energy > max)
            {
                Energy = max;
                EnergyChanged?.Invoke();
            }
            StatsChanged?.Invoke();
        }

        // ---------- Guardado ----------

        public void Capture(SaveData data)
        {
            data.guardianId = Config.Profile.GuardianId;
            data.level = Level;
            data.experience = Experience;
            data.energy = Energy;
            data.blessings.Clear();
            data.blessings.AddRange(unlocked);
            data.primaryBlessing = PrimaryBlessing;
            data.secondaryBlessing = SecondaryBlessing;
            data.defeats.Clear();
            foreach (var pair in defeats)
                data.defeats.Add(new KeyCount { key = pair.Key, count = pair.Value });
        }

        public void Restore(SaveData data)
        {
            Level = Mathf.Clamp(data.level, 1, Config.MaxLevel);
            Experience = Mathf.Max(0, data.experience);
            unlocked.Clear();
            foreach (var id in data.blessings)
                if (!string.IsNullOrEmpty(id) && !unlocked.Contains(id))
                    unlocked.Add(id);
            PrimaryBlessing = unlocked.Contains(data.primaryBlessing) ? data.primaryBlessing : null;
            SecondaryBlessing = unlocked.Contains(data.secondaryBlessing) ? data.secondaryBlessing : null;
            defeats.Clear();
            foreach (var entry in data.defeats)
                if (!string.IsNullOrEmpty(entry.key))
                    defeats[entry.key] = entry.count;
            var max = Stats.maxEnergy;
            Energy = data.energy < 0 ? max : Mathf.Min(data.energy, max);
            LevelChanged?.Invoke(Level);
            BlessingsChanged?.Invoke();
            StatsChanged?.Invoke();
            EnergyChanged?.Invoke();
        }
    }

    /// <summary>
    /// Reglas por defecto si no hay ConfigProgresion (pruebas, o Boot sin datos): los atributos iniciales del Jaguar en
    /// la escala ×10 (50 de vida, 10 de ataque, 100 de energía) y la misma curva que la configuración olmeca.
    /// </summary>
    public sealed class DefaultProgressionConfig : IProgressionConfig, IStatProfile
    {
        public static readonly DefaultProgressionConfig Instance = new();

        public IStatProfile Profile => this;
        public string GuardianId => "jaguar";
        public int MaxLevel => 20;
        public int ExperienceToNext(int level) => Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f));
        public float OverLevelPenalty => 0.25f;
        public float OverLevelFloor => 0.1f;
        public float RepeatFactor => 0.5f;
        public float RepeatCutoff => 0.2f;

        public CharacterStats At(int level) =>
            new(50 + 6 * (level - 1), 10 + (level - 1) * 3 / 2, (level - 1) / 2, 100 + 5 * (level - 1));
    }
}
