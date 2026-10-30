using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Gameplay.Rewards;
using UnityEditor;
using UnityEngine;
using Shape = CronicasDeMexico.Editor.SceneGeneration.ItemIconFactory.Shape;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Configuración olmeca del sistema de recompensas (M5; Docs/Contenido/Catalogo_Objetos_Olmeca.md y
    /// Tabla_Recompensas_Olmeca.md): objetos, Bendiciones, tablas de botín, ofrendas, recompensas de pisos, jefes, nota y
    /// misiones, recetas del artesano, ofertas del comerciante, la entrada del Museo y la configuración de progresión.
    /// Cada asset se crea solo si falta, así que después se ajusta en el Inspector sin que regenerar lo pise (para volver
    /// a los valores del código, se borra el asset). Los catálogos de Resources se rehacen siempre con lo que hay.
    /// </summary>
    public static class OlmecaRewardAssets
    {
        public const string Folder = "Assets/_Project/Data/Rewards/Olmeca";
        const string ResourcesFolder = "Assets/_Project/Resources/Recompensas";
        public const string Culture = "olmeca";

        // Niveles recomendados por zona (protección contra el farmeo): selva 1, pisos 2, 3 y 4.
        public const int JungleLevel = 1;
        public static int FloorLevel(int floor) => floor + 1;

        // Banderas que desbloquean recetas, ofertas y entradas del Museo.
        public const string RecordCulture = RewardService.RecordPrefix + "cultura." + Culture;
        public const string PrimeMemoryFlag = "olm.primera_memoria";

        public sealed class Content
        {
            public readonly Dictionary<string, ItemData> Items = new();
            public LootTable CommonSpirit, ErasedEcho, StoneJaguar, CommonOffering, SpecialOffering;
            public RewardBundle Boss, Final;
            public RewardBundle[] Grades;
            public readonly Dictionary<string, RewardBundle> Offerings = new();
            public readonly Dictionary<int, RewardBundle> Discoveries = new();
            public BlessingData Earth;
            public ItemData Item(string id) => Items[id];
        }

        static readonly Color32 Jade = new(79, 179, 154, 255), JadeLight = new(155, 227, 200, 255);
        static readonly Color32 Basalt = new(74, 71, 59, 255), BasaltLight = new(125, 116, 91, 255);
        static readonly Color32 Obsidian = new(44, 48, 42, 255), ObsidianLight = new(125, 140, 160, 255);
        static readonly Color32 Earth = new(155, 113, 63, 255), EarthLight = new(227, 180, 124, 255);
        static readonly Color32 Green = new(86, 139, 38, 255), GreenLight = new(146, 181, 53, 255);
        static readonly Color32 Violet = new(123, 79, 176, 255), VioletLight = new(217, 204, 255, 255);
        static readonly Color32 Gold = new(242, 165, 65, 255), GoldLight = new(255, 224, 138, 255);
        static readonly Color32 Clay = new(177, 97, 67, 255), ClayLight = new(198, 146, 90, 255);

        public static Content EnsureAssets(OlmecaNarrativeAssets.Content narrative)
        {
            OlmecaPrototypeGenerator.EnsureFolder(Folder);
            OlmecaPrototypeGenerator.EnsureFolder(ResourcesFolder);
            var content = new Content();

            CreateItems(content);
            CreateBlessings(content, out var blessings);
            CreateLoot(content);
            CreateRewards(content);
            var economy = CreateEconomy(content);
            var museum = CreateMuseum();
            CreateQuestRewards(content, narrative);

            var itemCatalog = LoadOrCreate($"{ResourcesFolder}/CatalogoObjetos.asset", () => ScriptableObject.CreateInstance<ItemCatalog>());
            itemCatalog.SetItems(content.Items.Values);
            EditorUtility.SetDirty(itemCatalog);
            var blessingCatalog = LoadOrCreate($"{ResourcesFolder}/CatalogoBendiciones.asset", () => ScriptableObject.CreateInstance<BlessingCatalog>());
            blessingCatalog.SetBlessings(blessings);
            EditorUtility.SetDirty(blessingCatalog);
            var economyCatalog = LoadOrCreate($"{ResourcesFolder}/CatalogoEconomia.asset", () => ScriptableObject.CreateInstance<EconomyCatalog>());
            economyCatalog.Configure(economy.recipes, economy.offers);
            EditorUtility.SetDirty(economyCatalog);
            var museumCatalog = LoadOrCreate($"{ResourcesFolder}/CatalogoMuseo.asset", () => ScriptableObject.CreateInstance<MuseumCatalog>());
            museumCatalog.SetCultures(new[] { museum });
            EditorUtility.SetDirty(museumCatalog);
            var config = LoadOrCreate($"{ResourcesFolder}/ConfigProgresion.asset", () => ScriptableObject.CreateInstance<ProgressionConfig>());
            if (config.Guardian == null)
            {
                config.Guardian = AssetDatabase.LoadAssetAtPath<GuardianData>(OlmecaNarrativeAssets.GuardianPath);
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            return content;
        }

        // ---------- Objetos ----------

        static void CreateItems(Content c)
        {
            // Materiales
            Item(c, "jade", "Jade", "Piedra verde pulida, traída de muy lejos.", ItemCategory.Material, Rarity.PocoComun, Origin.Documentado,
                "D: en La Venta se trabajaron jade y serpentina traídos de lejos (Docs/Culturas/Olmeca.md).",
                "Para reliquias y para la reliquia espiritual (artesano). El comerciante lo cambia.", 30, 12, Shape.Piedra, Jade, JadeLight);
            Item(c, "obsidiana", "Obsidiana", "Vidrio volcánico de filo cortante.", ItemCategory.Material, Rarity.PocoComun, Origin.Documentado,
                "D: la obsidiana llegaba por intercambio a larga distancia. I: su uso aquí como material de mejora.",
                "Para el talismán de obsidiana (más ataque).", 30, 10, Shape.Lasca, Obsidian, ObsidianLight);
            Item(c, "basalto", "Basalto", "Piedra oscura y pesada, la de los monumentos.", ItemCategory.Material, Rarity.Comun, Origin.Documentado,
                "D: el basalto de los Tuxtlas se usó para monumentos y columnas en La Venta.",
                "Para la protección de basalto (más defensa); trueque por obsidiana.", 60, 3, Shape.Piedra, Basalt, BasaltLight);
            Item(c, "fibras", "Fibras vegetales", "Fibras de palma y corteza, para cordeles.", ItemCategory.Material, Rarity.Comun, Origin.Interpretado,
                "I: cordelería de fibras vegetales; el detalle concreto no está documentado aquí.",
                "Para amuletos y protecciones (cordeles).", 60, 2, Shape.Haz, Earth, EarthLight);
            Item(c, "semillas", "Semillas medicinales", "Semillas y hojas que la gente del río usa para curar.", ItemCategory.Material, Rarity.Comun, Origin.Interpretado,
                "I: plantas medicinales sin identificar todavía (no se nombran especies hasta investigarlas).",
                "Para la semilla medicinal preparada (curación).", 60, 3, Shape.Semilla, Green, GreenLight);
            Item(c, "arcilla", "Arcilla", "Barro fino de la orilla.", ItemCategory.Material, Rarity.Comun, Origin.Documentado,
                "D: los olmecas fabricaban cerámica.", "Para preparar las semillas medicinales (cuenco).", 60, 2, Shape.Terron, Clay, ClayLight);
            Item(c, "esencia_olvido", "Esencia del olvido", "Lo que queda de un ser corrompido: un brillo violeta que se apaga.", ItemCategory.Material, Rarity.PocoComun, Origin.Ficticio,
                "F: la Sombra del Olvido es ficción del juego.", "Para el talismán de obsidiana; el artesano la purifica en esencia de energía.", 30, 8, Shape.Esencia, Violet, VioletLight);
            Item(c, "basalto_guardian", "Basalto de la Piedra Viva", "Un trozo del Guardián liberado, todavía tibio.", ItemCategory.Material, Rarity.Epico, Origin.Ficticio,
                "F: material único del jefe olmeca.", "Para despertar el Corazón de la Piedra Viva (artesano). No se vende.", 3, 0, Shape.Piedra, Basalt, GoldLight, sellable: false);
            Item(c, Inventory.Currency, "Ecos", "Destellos de memoria que el comerciante acepta.", ItemCategory.Moneda, Rarity.Comun, Origin.Ficticio,
                "F: los «ecos» son una unidad inventada del Mundo de las Memorias. NO representan una moneda histórica de ninguna cultura.",
                "Para comprar al comerciante.", 9999, 0, Shape.Orbe, Gold, GoldLight);

            // Consumibles (limitados por el tope de la bolsa)
            Item(c, "semilla_preparada", "Semilla medicinal preparada", "Semillas molidas en un cuenco de barro.", ItemCategory.Consumible, Rarity.PocoComun, Origin.Interpretado,
                "I: preparación medicinal; F: sus efectos en el juego.", "Recupera 20 de vida (40 % al nivel 1). Lleva hasta 5.", 5, 8, Shape.Bolsa, Green, GreenLight,
                effect: new ConsumableEffect { heal = 20 });
            Item(c, "esencia_energia", "Esencia de energía", "Esencia del olvido purificada: luz verde que se bebe.", ItemCategory.Consumible, Rarity.PocoComun, Origin.Ficticio,
                "F.", "Recupera 40 de energía. Lleva hasta 5.", 5, 10, Shape.Frasco, Jade, JadeLight, effect: new ConsumableEffect { energy = 40 });
            Item(c, "infusion_basalto", "Infusión de basalto", "Agua que reposó sobre basalto: pesa en el cuerpo.", ItemCategory.Consumible, Rarity.Raro, Origin.Ficticio,
                "F.", "Defensa +5 durante 30 s. Lleva hasta 3.", 3, 15, Shape.Frasco, Basalt, BasaltLight,
                effect: new ConsumableEffect { defenseBonus = 5, duration = 30f });

            // Reliquias (accesorios del animal: amuletos, collares, marcas; nunca armaduras humanas)
            Item(c, "amuleto_jade", "Amuleto de jade", "Una cuenta de jade atada al cuello con fibra.", ItemCategory.Reliquia, Rarity.PocoComun, Origin.Interpretado,
                "I: cuentas de jade como adorno; F: su efecto.", "Defensa +2, vida +10. Se fabrica con el artesano.", 1, 25, Shape.Amuleto, Jade, JadeLight,
                bonus: new CharacterStats(10, 0, 2, 0));
            Item(c, "talisman_obsidiana", "Talismán de obsidiana", "Una lasca de obsidiana envuelta en fibra.", ItemCategory.Reliquia, Rarity.PocoComun, Origin.Ficticio,
                "F.", "Ataque +3. Se fabrica con el artesano.", 1, 25, Shape.Talisman, Obsidian, ObsidianLight, bonus: new CharacterStats(0, 3, 0, 0));
            Item(c, "proteccion_basalto", "Protección de basalto", "Un collar de cuentas de basalto: pesa y protege.", ItemCategory.Reliquia, Rarity.Raro, Origin.Ficticio,
                "F.", "Defensa +4. Ofrenda del Jaguar de Piedra o artesano.", 1, 35, Shape.Amuleto, Basalt, BasaltLight, bonus: new CharacterStats(0, 0, 4, 0));
            Item(c, "reliquia_espiritual", "Reliquia espiritual", "Jade y un fragmento de memoria, unidos.", ItemCategory.Reliquia, Rarity.Raro, Origin.Ficticio,
                "F.", "Energía máxima +20: más golpes de tierra antes de descansar.", 1, 35, Shape.Amuleto, Jade, VioletLight, bonus: new CharacterStats(0, 0, 0, 20));
            Item(c, "marca_jaguar", "Marca del jaguar", "Unas manchas que brillan en la piel cuando acechas.", ItemCategory.Reliquia, Rarity.Raro, Origin.Ficticio,
                "F. Solo funciona con el Jaguar.", "Solo Jaguar: ataque +2, vida +10.", 1, 0, Shape.Talisman, Gold, GoldLight,
                bonus: new CharacterStats(10, 2, 0, 0), guardianOnly: "jaguar", sellable: false);
            Item(c, "corazon_piedra_viva", "Corazón de la Piedra Viva", "El disco de jade del Guardián, que te dejó al liberarse.", ItemCategory.Reliquia, Rarity.Legendario, Origin.Ficticio,
                "F: reliquia única de la mazmorra olmeca.", "Vida +20, defensa +3, energía +20. Única: no se vende.", 1, 0, Shape.Corazon, Jade, GoldLight,
                bonus: new CharacterStats(20, 0, 3, 20), sellable: false);
            Item(c, "corazon_despierto", "Corazón de la Piedra Viva, despierto", "El artesano lo pulió con basalto del Guardián: late.", ItemCategory.Reliquia, Rarity.Legendario, Origin.Ficticio,
                "F: mejora del Corazón.", "Vida +30, defensa +4, energía +30. Única.", 1, 0, Shape.Corazon, Jade, JadeLight,
                bonus: new CharacterStats(30, 0, 4, 30), sellable: false);

            // Coleccionables culturales (desbloquean entradas del Museo)
            Item(c, "figurilla_jade", "Figurilla de jade", "Una figura pequeña de rostro sereno y cabeza alargada.", ItemCategory.Coleccionable, Rarity.Raro, Origin.Documentado,
                "D: en La Venta se hallaron figurillas de jade y serpentina en ofrendas (Ofrenda 4). F: esta pieza concreta.",
                "Coleccionable (Museo).", 1, 0, Shape.Figurilla, Jade, JadeLight, sellable: false, record: "figurilla_jade");
            Item(c, "hacha_votiva", "Hacha votiva", "Un hacha de piedra verde que nunca cortó nada.", ItemCategory.Coleccionable, Rarity.PocoComun, Origin.Documentado,
                "D: hachas pulidas de jade y serpentina se depositaban como ofrenda en La Venta.", "Coleccionable (Museo).", 1, 0, Shape.Hacha, Jade, JadeLight,
                sellable: false, record: "hacha_votiva");
            Item(c, "espejo_mineral", "Espejo de mineral de hierro", "Un disco cóncavo pulido: en él la luz se junta.", ItemCategory.Coleccionable, Rarity.Raro, Origin.Documentado,
                "D: en La Venta se pulieron espejos cóncavos de ilmenita y magnetita.", "Coleccionable (Museo).", 1, 0, Shape.Espejo, Obsidian, ObsidianLight,
                sellable: false, record: "espejo_mineral");
            Item(c, "fragmento_estela", "Fragmento de estela borrada", "Un trozo de estela, liso donde debería haber relieves.", ItemCategory.Coleccionable, Rarity.Comun, Origin.Ficticio,
                "F: prueba del borrado de la Sombra del Olvido.", "Coleccionable (Museo).", 1, 0, Shape.Lasca, Basalt, VioletLight, sellable: false, record: "fragmento_estela");

            // Objetos de la historia (no se venden ni se pierden)
            Item(c, "fragmento_menor", "Fragmento de memoria menor", "Una lasca de piedra verde que guarda un recuerdo pequeño.", ItemCategory.Narrativo, Rarity.PocoComun, Origin.Ficticio,
                "F.", "Recuerdos. Con jade, el artesano hace una reliquia espiritual.", 9, 0, Shape.Fragmento, Jade, JadeLight, sellable: false);
            Item(c, "fragmento_primera_memoria", "Fragmento de la Primera Memoria", "Lo que el Guardián protegía: el recuerdo de cómo empezó esta tierra.",
                ItemCategory.Narrativo, Rarity.Legendario, Origin.Ficticio, "F: objeto central del arco olmeca (Docs/Historia_Mundo.md).",
                "Llévalo al asentamiento (MP_OLM_06).", 1, 0, Shape.Fragmento, Gold, GoldLight, sellable: false);
        }

        static void Item(Content c, string id, string name, string description, ItemCategory category, Rarity rarity, Origin origin,
            string originNote, string usage, int maxStack, int value, Shape shape, Color32 main, Color32 light,
            CharacterStats bonus = default, ConsumableEffect effect = default, string guardianOnly = null, bool sellable = true, string record = null)
        {
            var item = LoadOrCreate($"{Folder}/OBJ_{id}.asset", () =>
            {
                var created = ScriptableObject.CreateInstance<ItemData>();
                created.Configure(id, name, description, category, rarity, origin, originNote, usage, maxStack, value,
                    sellable && category != ItemCategory.Narrativo, bonus, effect, guardianOnly, record);
                return created;
            });
            if (item.Icon == null)
            {
                item.SetIcon(ItemIconFactory.Create(id, shape, main, light));
                EditorUtility.SetDirty(item);
            }
            c.Items[id] = item;
        }

        // ---------- Bendiciones ----------

        static void CreateBlessings(Content c, out List<BlessingData> all)
        {
            c.Earth = Blessing("tierra", "Bendición de la Tierra",
                "El Guardián de la Piedra Viva te presta su peso: la tierra responde cuando la golpeas.", BlessingSlot.Principal, default, 30, 4f,
                "Golpe de tierra (tecla C o botón Este): daño en área de 2,5 m (2 × ataque).",
                "Rompe el basalto agrietado: abre pasos y ofrendas ocultas.",
                "Liberar al Guardián de la Piedra Viva (obligatoria, no se pierde).");
            all = new List<BlessingData>
            {
                c.Earth,
                Blessing("fuerza_tierra", "Fuerza de la Tierra", "La piedra te sostiene cuando te golpean.", BlessingSlot.Secundaria,
                    new CharacterStats(10, 0, 4, 0), 0, 0f, "Defensa +4 y vida +10.", string.Empty, "Elegir al completar la mazmorra olmeca."),
                Blessing("instinto_jaguar", "Instinto del Jaguar", "El acecho del jaguar en cada zarpazo.", BlessingSlot.Secundaria,
                    new CharacterStats(0, 4, 0, 0), 0, 0f, "Ataque +4: daño cercano.", string.Empty, "Elegir al completar la mazmorra olmeca."),
                Blessing("memoria_agua", "Memoria del Agua", "El río recuerda: te recuperas mejor entre combates.", BlessingSlot.Secundaria,
                    new CharacterStats(10, 0, 0, 30), 0, 0f, "Vida +10 y energía máxima +30 (más habilidades antes de descansar).",
                    string.Empty, "Elegir al completar la mazmorra olmeca."),
            };
        }

        static BlessingData Blessing(string id, string name, string description, BlessingSlot slot, CharacterStats bonus, int cost,
            float cooldown, string combat, string exploration, string unlock) =>
            LoadOrCreate($"{Folder}/BEN_{id}.asset", () =>
            {
                var blessing = ScriptableObject.CreateInstance<BlessingData>();
                blessing.Configure(id, name, description, slot, bonus, cost, cooldown, combat, exploration, unlock, Culture);
                return blessing;
            });

        // ---------- Botín ----------

        static LootTable.Entry E(Content c, string id, float chance, int min, int max = 0) => new(c.Item(id), chance, min, max > 0 ? max : min);

        static void CreateLoot(Content c)
        {
            c.CommonSpirit = Table("BOT_espiritu_corrompido", 15, null, new[]
            {
                E(c, "basalto", 0.5f, 1, 2), E(c, "fibras", 0.35f, 1, 2), E(c, "semillas", 0.25f, 1),
                E(c, "esencia_olvido", 0.12f, 1), E(c, Inventory.Currency, 0.6f, 2, 5),
            });
            c.ErasedEcho = Table("BOT_eco_borrado", 35, null, new[]
            {
                E(c, "esencia_olvido", 0.6f, 1, 2), E(c, "obsidiana", 0.35f, 1), E(c, "jade", 0.1f, 1),
                E(c, "esencia_energia", 0.15f, 1), E(c, Inventory.Currency, 0.8f, 4, 8),
            });
            c.StoneJaguar = Table("BOT_jaguar_piedra", 150, new[]
            {
                E(c, "basalto", 1f, 3), E(c, "jade", 1f, 2), E(c, "esencia_energia", 1f, 1), E(c, Inventory.Currency, 1f, 30),
            }, null);
            c.CommonOffering = Table("BOT_ofrenda_comun", 10, null, new[]
            {
                E(c, "semillas", 0.6f, 1, 2), E(c, "fibras", 0.5f, 1, 2), E(c, "arcilla", 0.5f, 1, 2),
                E(c, "semilla_preparada", 0.3f, 1), E(c, Inventory.Currency, 1f, 5, 10),
            });
            c.SpecialOffering = Table("BOT_ofrenda_especial", 20, new[] { E(c, "jade", 1f, 1) }, new[]
            {
                E(c, "obsidiana", 0.5f, 1), E(c, "esencia_energia", 0.5f, 1), E(c, "semilla_preparada", 0.5f, 1),
                E(c, Inventory.Currency, 1f, 10, 20),
            });
        }

        static LootTable Table(string name, int experience, LootTable.Entry[] guaranteed, LootTable.Entry[] chances) =>
            LoadOrCreate($"{Folder}/{name}.asset", () =>
            {
                var table = ScriptableObject.CreateInstance<LootTable>();
                table.Configure(experience, guaranteed, chances);
                return table;
            });

        // ---------- Recompensas fijas ----------

        static RewardBundle.Stack S(Content c, string id, int count) => new(c.Item(id), count);

        static void CreateRewards(Content c)
        {
            // Jefe final: lo garantizado al liberarlo (Bendición de la Tierra obligatoria, objeto narrativo, experiencia).
            c.Boss = Bundle("REC_jefe_guardian", 400, new[] { S(c, "fragmento_primera_memoria", 1) },
                new[] { RecordCulture, PrimeMemoryFlag, "receta.reliquia_espiritual" }, c.Earth);
            // Ofrenda final: reliquia exclusiva y materiales especiales.
            c.Final = Bundle("REC_ofrenda_final", 0, new[]
            {
                S(c, "corazon_piedra_viva", 1), S(c, "basalto_guardian", 1), S(c, "jade", 3), S(c, Inventory.Currency, 50),
            }, new[] { "receta.corazon_despierto" });

            c.Offerings["p1_comun"] = Bundle("REC_ofrenda_p1_comun", 0, new[] { S(c, "semilla_preparada", 1) }, null, null, c.CommonOffering);
            c.Offerings["p1_especial"] = Bundle("REC_ofrenda_p1_especial", 20, new[] { S(c, "hacha_votiva", 1) }, null, null, c.SpecialOffering);
            c.Offerings["p1_oculta"] = Bundle("REC_ofrenda_p1_oculta", 40, new[] { S(c, "figurilla_jade", 1), S(c, "reliquia_espiritual", 1) });
            c.Offerings["p2_especial"] = Bundle("REC_ofrenda_p2_especial", 20, new[] { S(c, "fragmento_estela", 1), S(c, "infusion_basalto", 1) }, null, null, c.SpecialOffering);
            c.Offerings["p2_oculta"] = Bundle("REC_ofrenda_p2_oculta", 40, new[] { S(c, "marca_jaguar", 1), S(c, "esencia_energia", 1) });
            c.Offerings["p2_guardian"] = Bundle("REC_ofrenda_p2_guardian", 30, new[] { S(c, "proteccion_basalto", 1), S(c, "jade", 2) });
            c.Offerings["p3_oculta"] = Bundle("REC_ofrenda_p3_oculta", 40, new[] { S(c, "espejo_mineral", 1), S(c, "semilla_preparada", 2) });

            for (var floor = 1; floor <= OlmecaDungeonGenerator.FloorCount; floor++)
                c.Discoveries[floor] = Bundle($"REC_descubrir_piso{floor}", 15 + 15 * floor, null);

            c.Grades = new[]
            {
                Bundle("REC_nota_C", 0, new[] { S(c, Inventory.Currency, 20) }),
                Bundle("REC_nota_B", 30, new[] { S(c, Inventory.Currency, 40), S(c, "semilla_preparada", 1) }),
                Bundle("REC_nota_A", 60, new[] { S(c, Inventory.Currency, 60), S(c, "esencia_energia", 2), S(c, "jade", 1) }),
                Bundle("REC_nota_S", 100, new[] { S(c, Inventory.Currency, 100), S(c, "infusion_basalto", 1), S(c, "jade", 2) },
                    new[] { "titulo.memoria_de_piedra" }),
            };
        }

        static RewardBundle Bundle(string name, int experience, RewardBundle.Stack[] items, string[] flags = null,
            BlessingData blessing = null, LootTable extra = null) =>
            LoadOrCreate($"{Folder}/{name}.asset", () =>
            {
                var bundle = ScriptableObject.CreateInstance<RewardBundle>();
                bundle.Configure(experience, items, flags, blessing, extra);
                return bundle;
            });

        // ---------- Fabricación y comercio ----------

        static (List<RecipeData> recipes, List<TradeOfferData> offers) CreateEconomy(Content c)
        {
            var recipes = new List<RecipeData>
            {
                Recipe(c, "semilla_preparada", "semilla_preparada", 1, null, ("semillas", 2), ("arcilla", 1)),
                Recipe(c, "amuleto_jade", "amuleto_jade", 1, "receta.amuleto_jade", ("jade", 2), ("fibras", 2)),
                Recipe(c, "esencia_energia", "esencia_energia", 1, "receta.esencia_energia", ("esencia_olvido", 2), ("semillas", 1)),
                Recipe(c, "talisman_obsidiana", "talisman_obsidiana", 1, "receta.talisman_obsidiana", ("obsidiana", 2), ("esencia_olvido", 1), ("fibras", 1)),
                Recipe(c, "proteccion_basalto", "proteccion_basalto", 1, "receta.proteccion_basalto", ("basalto", 4), ("fibras", 2)),
                Recipe(c, "reliquia_espiritual", "reliquia_espiritual", 1, "receta.reliquia_espiritual", ("jade", 1), ("fragmento_menor", 1)),
                // Mejora: el Corazón se despierta con basalto del Guardián (consume el Corazón).
                Recipe(c, "corazon_despierto", "corazon_despierto", 1, "receta.corazon_despierto",
                    ("corazon_piedra_viva", 1), ("basalto_guardian", 1), ("jade", 3)),
            };
            var offers = new List<TradeOfferData>
            {
                Offer(c, "semilla_preparada", "semilla_preparada", 1, Inventory.Currency, 12),
                Offer(c, "esencia_energia", "esencia_energia", 1, Inventory.Currency, 15),
                Offer(c, "fibras", "fibras", 3, Inventory.Currency, 6),
                Offer(c, "arcilla", "arcilla", 2, Inventory.Currency, 4),
                Offer(c, "trueque_obsidiana", "obsidiana", 1, "basalto", 3),
                Offer(c, "trueque_jade", "jade", 1, "obsidiana", 2),
                Offer(c, "infusion_basalto", "infusion_basalto", 1, Inventory.Currency, 30, "oferta.infusion_basalto"),
            };
            return (recipes, offers);
        }

        static RecipeData Recipe(Content c, string id, string result, int count, string unlock, params (string id, int count)[] ingredients) =>
            LoadOrCreate($"{Folder}/RCT_{id}.asset", () =>
            {
                var recipe = ScriptableObject.CreateInstance<RecipeData>();
                recipe.Configure(id, c.Item(result), count, ingredients.Select(i => S(c, i.id, i.count)).ToArray(), unlock);
                return recipe;
            });

        static TradeOfferData Offer(Content c, string id, string item, int count, string price, int priceCount, string unlock = null) =>
            LoadOrCreate($"{Folder}/OFE_{id}.asset", () =>
            {
                var offer = ScriptableObject.CreateInstance<TradeOfferData>();
                offer.Configure(id, c.Item(item), count, c.Item(price), priceCount, unlock);
                return offer;
            });

        // ---------- Museo y misiones ----------

        static CultureRecord CreateMuseum() =>
            LoadOrCreate($"{Folder}/CUL_{Culture}.asset", () =>
            {
                var record = ScriptableObject.CreateInstance<CultureRecord>();
                record.Configure(Culture, "La memoria olmeca", RecordCulture,
                    "D: La Venta (Tabasco) fue un gran centro olmeca entre aproximadamente 900 y 400 a. C.: montículos de arcilla, " +
                    "columnas de basalto, altares-trono, cabezas colosales y ofrendas enterradas de jade y serpentina.\n" +
                    "I: la vida en la aldea, la selva y el río tal como aparecen en el juego.\n" +
                    "F: la Sombra del Olvido, los Guardianes y el Fragmento de la Primera Memoria.",
                    new[]
                    {
                        new CultureRecord.Guardian("Jaguar de Piedra", "olm.jaguar_piedra_vencido",
                            "Lo tallaron para vigilar, no para odiar."),
                        new CultureRecord.Guardian("Guardián de la Piedra Viva", OlmecaNarrativeAssets.GuardianFreedFlag,
                            "Guardaba la primera memoria de esta tierra; la Sombra le hizo ver en ti a quien venía a robarla."),
                    });
                return record;
            });

        static void CreateQuestRewards(Content c, OlmecaNarrativeAssets.Content narrative)
        {
            SetQuestReward(narrative.SettlementQuest, Bundle("REC_mision_MP_OLM_02", 40, new[] { S(c, "semillas", 3), S(c, "arcilla", 2) }));
            SetQuestReward(narrative.JungleQuest, Bundle("REC_mision_MP_OLM_03", 60, new[] { S(c, Inventory.Currency, 15), S(c, "fibras", 2) },
                new[] { "receta.amuleto_jade" }));
            SetQuestReward(narrative.DungeonQuest, Bundle("REC_mision_MP_OLM_04", 100, new[] { S(c, "esencia_energia", 1) },
                new[] { "receta.esencia_energia" }));
            SetQuestReward(narrative.ReturnQuest, Bundle("REC_mision_MP_OLM_06", 120, new[] { S(c, Inventory.Currency, 40) },
                new[] { "receta.talisman_obsidiana", "receta.proteccion_basalto", "oferta.infusion_basalto",
                        RewardService.RecordPrefix + "restauracion_altar" }));
        }

        static void SetQuestReward(Data.Narrative.QuestData quest, RewardBundle bundle)
        {
            if (quest == null || quest.RewardBundle != null)
                return;
            quest.RewardBundle = bundle;
            EditorUtility.SetDirty(quest);
        }

        /// <summary>Botín de un enemigo: su tabla y el nivel recomendado de la zona.</summary>
        public static void AddLoot(GameObject enemy, LootTable table, int zoneLevel)
        {
            var reward = enemy.AddComponent<EnemyReward>();
            reward.Table = table;
            reward.ZoneLevel = zoneLevel;
        }

        static T LoadOrCreate<T>(string path, Func<T> create) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = create();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
