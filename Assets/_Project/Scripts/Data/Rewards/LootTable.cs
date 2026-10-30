using System;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Tabla de botín (Docs/Contenido/Tabla_Recompensas_Olmeca.md): experiencia, lo garantizado y lo que sale con
    /// probabilidad. Cada tipo de enemigo y de cofre tiene la suya. Lo importante para la historia nunca va en la parte
    /// aleatoria: va en «garantizado» o en un RewardBundle.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Tabla de botín", fileName = "BOT_")]
    public sealed class LootTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ItemData item;
            [Range(0f, 1f)] public float chance;
            [Min(1)] public int min;
            [Min(1)] public int max;

            public Entry(ItemData item, float chance, int min, int max)
            {
                this.item = item;
                this.chance = chance;
                this.min = min;
                this.max = max;
            }
        }

        [SerializeField, Min(0), Tooltip("Experiencia base (antes de la protección contra el farmeo).")] int experience;
        [SerializeField, Tooltip("Sale siempre.")] Entry[] guaranteed = Array.Empty<Entry>();
        [SerializeField, Tooltip("Cada entrada se tira por separado con su probabilidad.")] Entry[] chances = Array.Empty<Entry>();

        public int Experience => experience;
        public Entry[] Guaranteed => guaranteed;
        public Entry[] Chances => chances;

        public void Configure(int experience, Entry[] guaranteed, Entry[] chances)
        {
            this.experience = experience;
            this.guaranteed = guaranteed ?? Array.Empty<Entry>();
            this.chances = chances ?? Array.Empty<Entry>();
        }

        /// <summary>Tira la tabla con el azar dado (RewardService.Random, o uno con semilla en las pruebas).</summary>
        public RewardPackage Roll(System.Random random)
        {
            var package = new RewardPackage { Experience = experience };
            foreach (var entry in guaranteed)
                if (entry.item != null)
                    package.Add(entry.item.Id, Amount(entry, random));
            foreach (var entry in chances)
                if (entry.item != null && random.NextDouble() < entry.chance)
                    package.Add(entry.item.Id, Amount(entry, random));
            return package;
        }

        static int Amount(Entry entry, System.Random random) =>
            entry.max > entry.min ? random.Next(entry.min, entry.max + 1) : Mathf.Max(1, entry.min);
    }
}
