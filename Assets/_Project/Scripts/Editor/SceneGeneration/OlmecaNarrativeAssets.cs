using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Data.Narrative;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Personajes, diálogos y misiones del asentamiento olmeca (Docs/Personajes/NPC_Olmecas.md y
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
        public const string SettlementQuestId = "MP_OLM_02";

        // Banderas propias de este arco. Las de misión las pone QuestService (StoryEvents).
        public const string ElderToldFlag = "olm.anciana_conto";
        public const string EntranceFoundFlag = "olm.entrada_descubierta";
        static readonly string QuestActive = StoryEvents.QuestActiveFlag(SettlementQuestId);
        static readonly string QuestDone = StoryEvents.QuestCompletedFlag(SettlementQuestId);

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
            public Character Artisan, Elder, Warrior, Merchant;
            public QuestData SettlementQuest;
        }

        public static Content EnsureAssets()
        {
            OlmecaPrototypeGenerator.EnsureFolder(Folder);
            OlmecaPrototypeGenerator.EnsureFolder(System.IO.Path.GetDirectoryName(CatalogPath)!.Replace('\\', '/'));

            // La misión primero: el diálogo del artesano la referencia.
            var quest = LoadOrCreate($"{Folder}/MIS_{SettlementQuestId}.asset", () => QuestData.Create(
                SettlementQuestId, "El asentamiento inquieto", new[]
                {
                    new QuestStep(StoryEvents.Talk(ElderId), "Habla con la anciana, junto a las enramadas"),
                    new QuestStep(StoryEvents.Talk(WarriorId), "Busca al joven guerrero junto al fogón, cerca de la plaza"),
                }, EntranceFoundFlag));
            EnsureInCatalog(quest);

            var content = new Content { SettlementQuest = quest };
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
                    questToStart: quest),
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

            content.Warrior = CreateCharacter(WarriorId, "Joven guerrero", "guerrero", new[]
            {
                new DialogueBranch(Lines(
                        "Esas criaturas cuidaban la entrada. Cuando sane, iré contigo. Por ahora, ten cuidado."),
                    requiredFlags: new[] { QuestDone }),
                new DialogueBranch(Lines(
                        "No me mires así. Es solo un rasguño... bueno, uno grande.",
                        "Fuimos tres a revisar los caminos de la selva. Los animales no huían de nosotros: nos esperaban.",
                        "Tenían algo raro. Grietas que brillaban en la piel, como si les faltaran pedazos.",
                        "Más allá de las ceibas, entre las raíces, encontré una entrada de piedra que no recordaba. Y conozco bien esa parte de la selva."),
                    requiredFlags: new[] { ElderToldFlag }),
                new DialogueBranch(Lines(
                        "Me dijeron que descansara. Descansar es más difícil que pelear.")),
            });

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
