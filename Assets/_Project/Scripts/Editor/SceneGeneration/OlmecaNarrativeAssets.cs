using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Narrative;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Guardián, personajes, diálogos y misiones del despertar y del asentamiento olmeca (Docs/Personajes y
    /// Docs/Historia/Misiones.md). Los assets se crean **solo si no existen**: después, el asset es la fuente y se
    /// edita en el Inspector; regenerar la ciudad no lo pisa. Para volver a este texto, borrar el asset.
    /// </summary>
    public static class OlmecaNarrativeAssets
    {
        public const string Folder = "Assets/_Project/Data/Narrative/Olmeca";
        public const string CatalogPath = "Assets/_Project/Resources/" + QuestCatalog.ResourcePath + ".asset";

        public const string ArtisanId = "artesano_jade";
        public const string ElderId = "anciana";
        public const string WarriorId = "guerrero_joven";
        public const string MerchantId = "comerciante";
        public const string SpiritId = "figura_espiritual";
        public const string AwakeningQuestId = "MP_OLM_01";
        public const string SettlementQuestId = "MP_OLM_02";
        public const string JungleQuestId = "MP_OLM_03";
        public const string ReflectionId = "reflejo"; // aviso "examinar:reflejo" en la orilla
        // Selva (MP_OLM_03): avisos "llegar:selva", "vencer:espiritu_corrompido", "examinar:fragmento_menor" y
        // "llegar:entrada_estructura".
        public const string JungleZoneId = "selva";
        public const string EntranceZoneId = "entrada_estructura";
        public const string CorruptedSpiritId = "espiritu_corrompido";
        public const string FragmentId = "fragmento_menor";
        public const int JungleSpiritCount = 3;
        public const string GuardianPath = "Assets/_Project/Data/Characters/GUA_jaguar.asset";
        const string PlayerSpriteSetPath = "Assets/_Project/Data/Characters/DSS_Jugador.asset";

        // Banderas propias de este arco. Las de misión las pone QuestService (StoryEvents).
        public const string OathFlag = "olm.juramento";
        public const string AwakeningDoneFlag = "olm.despertar_completo";
        public const string ElderToldFlag = "olm.anciana_conto";
        public const string EntranceFoundFlag = "olm.entrada_descubierta";
        public const string WarriorInJungleFlag = "olm.guerrero_en_selva";
        public const string StructureFoundFlag = "olm.estructura_encontrada";
        static readonly string QuestActive = StoryEvents.QuestActiveFlag(SettlementQuestId);
        static readonly string QuestDone = StoryEvents.QuestCompletedFlag(SettlementQuestId);
        static readonly string JungleQuestDone = StoryEvents.QuestCompletedFlag(JungleQuestId);

        /// <summary>Este espíritu corrompido ya fue vencido (no vuelve y deja su fragmento). i empieza en 1.</summary>
        public static string SpiritDefeatedFlag(int i) => $"olm.selva.espiritu_{i}_vencido";

        /// <summary>Este fragmento de memoria ya se recogió. i empieza en 1.</summary>
        public static string FragmentFlag(int i) => $"olm.selva.fragmento_{i}";

        // Lo que recuerda cada fragmento al tocarlo. D/I: el basalto se llevó a La Venta desde los Tuxtlas y la piedra
        // verde se pulía con arena y agua; el resto es ficción del juego.
        public static readonly string[] FragmentTexts =
        {
            "Un fragmento de memoria, tibio como una piedra al sol. Al tocarlo oyes voces que cantan mientras arrastran " +
            "una piedra enorme río abajo, desde montañas que no conoces.",
            "Otro fragmento. Ves unas manos que pulen una piedra verde con arena y agua, día tras día, sin prisa.",
            "El último fragmento. Por un instante la selva entera recuerda un nombre... y lo olvida otra vez.",
        };

        // Al asomarse al agua (Historia_Mundo §2.1–2.2): recuerdos rotos de la vida anterior y el reflejo animal.
        public const string ReflectionText =
            "Recuerdas, a pedazos: una voz, una luz, piedras que se derrumban y la sensación de caer.\n" +
            "Te asomas al agua. Quieres hablar y solo sale un gruñido. En el reflejo no hay una persona: te mira un jaguar.";

        public readonly struct Character
        {
            public readonly NpcData Npc;
            public readonly DialogueData Dialogue;

            public Character(NpcData npc, DialogueData dialogue)
            {
                Npc = npc;
                Dialogue = dialogue;
            }
        }

        public sealed class Content
        {
            public Character Spirit, Artisan, Elder, Warrior, Merchant;
            /// <summary>El joven guerrero en la entrada de la estructura (mismo NpcData, otro diálogo).</summary>
            public Character WarriorAtEntrance;
            public QuestData AwakeningQuest, SettlementQuest, JungleQuest;
            public GuardianData Guardian;
        }

        public static Content EnsureAssets()
        {
            OlmecaPrototypeGenerator.EnsureFolder(Folder);
            OlmecaPrototypeGenerator.EnsureFolder(System.IO.Path.GetDirectoryName(CatalogPath)!.Replace('\\', '/'));

            // El Jaguar es el Guardián del vertical slice (Docs/Personajes/Protagonista.md). Afinidad inicial propuesta:
            // oscuridad (el GDD propone oscuridad/tierra, y tierra es la que concede el jefe olmeca).
            var guardian = LoadOrCreate(GuardianPath, () => GuardianData.Create("jaguar", "Jaguar",
                "Fuerza, movilidad y combate cercano.", Affinity.Oscuridad, SpeciesStatus.Nativa,
                AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(PlayerSpriteSetPath)));

            // Las misiones primero: los diálogos las referencian.
            var awakening = LoadOrCreate($"{Folder}/MIS_{AwakeningQuestId}.asset", () => QuestData.Create(
                AwakeningQuestId, "El despertar", new[]
                {
                    new QuestStep(StoryEvents.Examine(ReflectionId), "Asómate al agua"),
                    new QuestStep(StoryEvents.Talk(SpiritId), "Escucha a la figura que te espera en la orilla"),
                    new QuestStep(StoryEvents.Talk(ArtisanId), "Llega al asentamiento y busca al artesano de jade"),
                }, AwakeningDoneFlag));
            var quest = LoadOrCreate($"{Folder}/MIS_{SettlementQuestId}.asset", () => QuestData.Create(
                SettlementQuestId, "El asentamiento inquieto", new[]
                {
                    new QuestStep(StoryEvents.Talk(ElderId), "Habla con la anciana, junto a las enramadas"),
                    new QuestStep(StoryEvents.Talk(WarriorId), "Busca al joven guerrero junto al fogón, cerca de la plaza"),
                }, EntranceFoundFlag));
            // Cada fragmento solo aparece al vencer a su espíritu, así que recoger tres implica haber vencido a tres; y
            // como los pasos cuentan en orden, ningún fragmento se puede recoger "antes de tiempo" y perderse.
            var jungle = LoadOrCreate($"{Folder}/MIS_{JungleQuestId}.asset", () => QuestData.Create(
                JungleQuestId, "Rastros en la selva", new[]
                {
                    new QuestStep(StoryEvents.Reach(JungleZoneId), "Sal de la plaza por el sendero del oeste, hacia la selva"),
                    new QuestStep(StoryEvents.Examine(FragmentId),
                        "Vence a los espíritus corrompidos y recoge los fragmentos de memoria que dejan", JungleSpiritCount),
                    new QuestStep(StoryEvents.Talk(WarriorId), "Sigue el sendero hasta la entrada de piedra; el joven guerrero te espera"),
                }, StructureFoundFlag));
            EnsureInCatalog(awakening);
            EnsureInCatalog(quest);
            EnsureInCatalog(jungle);

            var content = new Content
            {
                AwakeningQuest = awakening, SettlementQuest = quest, JungleQuest = jungle, Guardian = guardian,
            };

            // Historia_Mundo §2.2–2.4. No es un dios ni un enemigo; no tiene rasgos de ninguna deidad.
            content.Spirit = CreateCharacter(SpiritId, "Figura espiritual", "guía", new[]
            {
                new DialogueBranch(Lines(
                        "Ve al asentamiento. Busca al artesano de jade: sus manos todavía recuerdan."),
                    requiredFlags: new[] { OathFlag }),
                new DialogueBranch(Lines(
                        "No intentes hablar. Aquí no hace falta la voz para que te escuche.",
                        "Has llegado al Mundo de las Memorias. Aquí permanecen los ecos de los pueblos: sus caminos, sus relatos, lo que supieron.",
                        "No soy un dios, ni vengo a hacerte daño. Solo esperaba a alguien que pudiera ver este mundo desde fuera de sus épocas.",
                        "Las memorias se están deshaciendo. Las estelas enmudecen, los caminos se borran y hay criaturas que ya no reconocen a nadie.",
                        "Tu forma es la del jaguar. Con ella cruzarás la selva.",
                        "Cerca de aquí hay un asentamiento. Sus guardianes han dejado de responder. Ve, escucha a su gente y averigua por qué."),
                    flagsOnFinish: new[] { OathFlag }),
            });

            content.Artisan = CreateCharacter(ArtisanId, "Artesano de jade", "artesano", new[]
            {
                new DialogueBranch(Lines(
                        "Así que hay rastros en la selva... Yo seguiré con mi piedra. Alguien tiene que recordar cómo se pule."),
                    requiredFlags: new[] { QuestDone }),
                new DialogueBranch(Lines(
                        "Ve con la anciana, junto a las enramadas. Y si ves al joven guerrero, dile que no se esfuerce: aún no sana."),
                    requiredFlags: new[] { QuestActive }),
                new DialogueBranch(Lines(
                        "Un animal que escucha... Entonces es cierto lo que cuentan: a veces la selva manda mensajeros.",
                        "Mira esta piedra verde. La traen de muy lejos y la pulo durante días con arena y agua.",
                        "Pero las piedras grandes de la plaza están perdiendo sus marcas. No es la lluvia: ayer estaban y hoy ya no.",
                        "La anciana que vive junto a las enramadas recuerda más que nadie. Habla con ella."),
                    requiredFlags: new[] { OathFlag },
                    questToStart: quest),
                new DialogueBranch(Lines(
                        "¡Quieto ahí! ...Un jaguar que no ataca. Qué cosa tan rara.",
                        "Si de verdad entiendes lo que digo, vuelve cuando sepas a qué vienes.")),
            });

            content.Elder = CreateCharacter(ElderId, "La anciana", "anciana", new[]
            {
                new DialogueBranch(Lines(
                        "Si encuentras el final de mi relato, tráemelo. Todavía recuerdo cómo empieza."),
                    requiredFlags: new[] { QuestDone }),
                new DialogueBranch(Lines(
                        "Busca al joven guerrero junto al fogón, cerca de la plaza. Yo intentaré recordar."),
                    requiredFlags: new[] { ElderToldFlag }),
                new DialogueBranch(Lines(
                        "Así que el artesano te mandó. Acércate.",
                        "Mi abuela me contó un relato sobre el principio de las cosas. Yo se lo contaba a los niños cada temporada de lluvias.",
                        "Ahora, cuando llego a la mitad, las palabras se me escapan. Y no soy la única: a todos nos falta un pedazo de algo.",
                        "El joven guerrero volvió herido de la selva. Vio algo allá. Lo encontrarás junto al fogón, cerca de la plaza."),
                    requiredFlags: new[] { QuestActive },
                    flagsOnFinish: new[] { ElderToldFlag }),
                new DialogueBranch(Lines(
                        "Las lluvias llegarán pronto. Hay que subir las vasijas antes de que el río crezca.",
                        "¿Un jaguar tan tranquilo? Qué tiempos tan raros.")),
            });

            // Al terminar MP_OLM_02 encadena MP_OLM_03 y se adelanta a la entrada (en la selva, DLG_guerrero_joven_entrada).
            content.Warrior = CreateCharacter(WarriorId, "Joven guerrero", "guerrero", new[]
            {
                new DialogueBranch(Lines(
                        "Sal de la plaza por el sendero del oeste, el que se mete entre las palmas. Te alcanzo en la entrada."),
                    requiredFlags: new[] { WarriorInJungleFlag }),
                new DialogueBranch(Lines(
                        "No me mires así. Es solo un rasguño... bueno, uno grande.",
                        "Fuimos tres a revisar los caminos de la selva. Los animales no huían de nosotros: nos esperaban.",
                        "Tenían algo raro. Grietas que brillaban en la piel, como si les faltaran pedazos.",
                        "Más allá de las ceibas, entre las raíces, encontré una entrada de piedra que no recordaba. Y conozco bien esa parte de la selva.",
                        "¿Vas a ir? Entonces sal de la plaza por el sendero del oeste. Cuando caían, esas criaturas dejaban una piedrecita con luz. Yo no me atreví a tocarlas.",
                        "Te alcanzo en la entrada. Conozco un atajo, aunque sea cojeando."),
                    requiredFlags: new[] { ElderToldFlag },
                    flagsOnFinish: new[] { WarriorInJungleFlag },
                    questToStart: jungle),
                new DialogueBranch(Lines(
                        "Me dijeron que descansara. Descansar es más difícil que pelear.")),
            });

            // En la entrada: reconoce el lugar cuando el jugador trae los tres fragmentos (el paso 3 de MP_OLM_03).
            var allFragments = Enumerable.Range(1, JungleSpiritCount).Select(FragmentFlag).ToArray();
            var warriorAtEntrance = LoadOrCreate($"{Folder}/DLG_{WarriorId}_entrada.asset", () => DialogueData.Create(
                $"DLG_{WarriorId}_entrada",
                new DialogueBranch(Lines(
                        "Yo vigilo aquí afuera. Lo que borra las piedras está ahí abajo, y tú eres quien puede entrar."),
                    requiredFlags: new[] { JungleQuestDone }),
                new DialogueBranch(Lines(
                        "Llegaste. Y traes esas piedrecitas con luz... ¿las sientes? Son pedazos de algo que alguien recordó.",
                        "Mira las columnas: son de basalto, como las del recinto de la plaza. Mi abuela decía que bajo los montículos así descansaba alguien importante.",
                        "Pero yo crecí en esta selva y nunca vi esta entrada. No estaba aquí... o no la recordábamos.",
                        "Algo allá abajo se está comiendo los recuerdos. Si alguien puede detenerlo, es quien vino de fuera."),
                    requiredFlags: allFragments),
                new DialogueBranch(Lines(
                        "Aquí es, entre las raíces. Llegué antes que tú: conozco los atajos.",
                        "No entres todavía. Las criaturas corrompidas siguen en el sendero, y lo que dejan al caer hay que recogerlo antes de que se apague."))));
            content.WarriorAtEntrance = new Character(content.Warrior.Npc, warriorAtEntrance);

            content.Merchant = CreateCharacter(MerchantId, "Comerciante", "comerciante", new[]
            {
                new DialogueBranch(Lines(
                        "¿Una entrada en la selva? Con razón los caminos están tan raros. Yo me quedo junto al río."),
                    requiredFlags: new[] { QuestDone }),
                new DialogueBranch(Lines(
                        "Traigo obsidiana, conchas y piedra verde de muy lejos... o traía. Los caminos ya no son seguros.",
                        "Antes sabía cada vuelta del camino. Ahora llego a un cruce y no recuerdo hacia dónde iba.",
                        "Cuando vuelvan a ser seguros, tendré buenas cosas que cambiar.")),
            });

            AssetDatabase.SaveAssets();
            return content;
        }

        static Character CreateCharacter(string id, string displayName, string role,
            DialogueBranch[] branches)
        {
            var npc = LoadOrCreate($"{Folder}/NPC_{id}.asset", () => NpcData.Create(id, displayName, role));
            var dialogue = LoadOrCreate($"{Folder}/DLG_{id}.asset", () => DialogueData.Create($"DLG_{id}", branches));
            return new Character(npc, dialogue);
        }

        static DialogueLine[] Lines(params string[] texts) => texts.Select(text => new DialogueLine(text)).ToArray();

        static T LoadOrCreate<T>(string path, System.Func<T> create) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // El catálogo se conserva si ya existe: solo se añade la misión que falte.
        static void EnsureInCatalog(QuestData quest)
        {
            var catalog = LoadOrCreate(CatalogPath, () => QuestCatalog.Create());
            if (catalog.Quests.Contains(quest))
                return;

            var serialized = new SerializedObject(catalog);
            var list = serialized.FindProperty("quests");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = quest;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
