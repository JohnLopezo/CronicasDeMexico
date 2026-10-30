using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Save;

namespace CronicasDeMexico.Core.Progress
{
    public enum RewardNoticeKind { Item, Experience, LevelUp, Blessing }

    /// <summary>Algo que se acaba de obtener, para el aviso del HUD.</summary>
    public readonly struct RewardNotice
    {
        public readonly RewardNoticeKind Kind;
        public readonly string Id;
        public readonly int Amount;

        public RewardNotice(RewardNoticeKind kind, string id, int amount)
        {
            Kind = kind;
            Id = id;
            Amount = amount;
        }
    }

    /// <summary>Resultado de completar una mazmorra (pantalla de resultados). No se guarda: se muestra al momento.</summary>
    public sealed class DungeonResult
    {
        public string Title;
        public string Grade;
        public int SecretsFound;
        public int SecretsTotal;
        public bool Fallen;
        public RewardPackage Rewards = new();
    }

    /// <summary>
    /// Entrega recompensas (Docs/Sistemas/Sistema_Recompensas.md): experiencia, objetos, banderas y Bendiciones de un
    /// RewardPackage. Las recompensas únicas (cofres, jefes, misiones, mazmorras) llevan una clave y nunca se entregan
    /// dos veces, ni al recargar la escena ni al cargar la partida. También entrega lo que dan las misiones al
    /// completarse. Avisa de cada cosa obtenida para el HUD. Lo registra GameBootstrapper.
    /// </summary>
    public sealed class RewardService : ISaveParticipant
    {
        /// <summary>Prefijo de las banderas que desbloquean entradas del Museo de la Memoria.</summary>
        public const string RecordPrefix = "registro.";

        readonly HashSet<string> granted = new();

        public RewardService(Inventory inventory, Progression progression, StoryState story, QuestService quests = null)
        {
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Progression = progression ?? throw new ArgumentNullException(nameof(progression));
            Story = story;
            Progression.LevelChanged += level => Notified?.Invoke(new RewardNotice(RewardNoticeKind.LevelUp, null, level));
            if (quests != null)
                quests.Completed += OnQuestCompleted;
        }

        public Inventory Inventory { get; }
        public Progression Progression { get; }
        public StoryState Story { get; }

        /// <summary>Azar de las tablas de botín; las pruebas lo cambian por uno con semilla fija.</summary>
        public Random Random { get; set; } = new();

        /// <summary>Último resultado de mazmorra, para la pantalla de resultados.</summary>
        public DungeonResult LastResult { get; set; }

        public event Action<RewardNotice> Notified;

        public bool WasGranted(string key) => !string.IsNullOrEmpty(key) && granted.Contains(key);

        /// <summary>
        /// Entrega el paquete. Con <paramref name="onceKey"/>, solo la primera vez (devuelve false si ya se dio).
        /// <paramref name="zoneLevel"/> y <paramref name="repeatKey"/> ajustan la experiencia (Progression).
        /// </summary>
        public bool Grant(RewardPackage package, string onceKey = null, int zoneLevel = 0, string repeatKey = null)
        {
            if (package == null || (!string.IsNullOrEmpty(onceKey) && !granted.Add(onceKey)))
                return false;

            if (package.Experience > 0)
            {
                var experience = Progression.AddExperience(package.Experience, zoneLevel, repeatKey);
                if (experience > 0)
                    Notified?.Invoke(new RewardNotice(RewardNoticeKind.Experience, null, experience));
            }
            else if (!string.IsNullOrEmpty(repeatKey))
            {
                Progression.AddExperience(0, zoneLevel, repeatKey);
            }

            foreach (var stack in package.Items)
            {
                var added = Inventory.Add(stack.id, stack.count);
                if (added <= 0)
                    continue;
                var record = Inventory.Catalog?.Find(stack.id)?.RecordEntry;
                if (!string.IsNullOrEmpty(record))
                    Story?.SetFlag(RecordPrefix + record);
                Notified?.Invoke(new RewardNotice(RewardNoticeKind.Item, stack.id, added));
            }

            foreach (var flag in package.Flags)
                Story?.SetFlag(flag);

            if (!string.IsNullOrEmpty(package.Blessing) && Progression.UnlockBlessing(package.Blessing))
                Notified?.Invoke(new RewardNotice(RewardNoticeKind.Blessing, package.Blessing, 1));
            return true;
        }

        void OnQuestCompleted(IQuestDefinition quest)
        {
            var reward = quest.Reward;
            if (reward != null && !reward.IsEmpty)
                Grant(reward, "mision." + quest.Id);
        }

        public void Capture(SaveData data)
        {
            data.grantedRewards.Clear();
            data.grantedRewards.AddRange(granted);
            data.grantedRewards.Sort(StringComparer.Ordinal);
        }

        public void Restore(SaveData data)
        {
            granted.Clear();
            foreach (var key in data.grantedRewards)
                if (!string.IsNullOrEmpty(key))
                    granted.Add(key);
        }
    }
}
