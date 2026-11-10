using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Save;

namespace CronicasDeMexico.Core.Progress
{
    /// <summary>
    /// Inventario del Guardián (Docs/Sistemas/Sistema_Inventario.md): cuántos hay de cada objeto, con el tope por
    /// objeto de su definición, y las reliquias equipadas (hasta <see cref="RelicSlots"/>). Los «ecos», la unidad de
    /// intercambio ficticia, son un objeto más. Lo registra GameBootstrapper y se guarda con la partida.
    /// </summary>
    public sealed class Inventory : ISaveParticipant
    {
        public const string Currency = "ecos";
        public const int RelicSlots = 2;
        // Tope por defecto si el objeto no está en el catálogo (p. ej. en pruebas).
        const int DefaultMaxStack = 99;

        readonly Dictionary<string, int> counts = new();
        readonly List<string> order = new();
        readonly List<string> equippedRelics = new();

        public Inventory(IItemCatalog catalog = null) => Catalog = catalog;

        public IItemCatalog Catalog { get; }

        /// <summary>Cambió la cantidad de algún objeto.</summary>
        public event Action Changed;

        /// <summary>Cambiaron las reliquias equipadas (afecta a los atributos).</summary>
        public event Action RelicsChanged;

        /// <summary>La interfaz pide usar un consumible; lo aplica quien lleva el personaje (PlayerStats, en Gameplay).</summary>
        public event Action<string> UseRequested;

        public void RequestUse(string itemId) => UseRequested?.Invoke(itemId);

        /// <summary>Objetos con al menos una unidad, en el orden en que se obtuvieron.</summary>
        public IReadOnlyList<string> Items => order;

        public IReadOnlyList<string> EquippedRelics => equippedRelics;

        public int Count(string itemId) => itemId != null && counts.TryGetValue(itemId, out var count) ? count : 0;

        public bool Has(string itemId, int count = 1) => Count(itemId) >= count;

        public int MaxStack(string itemId)
        {
            var max = Catalog?.Find(itemId)?.MaxStack ?? DefaultMaxStack;
            return max > 0 ? max : DefaultMaxStack;
        }

        /// <summary>Añade hasta el tope del objeto. Devuelve cuántos entraron de verdad.</summary>
        public int Add(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
                return 0;
            var current = Count(itemId);
            var added = Math.Min(count, MaxStack(itemId) - current);
            if (added <= 0)
                return 0;

            counts[itemId] = current + added;
            if (current == 0)
                order.Add(itemId);
            Changed?.Invoke();
            return added;
        }

        /// <summary>Quita <paramref name="count"/> si los hay; si no, no quita nada y devuelve false.</summary>
        public bool Remove(string itemId, int count)
        {
            if (count <= 0 || !Has(itemId, count))
                return false;

            var left = counts[itemId] - count;
            if (left > 0)
                counts[itemId] = left;
            else
            {
                counts.Remove(itemId);
                order.Remove(itemId);
                if (equippedRelics.Remove(itemId))
                    RelicsChanged?.Invoke();
            }
            Changed?.Invoke();
            return true;
        }

        public bool IsEquipped(string itemId) => equippedRelics.Contains(itemId);

        /// <summary>Equipa una reliquia que se tiene, si queda ranura libre.</summary>
        public bool EquipRelic(string itemId)
        {
            if (!Has(itemId) || IsEquipped(itemId) || equippedRelics.Count >= RelicSlots)
                return false;
            var info = Catalog?.Find(itemId);
            if (info != null && info.Category != ItemCategory.Reliquia)
                return false;
            equippedRelics.Add(itemId);
            RelicsChanged?.Invoke();
            return true;
        }

        public bool UnequipRelic(string itemId)
        {
            if (!equippedRelics.Remove(itemId))
                return false;
            RelicsChanged?.Invoke();
            return true;
        }

        public void Capture(SaveData data)
        {
            data.items.Clear();
            foreach (var id in order)
                data.items.Add(new ItemStack(id, counts[id]));
            data.equippedRelics.Clear();
            data.equippedRelics.AddRange(equippedRelics);
        }

        public void Restore(SaveData data)
        {
            counts.Clear();
            order.Clear();
            equippedRelics.Clear();
            foreach (var stack in data.items)
                if (!string.IsNullOrEmpty(stack.id) && stack.count > 0 && !counts.ContainsKey(stack.id))
                {
                    counts[stack.id] = stack.count;
                    order.Add(stack.id);
                }
            foreach (var relic in data.equippedRelics)
                if (Has(relic) && !equippedRelics.Contains(relic) && equippedRelics.Count < RelicSlots)
                    equippedRelics.Add(relic);
            Changed?.Invoke();
            RelicsChanged?.Invoke();
        }
    }
}
