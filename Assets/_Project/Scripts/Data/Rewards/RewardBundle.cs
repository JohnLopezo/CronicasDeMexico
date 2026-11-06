using System;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Recompensa fija: experiencia, objetos, banderas y, si toca, una Bendición. La dan los jefes, los cofres, los
    /// pisos al descubrirlos, las misiones y la nota de la mazmorra. Siempre se entrega completa.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Recompensa fija", fileName = "REC_")]
    public sealed class RewardBundle : ScriptableObject
    {
        [Serializable]
        public struct Stack
        {
            public ItemData item;
            [Min(1)] public int count;

            public Stack(ItemData item, int count)
            {
                this.item = item;
                this.count = count;
            }
        }

        [SerializeField, Min(0)] int experience;
        [SerializeField] Stack[] items = Array.Empty<Stack>();
        [SerializeField, Tooltip("Banderas que activa (p. ej. recetas desbloqueadas, registro.<id>).")] string[] flags = Array.Empty<string>();
        [SerializeField, Tooltip("Opcional: Bendición que desbloquea.")] BlessingData blessing;
        [SerializeField, Tooltip("Opcional: tabla que se tira además de lo fijo.")] LootTable extra;

        public int Experience => experience;
        public Stack[] Items => items;
        public string[] Flags => flags;
        public BlessingData Blessing => blessing;

        public void Configure(int experience, Stack[] items, string[] flags = null, BlessingData blessing = null, LootTable extra = null)
        {
            this.experience = experience;
            this.items = items ?? Array.Empty<Stack>();
            this.flags = flags ?? Array.Empty<string>();
            this.blessing = blessing;
            this.extra = extra;
        }

        /// <summary>Lo fijo; la tabla extra, si la hay, se tira con <paramref name="random"/> (o con uno nuevo).</summary>
        public RewardPackage ToPackage(System.Random random = null)
        {
            var package = new RewardPackage { Experience = experience, Blessing = blessing != null ? blessing.Id : null };
            foreach (var stack in items)
                if (stack.item != null)
                    package.Add(stack.item.Id, stack.count);
            package.Flags.AddRange(flags);
            if (extra != null)
                package.Merge(extra.Roll(random ?? new System.Random()));
            return package;
        }
    }
}
