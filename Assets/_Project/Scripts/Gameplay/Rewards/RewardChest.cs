using System.Text;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Gameplay.Interaction;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Rewards
{
    /// <summary>Tipos de cofre (ofrenda) de Docs/Sistemas/Sistema_Recompensas.md; cada uno con su tabla.</summary>
    public enum ChestKind { Comun, Especial, Oculto, Guardian, Final }

    /// <summary>
    /// Ofrenda (el «cofre» del juego: en La Venta se enterraban ofrendas, D): se abre una sola vez en toda la partida y
    /// entrega lo fijo (RewardBundle) y lo que salga de su tabla. Lo garantizado se entrega al abrir, con una clave única,
    /// así que no se pierde ni se duplica al recargar. Al cerrarse el mensaje activa su bandera ("ofrenda.id") y
    /// ActiveWhenFlags la muestra abierta (como MemoryFragmentInteractable). La final avisa a DungeonCompletion.
    /// </summary>
    public sealed class RewardChest : MonoBehaviour, IConversation
    {
        public const string FlagPrefix = "ofrenda.";

        [SerializeField, Tooltip("Id único de la ofrenda.")] string chestId;
        [SerializeField] ChestKind kind;
        [SerializeField, Tooltip("Lo que sale siempre.")] RewardBundle bundle;
        [SerializeField, Tooltip("Lo que sale con probabilidad.")] LootTable loot;
        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")] StringEventChannel messageChannel;
        [SerializeField, Tooltip("Solo la ofrenda final: quien cierra la mazmorra.")] Dungeon.DungeonCompletion completion;

        bool open;

        public string Prompt => "Abrir la ofrenda";
        public string Flag => FlagPrefix + chestId;
        public ChestKind Kind => kind;

        public string ChestId
        {
            get => chestId;
            set => chestId = value;
        }

        public void Configure(string id, ChestKind chestKind, RewardBundle fixedReward, LootTable table,
            StringEventChannel channel, Dungeon.DungeonCompletion dungeonCompletion = null)
        {
            chestId = id;
            kind = chestKind;
            bundle = fixedReward;
            loot = table;
            messageChannel = channel;
            completion = dungeonCompletion;
        }

        public void Interact(Transform interactor)
        {
            open = true;
            if (!ServiceLocator.TryGet(out RewardService rewards))
                return;

            string text;
            if (rewards.WasGranted(Flag) || (rewards.Story != null && rewards.Story.HasFlag(Flag)))
                text = "La ofrenda ya está vacía.";
            else
            {
                var package = bundle != null ? bundle.ToPackage(rewards.Random) : new RewardPackage();
                if (loot != null)
                    package.Merge(loot.Roll(rewards.Random));
                rewards.Grant(package, Flag);
                text = Describe(package, rewards.Inventory.Catalog);
            }
            if (messageChannel != null)
                messageChannel.Raise(text);
        }

        public bool Advance() => false;

        public void End()
        {
            if (!open)
                return;
            open = false;
            var wasOpened = ServiceLocator.TryGet(out StoryState story) && story.HasFlag(Flag);
            story?.SetFlag(Flag);
            if (!wasOpened && completion != null)
                completion.Finish();
        }

        static string Describe(RewardPackage package, IItemCatalog catalog)
        {
            var text = new StringBuilder("Abres la ofrenda. Encuentras:");
            foreach (var stack in package.Items)
                text.Append("\n· ").Append(catalog?.Find(stack.id)?.DisplayName ?? stack.id).Append(" ×").Append(stack.count);
            if (package.Experience > 0)
                text.Append("\n· ").Append(package.Experience).Append(" de experiencia");
            if (package.Items.Count == 0 && package.Experience <= 0)
                text.Append(" nada más que polvo.");
            return text.ToString();
        }
    }
}
