using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Rewards;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Interfaz de recompensas y progresión (Docs/Sistemas/Sistema_Recompensas.md §14), en el mismo documento que el HUD:
    /// - Barras de energía y experiencia con el nivel, y avisos breves de lo obtenido (como mucho tres a la vez).
    /// - El menú (acción Menu, tecla I, o el botón «Bolsa» en pantallas táctiles): bolsa e inventario, Bendiciones y, si
    ///   se abrió desde el mundo, fabricación (artesano), comercio (comerciante) o el Museo de la Memoria (santuario).
    /// - Las pantallas de cierre de mazmorra: elección de Bendición secundaria y resultados.
    /// El mundo pide pantallas por el canal EVT_Menu_Abrir (MenuTabs). El menú pausa el juego mientras está abierto.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameMenuController : MonoBehaviour
    {
        const string Hidden = "hidden";
        const int MaxToasts = 3;
        const float ToastSeconds = 2.6f;

        [SerializeField, Tooltip("Canal por el que el mundo pide abrir una pantalla (MenuTabs).")] StringEventChannel menuChannel;
        [SerializeField, Tooltip("Acción que abre y cierra el menú (Player/Menu).")] InputActionReference menuAction;
        [SerializeField, Tooltip("Acción de opciones (Player/Options): con el menú abierto, lo cierra.")] InputActionReference optionsAction;

        static readonly string[] RarityNames = { "Común", "Poco común", "Raro", "Épico", "Legendario" };
        static readonly string[] RarityClasses = { "rareza-comun", "rareza-pococomun", "rareza-raro", "rareza-epico", "rareza-legendario" };
        static readonly string[] OriginNames = { "Documentado", "Interpretación", "Ficción del juego" };

        /// <summary>Hay una pantalla del menú abierta (el panel de calidad no responde a Esc mientras tanto).</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>Frame en que se cerró por última vez (para que la misma pulsación de Esc no abra las opciones).</summary>
        public static int ClosedFrame { get; private set; } = -1;

        VisualElement progress, energyFill, xpFill, toasts, menu, tabs;
        Label energyText, levelText, summary;
        ScrollView content;
        Inventory inventory;
        Progression progression;
        RewardService rewards;
        StoryState story;
        ItemCatalog items;
        BlessingCatalog blessings;
        EconomyCatalog economy;
        MuseumCatalog museum;
        string currentTab;
        string contextTab;
        float pausedTimeScale = 1f;
        readonly Queue<RewardNotice> pending = new();
        readonly List<(VisualElement element, float until)> shown = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            progress = root.Q("progress");
            energyFill = root.Q("energy-fill");
            xpFill = root.Q("xp-fill");
            energyText = root.Q<Label>("energy-text");
            levelText = root.Q<Label>("level-text");
            toasts = root.Q("toasts");
            menu = root.Q("game-menu");
            tabs = root.Q("menu-tabs");
            summary = root.Q<Label>("menu-summary");
            content = root.Q<ScrollView>("menu-content");
            root.Q<Button>("menu-button")?.RegisterCallback<ClickEvent>(_ => Toggle());
            root.Q<Button>("menu-close")?.RegisterCallback<ClickEvent>(_ => Close());

            items = Resources.Load<ItemCatalog>(ItemCatalog.ResourcePath);
            blessings = Resources.Load<BlessingCatalog>(BlessingCatalog.ResourcePath);
            economy = Resources.Load<EconomyCatalog>(EconomyCatalog.ResourcePath);
            museum = Resources.Load<MuseumCatalog>(MuseumCatalog.ResourcePath);

            menu?.AddToClassList(Hidden);
            if (menuChannel != null)
                menuChannel.Subscribe(Open);
            menuAction?.action.Enable();
            optionsAction?.action.Enable();
            Connect();
        }

        void OnDisable()
        {
            if (menuChannel != null)
                menuChannel.Unsubscribe(Open);
            menuAction?.action.Disable();
            Disconnect();
            if (IsOpen)
                Close();
        }

        // Al pulsar Play desde una escena de juego, Boot (que registra los servicios) se carga después.
        void Connect()
        {
            if (progression != null || !ServiceLocator.TryGet(out progression))
                return;
            ServiceLocator.TryGet(out inventory);
            ServiceLocator.TryGet(out rewards);
            ServiceLocator.TryGet(out story);
            progression.EnergyChanged += RefreshBars;
            progression.StatsChanged += RefreshBars;
            progression.ExperienceGained += OnExperience;
            if (rewards != null)
                rewards.Notified += Enqueue;
            if (inventory != null)
                inventory.Changed += RefreshOpen;
            progress?.RemoveFromClassList(Hidden);
            RefreshBars();
        }

        void Disconnect()
        {
            if (progression == null)
                return;
            progression.EnergyChanged -= RefreshBars;
            progression.StatsChanged -= RefreshBars;
            progression.ExperienceGained -= OnExperience;
            if (rewards != null)
                rewards.Notified -= Enqueue;
            if (inventory != null)
                inventory.Changed -= RefreshOpen;
            progression = null;
        }

        void Update()
        {
            if (progression == null)
                Connect();

            if (menuAction != null && menuAction.action.WasPressedThisFrame())
                Toggle();
            else if (IsOpen && optionsAction != null && optionsAction.action.WasPressedThisFrame())
                Close();

            UpdateToasts();
        }

        // ---------- HUD: barras y avisos ----------

        void OnExperience(int amount) => RefreshBars();

        void RefreshBars()
        {
            if (progression == null || energyFill == null)
                return;
            var stats = progression.Stats;
            energyFill.style.width = Length.Percent(stats.maxEnergy > 0 ? 100f * progression.Energy / stats.maxEnergy : 0f);
            energyText.text = $"Energía {progression.Energy}/{stats.maxEnergy}";
            levelText.text = $"Nv {progression.Level}";
            var next = progression.ExperienceToNext;
            xpFill.style.width = Length.Percent(next > 0 ? 100f * progression.Experience / next : 100f);
        }

        void Enqueue(RewardNotice notice) => pending.Enqueue(notice);

        void UpdateToasts()
        {
            var now = Time.unscaledTime;
            for (var i = shown.Count - 1; i >= 0; i--)
            {
                if (now < shown[i].until)
                    continue;
                shown[i].element.RemoveFromHierarchy();
                shown.RemoveAt(i);
            }
            while (pending.Count > 0 && shown.Count < MaxToasts && toasts != null)
            {
                var toast = BuildToast(pending.Dequeue());
                toasts.Add(toast);
                shown.Add((toast, now + ToastSeconds));
            }
        }

        VisualElement BuildToast(RewardNotice notice)
        {
            var toast = new VisualElement();
            toast.AddToClassList("hud-toast");
            string text;
            switch (notice.Kind)
            {
                case RewardNoticeKind.Item:
                    var item = items != null ? items.Get(notice.Id) : null;
                    text = $"+{notice.Amount} {item?.DisplayName ?? notice.Id}";
                    if (item != null)
                    {
                        toast.AddToClassList(RarityClasses[(int)item.Rarity]);
                        if (item.Icon != null)
                            toast.Add(Icon(item.Icon, "hud-toast__icon"));
                        if (item.Rarity >= Rarity.Raro)
                            text += $" ({RarityNames[(int)item.Rarity]})";
                    }
                    break;
                case RewardNoticeKind.Experience:
                    text = $"+{notice.Amount} de experiencia";
                    break;
                case RewardNoticeKind.LevelUp:
                    text = $"¡Nivel {notice.Amount}! Tus atributos mejoran.";
                    toast.AddToClassList(RarityClasses[(int)Rarity.Legendario]);
                    break;
                default:
                    text = $"Bendición: {blessings?.Get(notice.Id)?.DisplayName ?? notice.Id}";
                    toast.AddToClassList(RarityClasses[(int)Rarity.Epico]);
                    break;
            }
            var label = new Label(text);
            label.AddToClassList("hud-toast__text");
            toast.Add(label);
            return toast;
        }

        static VisualElement Icon(Sprite sprite, string className)
        {
            var icon = new VisualElement();
            icon.AddToClassList(className);
            icon.style.backgroundImage = new StyleBackground(sprite);
            return icon;
        }

        // ---------- Menú ----------

        void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open(MenuTabs.Inventory);
        }

        /// <summary>Abre (o cambia a) una pantalla del menú (MenuTabs).</summary>
        public void Open(string tab)
        {
            if (menu == null || string.IsNullOrEmpty(tab))
                return;
            if (tab is MenuTabs.Crafting or MenuTabs.Trading or MenuTabs.Museum)
                contextTab = tab;
            else if (!IsOpen && tab is MenuTabs.Inventory or MenuTabs.Blessings)
                contextTab = null;

            if (!IsOpen)
            {
                pausedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                IsOpen = true;
                menu.RemoveFromClassList(Hidden);
            }
            currentTab = tab;
            Render();
        }

        public void Close()
        {
            if (menu == null)
                return;
            menu.AddToClassList(Hidden);
            if (IsOpen)
                Time.timeScale = pausedTimeScale;
            IsOpen = false;
            ClosedFrame = Time.frameCount;
            currentTab = null;
            contextTab = null;
        }

        public string CurrentTab => currentTab;

        void RefreshOpen()
        {
            if (IsOpen)
                Render();
        }

        void Render()
        {
            if (content == null)
                return;
            content.Clear();
            RenderTabs();
            summary.text = Summary();
            if (progression == null || inventory == null)
            {
                Note("Los sistemas de progresión no están cargados (abre la escena desde Boot).");
                return;
            }

            switch (currentTab)
            {
                case MenuTabs.Blessings: RenderBlessings(); break;
                case MenuTabs.Crafting: RenderCrafting(); break;
                case MenuTabs.Trading: RenderTrading(); break;
                case MenuTabs.Museum: RenderMuseum(); break;
                case MenuTabs.BlessingChoice: RenderChoice(); break;
                case MenuTabs.Results: RenderResults(); break;
                default: RenderInventory(); break;
            }
        }

        void RenderTabs()
        {
            tabs.Clear();
            if (currentTab is MenuTabs.BlessingChoice or MenuTabs.Results)
                return; // pantallas de cierre, sin pestañas
            AddTab(MenuTabs.Inventory, "Bolsa");
            AddTab(MenuTabs.Blessings, "Bendiciones");
            if (contextTab == MenuTabs.Crafting)
                AddTab(MenuTabs.Crafting, "Fabricar");
            if (contextTab == MenuTabs.Trading)
                AddTab(MenuTabs.Trading, "Comerciar");
            if (contextTab == MenuTabs.Museum)
                AddTab(MenuTabs.Museum, "Museo de la Memoria");
        }

        void AddTab(string tab, string label)
        {
            var button = new Button(() => Open(tab)) { text = label };
            button.AddToClassList("hud-menu__tab");
            button.EnableInClassList("hud-menu__tab--selected", tab == currentTab);
            tabs.Add(button);
        }

        string Summary()
        {
            if (progression == null)
                return string.Empty;
            var stats = progression.Stats;
            return $"Nivel {progression.Level} · Experiencia {progression.Experience}/{progression.ExperienceToNext} · " +
                   $"Vida {stats.maxHealth} · Ataque {stats.attack} · Defensa {stats.defense} · " +
                   $"Energía {progression.Energy}/{stats.maxEnergy} · Ecos {inventory?.Count(Inventory.Currency) ?? 0}";
        }

        // ---------- Pantallas ----------

        void RenderInventory()
        {
            var order = new[] { ItemCategory.Consumible, ItemCategory.Reliquia, ItemCategory.Material, ItemCategory.Coleccionable, ItemCategory.Narrativo };
            var titles = new[] { "Consumibles (tecla 1 usa el más útil)", "Reliquias (2 equipadas como máximo)", "Materiales", "Coleccionables", "Objetos de la historia" };
            var any = false;
            for (var c = 0; c < order.Length; c++)
            {
                var owned = inventory.Items.Select(id => items?.Get(id)).Where(item => item != null && item.Category == order[c]).ToList();
                if (owned.Count == 0)
                    continue;
                any = true;
                Section(titles[c]);
                foreach (var item in owned)
                {
                    var row = ItemRow(item, $"{item.DisplayName} ×{inventory.Count(item.Id)} · {RarityNames[(int)item.Rarity]}\n{item.Description}" +
                                            (string.IsNullOrEmpty(item.Usage) ? string.Empty : $"\n{item.Usage}"));
                    if (item.Category == ItemCategory.Consumible)
                        RowButton(row, "Usar", () => inventory.RequestUse(item.Id), true);
                    else if (item.Category == ItemCategory.Reliquia)
                    {
                        var equipped = inventory.IsEquipped(item.Id);
                        RowButton(row, equipped ? "Quitar" : "Equipar", () =>
                        {
                            if (equipped)
                                inventory.UnequipRelic(item.Id);
                            else
                                inventory.EquipRelic(item.Id);
                            Render();
                        }, equipped || inventory.EquippedRelics.Count < Inventory.RelicSlots);
                    }
                }
            }
            if (!any)
                Note("La bolsa está vacía. Vence enemigos, abre ofrendas y explora para encontrar materiales.");
        }

        void RenderBlessings()
        {
            Section("Bendición principal (la da la historia; no se sustituye)");
            BlessingRow(progression.PrimaryBlessing, "Vacía: la concede el jefe de cada cultura al liberarlo.");
            Section("Bendición secundaria (se elige)");
            BlessingRow(progression.SecondaryBlessing, "Vacía: se elige una al completar una mazmorra.");

            var others = progression.UnlockedBlessings
                .Where(id => id != progression.PrimaryBlessing && id != progression.SecondaryBlessing)
                .Select(id => blessings?.Get(id)).Where(b => b != null && b.Slot == BlessingSlot.Secundaria).ToList();
            if (others.Count > 0)
            {
                Section("Otras Bendiciones desbloqueadas");
                foreach (var blessing in others)
                {
                    var row = Row(BlessingText(blessing), null);
                    RowButton(row, "Equipar", () =>
                    {
                        progression.EquipSecondary(blessing.Id);
                        Render();
                    }, true);
                }
            }

            // Si se cerró la elección sin escoger, se puede hacer aquí.
            if (string.IsNullOrEmpty(progression.SecondaryBlessing) && PendingChoices().Count > 0)
            {
                Section("Por elegir");
                RenderChoiceRows();
            }
        }

        void BlessingRow(string id, string empty)
        {
            var blessing = blessings?.Get(id);
            Row(blessing != null ? BlessingText(blessing) : empty, blessing != null ? RarityClasses[(int)Rarity.Epico] : null);
        }

        static string BlessingText(BlessingData blessing)
        {
            var text = $"{blessing.DisplayName}\n{blessing.Description}";
            if (!string.IsNullOrEmpty(blessing.CombatEffect))
                text += $"\nCombate: {blessing.CombatEffect}";
            if (!string.IsNullOrEmpty(blessing.ExplorationEffect))
                text += $"\nExploración: {blessing.ExplorationEffect}";
            if (blessing.EnergyCost > 0)
                text += $"\nEnergía: {blessing.EnergyCost} · Espera: {blessing.Cooldown:0.#} s";
            if (!blessing.Bonus.IsZero)
                text += $"\nBonificación: {Bonus(blessing.Bonus)}";
            return text;
        }

        static string Bonus(CharacterStats stats)
        {
            var parts = new List<string>();
            if (stats.maxHealth != 0) parts.Add($"vida {stats.maxHealth:+0;-0}");
            if (stats.attack != 0) parts.Add($"ataque {stats.attack:+0;-0}");
            if (stats.defense != 0) parts.Add($"defensa {stats.defense:+0;-0}");
            if (stats.maxEnergy != 0) parts.Add($"energía {stats.maxEnergy:+0;-0}");
            return string.Join(", ", parts);
        }

        void RenderCrafting()
        {
            Note("El artesano trabaja con lo que traes. Las recetas se desbloquean con misiones y descubrimientos.");
            if (economy == null)
                return;
            var locked = 0;
            Section("Recetas");
            foreach (var recipe in economy.Recipes)
            {
                if (recipe == null)
                    continue;
                if (!Economy.IsUnlocked(recipe.UnlockFlag, story))
                {
                    locked++;
                    continue;
                }
                var ingredients = string.Join(", ", recipe.Ingredients.Select(i =>
                    $"{items?.Get(i.id)?.DisplayName ?? i.id} {inventory.Count(i.id)}/{i.count}"));
                var missing = Economy.Missing(recipe, inventory);
                var text = $"{recipe.Result?.DisplayName} ×{recipe.ResultCount}\nNecesita: {ingredients}" +
                           (missing.Count > 0 ? $"\nFalta: {string.Join(", ", missing.Select(m => $"{items?.Get(m.id)?.DisplayName ?? m.id} ×{m.count}"))}" : string.Empty) +
                           (recipe.Result != null && !string.IsNullOrEmpty(recipe.Result.Usage) ? $"\n{recipe.Result.Usage}" : string.Empty);
                var row = ItemRow(recipe.Result, text);
                RowButton(row, "Fabricar", () =>
                {
                    if (Economy.Craft(recipe, inventory, story))
                        summary.text = $"Fabricaste: {recipe.Result.DisplayName}.";
                }, Economy.CanCraft(recipe, inventory, story));
            }
            if (locked > 0)
                Note($"{locked} receta(s) por descubrir.");
        }

        void RenderTrading()
        {
            Note("Los «ecos» son una unidad ficticia del Mundo de las Memorias, no una moneda histórica. " +
                 "El comerciante también acepta materiales en trueque.");
            if (economy == null)
                return;
            Section("Ofertas");
            foreach (var offer in economy.Offers)
            {
                if (offer == null || !Economy.IsUnlocked(offer.UnlockFlag, story))
                    continue;
                var row = ItemRow(offer.Item, $"{offer.Item?.DisplayName} ×{offer.ItemCount} por {offer.Price?.DisplayName} ×{offer.PriceCount} " +
                                              $"(tienes {inventory.Count(offer.PriceId)})\n{offer.Item?.Description}");
                RowButton(row, "Cambiar", () => Economy.Buy(offer, inventory, story), Economy.CanBuy(offer, inventory, story));
            }

            var sellable = inventory.Items.Where(id => Economy.CanSell(id, inventory)).Select(id => items?.Get(id)).Where(i => i != null).ToList();
            if (sellable.Count == 0)
                return;
            Section("Vender (los objetos únicos y de la historia nunca se venden)");
            foreach (var item in sellable)
            {
                var row = ItemRow(item, $"{item.DisplayName} ×{inventory.Count(item.Id)} → {item.Value} ecos cada uno");
                RowButton(row, "Vender uno", () => Economy.Sell(item.Id, inventory), true);
            }
        }

        void RenderMuseum()
        {
            Note("El Museo de la Memoria guarda lo que has recuperado. (D = documentado, I = interpretación, F = ficción del juego.)");
            if (museum != null)
            {
                Section("Culturas");
                foreach (var culture in museum.Cultures)
                {
                    if (culture == null)
                        continue;
                    var discovered = story != null && story.HasFlag(culture.DiscoveredFlag);
                    Row(discovered ? $"{culture.Title}\n{culture.Summary}" : "??? — Una memoria todavía por recuperar.", null);
                    if (!discovered)
                        continue;
                    foreach (var guardian in culture.Guardians)
                    {
                        var freed = story.HasFlag(guardian.defeatedFlag);
                        Row(freed ? $"Guardián liberado: {guardian.name}\n{guardian.memory}" : $"Guardián: {guardian.name} — aún corrompido.",
                            freed ? RarityClasses[(int)Rarity.Legendario] : null);
                    }
                }
            }

            ItemSection("Reliquias", ItemCategory.Reliquia);
            Section("Bendiciones");
            var unlocked = progression.UnlockedBlessings.Select(id => blessings?.Get(id)).Where(b => b != null).ToList();
            if (unlocked.Count == 0)
                Note("Ninguna todavía.");
            foreach (var blessing in unlocked)
                Row($"{blessing.DisplayName} ({(blessing.Slot == BlessingSlot.Principal ? "principal" : "secundaria")})\n{blessing.Description}",
                    RarityClasses[(int)Rarity.Epico]);
            ItemSection("Fragmentos de memoria y objetos de la historia", ItemCategory.Narrativo);
            ItemSection("Coleccionables", ItemCategory.Coleccionable);
        }

        void ItemSection(string title, ItemCategory category)
        {
            Section(title);
            var owned = inventory.Items.Select(id => items?.Get(id)).Where(i => i != null && i.Category == category).ToList();
            if (owned.Count == 0)
            {
                Note("Nada todavía.");
                return;
            }
            foreach (var item in owned)
                ItemRow(item, $"{item.DisplayName}{(inventory.Count(item.Id) > 1 ? $" ×{inventory.Count(item.Id)}" : string.Empty)} · " +
                              $"{OriginNames[(int)item.Origin]}\n{item.Description}" +
                              (string.IsNullOrEmpty(item.OriginNote) ? string.Empty : $"\n{item.OriginNote}"));
        }

        List<BlessingData> PendingChoices()
        {
            if (blessings == null || !string.IsNullOrEmpty(progression.SecondaryBlessing))
                return new List<BlessingData>();
            // Se ofrecen las secundarias de las culturas cuyo jefe ya entregó su recompensa.
            return blessings.Blessings.Where(b => b != null && b.Slot == BlessingSlot.Secundaria &&
                                                  rewards != null && rewards.WasGranted("jefe." + b.Culture)).ToList();
        }

        void RenderChoice()
        {
            Section("El Guardián liberado te ofrece una Bendición más");
            Note("Elige una. La Bendición de la Tierra es tuya en todo caso; esta ocupa la ranura secundaria.");
            RenderChoiceRows();
        }

        void RenderChoiceRows()
        {
            foreach (var blessing in PendingChoices())
            {
                var row = Row(BlessingText(blessing), RarityClasses[(int)Rarity.Epico]);
                RowButton(row, "Elegir", () =>
                {
                    progression.UnlockBlessing(blessing.Id);
                    progression.EquipSecondary(blessing.Id);
                    if (currentTab == MenuTabs.BlessingChoice && rewards?.LastResult != null)
                        Open(MenuTabs.Results);
                    else
                        Render();
                }, true);
            }
        }

        void RenderResults()
        {
            var result = rewards?.LastResult;
            if (result == null)
            {
                Note("No hay resultados que mostrar.");
                return;
            }
            Section($"Mazmorra completada: {result.Title}");
            var grade = new Label(result.Grade);
            grade.AddToClassList("hud-menu__grade");
            content.Add(grade);
            Note($"Ofrendas ocultas: {result.SecretsFound}/{result.SecretsTotal} · {(result.Fallen ? "Caíste en la incursión" : "Sin caer")}");
            Note("La nota da recompensas extra, pero nunca bloquea nada necesario para la historia.");
            if (result.Rewards != null && !result.Rewards.IsEmpty)
            {
                Section("Recompensa por la nota");
                foreach (var stack in result.Rewards.Items)
                    ItemRow(items?.Get(stack.id), $"{items?.Get(stack.id)?.DisplayName ?? stack.id} ×{stack.count}");
                if (result.Rewards.Experience > 0)
                    Row($"{result.Rewards.Experience} de experiencia", null);
            }
            var continueButton = new Button(Close) { text = "Continuar" };
            continueButton.AddToClassList("hud-menu__row-button");
            content.Add(continueButton);
        }

        // ---------- Piezas ----------

        void Section(string text)
        {
            var label = new Label(text);
            label.AddToClassList("hud-menu__section");
            content.Add(label);
        }

        void Note(string text)
        {
            var label = new Label(text);
            label.AddToClassList("hud-menu__note");
            content.Add(label);
        }

        VisualElement Row(string text, string rarityClass)
        {
            var row = new VisualElement();
            row.AddToClassList("hud-menu__row");
            if (rarityClass != null)
                row.AddToClassList(rarityClass);
            var label = new Label(text);
            label.AddToClassList("hud-menu__row-text");
            row.Add(label);
            content.Add(row);
            return row;
        }

        VisualElement ItemRow(ItemData item, string text)
        {
            var row = Row(text, item != null ? RarityClasses[(int)item.Rarity] : null);
            if (item != null && item.Icon != null)
                row.Insert(0, Icon(item.Icon, "hud-toast__icon"));
            return row;
        }

        void RowButton(VisualElement row, string text, System.Action action, bool enabled)
        {
            var button = new Button(() =>
            {
                action();
                if (IsOpen)
                    Render();
            }) { text = text };
            button.AddToClassList("hud-menu__row-button");
            button.SetEnabled(enabled);
            row.Add(button);
        }
    }
}
