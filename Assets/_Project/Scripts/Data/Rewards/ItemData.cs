using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Rewards
{
    /// <summary>
    /// Un objeto del juego (Docs/Contenido/Catalogo_Objetos_Olmeca.md): material, consumible, reliquia, coleccionable,
    /// objeto narrativo o la moneda ficticia (ecos). Indica si lo que representa está documentado, es interpretación
    /// o es ficción (Diseno.md §3).
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Recompensas/Objeto", fileName = "OBJ_")]
    public sealed class ItemData : ScriptableObject, IItemInfo
    {
        [SerializeField, Tooltip("Identificador estable: es lo que se guarda.")] string id;
        [SerializeField] string displayName;
        [SerializeField, TextArea(2, 5)] string description;
        [SerializeField] ItemCategory category;
        [SerializeField] Rarity rarity;
        [SerializeField] Origin origin = Origin.Ficticio;
        [SerializeField, TextArea(2, 4), Tooltip("Qué hay de documentado, interpretado o inventado (para el Museo).")]
        string originNote;
        [SerializeField] Sprite icon;
        [SerializeField, Min(1)] int maxStack = 99;
        [SerializeField, Min(0), Tooltip("Ecos que da el comerciante por una unidad (0 = no la compra).")] int value;
        [SerializeField, Tooltip("Se puede vender o dar en trueque. Desmarcado en lo único y narrativo.")] bool sellable = true;
        [SerializeField, Tooltip("Reliquias: bonificación mientras está equipada.")] CharacterStats bonus;
        [SerializeField, Tooltip("Reliquias de un solo animal (id del Guardián); vacío = cualquiera.")] string guardianOnly;
        [SerializeField, Tooltip("Consumibles.")] ConsumableEffect effect;
        [SerializeField, Tooltip("Entrada del Museo que desbloquea al obtenerlo (registro.<id>).")] string recordEntry;
        [SerializeField, Tooltip("Para qué sirve (se muestra en el inventario).")] string usage;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public Rarity Rarity => rarity;
        public Origin Origin => origin;
        public string OriginNote => originNote;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public int Value => value;
        public bool Sellable => sellable;
        public CharacterStats Bonus => bonus;
        public string GuardianOnly => guardianOnly;
        public ConsumableEffect Effect => effect;
        public string RecordEntry => recordEntry;
        public string Usage => usage;

        /// <summary>Lo usa el generador de contenido (OlmecaRewardAssets) para crear y actualizar los objetos.</summary>
        public void Configure(string id, string displayName, string description, ItemCategory category, Rarity rarity,
            Origin origin, string originNote, string usage, int maxStack, int value, bool sellable,
            CharacterStats bonus = default, ConsumableEffect effect = default, string guardianOnly = null, string recordEntry = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.category = category;
            this.rarity = rarity;
            this.origin = origin;
            this.originNote = originNote;
            this.usage = usage;
            this.maxStack = maxStack;
            this.value = value;
            this.sellable = sellable;
            this.bonus = bonus;
            this.effect = effect;
            this.guardianOnly = guardianOnly;
            this.recordEntry = recordEntry;
        }

        public void SetIcon(Sprite sprite) => icon = sprite;
    }
}
