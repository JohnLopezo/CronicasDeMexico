using System.Collections.Generic;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.Gameplay.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Genera la escena jugable de la ciudad olmeca a partir de Ciudad.json, que exporta el flujo de arte
    /// (Tools/ArtPipeline/generar_olmeca.py, planificar_ciudad). Las piezas comunes (suelo, agua, objetos, colisiones,
    /// cámara) las pone PixelArtSceneBuilder; aquí van las inscripciones, los habitantes, el despertar y el sendero
    /// hacia la selva.
    ///
    /// La escena se deriva por completo de los datos: regenerarla la reemplaza (no editarla a mano).
    /// Desde consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.OlmecaCityGenerator.Generate
    /// </summary>
    public static class OlmecaCityGenerator
    {
        public const string SceneName = "Olmeca_Ciudad";
        public const string ScenePath = "Assets/_Project/Scenes/Regions/Olmeca/" + SceneName + ".unity";
        public const string CityDataPath = "Assets/_Project/Data/World/Olmeca/Ciudad.json";
        /// <summary>Punto de llegada al volver de la selva (SpawnPoint).</summary>
        public const string FromJungleSpawnId = "desde_selva";

        // El despertar (MP_OLM_01): el jugador aparece en la orilla noreste del río, cerca de la figura espiritual, y
        // se asoma al agua en ReflectionPoint. Coordenadas del flujo, en tierra y lejos de obstáculos.
        static readonly Vector2 PlayerStart = new(17.4f, 22.8f);
        static readonly Vector2 ReflectionPoint = new(19.3f, 22.6f);

        // Textos al examinar. D = documentado, I = interpretación (Docs/Arte_Olmeca_Demo.md §8).
        const string AltarText =
            "Un altar-trono de basalto. En La Venta, de nichos como este sale la figura de un gobernante sentado " +
            "(Altar 4). Este nicho está vacío.";
        // Desde que la anciana recordó el final de su relato (MP_OLM_06): ahí se deja el Fragmento de la Primera Memoria (F).
        const string AltarWithMemoryText =
            "En el nicho del altar-trono brilla el Fragmento de la Primera Memoria. Al tocarlo, por un instante, toda la " +
            "plaza recuerda lo que fue.";
        internal const string StelaText = "Una estela de basalto. Si tuvo relieves, la lluvia y el tiempo los borraron.";
        internal const string BrokenStelaText = "Una estela partida. Su parte superior yace en el suelo, cubierta de musgo.";
        // Tras la misión del asentamiento: pista de que el borrado no es natural (se revela al cerrar el arco I).
        const string StelaAfterSettlementText =
            "Una estela de basalto. Desde que el artesano lo dijo, no puedes dejar de notarlo: la piedra no está " +
            "gastada como las rocas del camino. Está lisa, como si las marcas nunca hubieran existido.";

        // Habitantes del asentamiento (Docs/Personajes/NPC_Olmecas.md): sprite provisional y puntos de patrulla en
        // x, y del flujo (uno = quieto en su sitio). Posiciones en tierra y lejos de obstáculos.
        static readonly (string id, string sprites, Vector2[] points)[] Settlers =
        {
            (OlmecaNarrativeAssets.SpiritId, "Figura", new[] { new Vector2(18.6f, 23.6f) }),
            (OlmecaNarrativeAssets.ArtisanId, "Artesano", new[] { new Vector2(13.5f, 2.8f), new Vector2(14.6f, 2.8f) }),
            (OlmecaNarrativeAssets.ElderId, "Anciana", new[] { new Vector2(12.4f, 14.2f) }),
            (OlmecaNarrativeAssets.WarriorId, "Guerrero", new[] { new Vector2(12.0f, 8.5f) }),
            (OlmecaNarrativeAssets.MerchantId, "Comerciante", new[] { new Vector2(19.0f, -3.2f) }),
        };

        [MenuItem("Crónicas/Generar ciudad olmeca")]
        static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Generar ciudad olmeca",
                    $"{ScenePath} ya existe y se reemplazará entera (se genera desde Ciudad.json).", "Reemplazar", "Cancelar"))
                return;

            Generate();
        }

        public static void Generate()
        {
            var data = LoadData(CityDataPath);
            var sprites = LoadSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var messageChannel = MessageChannel();
            // Después de NewScene, que descarga los ScriptableObjects recién creados que nada referencia.
            var narrative = OlmecaNarrativeAssets.EnsureAssets();
            OlmecaRewardAssets.EnsureAssets(narrative);

            var root = BuildScenery("--- Ciudad ---", data, sprites,
                (placement, renderer) => AddMonumentInscription(placement, renderer, messageChannel));

            ConfigureLighting();
            var player = PlacePlayer(scene, PlayerStart);
            LinkRoofs(root, player.transform);
            PlaceSettlers(Group("--- Habitantes ---", null), narrative, messageChannel);
            BuildAwakening(Group("--- Despertar ---", null), narrative, messageChannel);
            BuildPathToJungle(Group("--- Sendero a la selva ---", null), data.puntos);
            BuildServices(Group("--- Servicios ---", null));
            CreateCamera(player.transform, data.limites);
            CreateHud(messageChannel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Generador] Ciudad olmeca generada en {ScenePath}: {data.objetos.Length} objetos.");
        }

        // Altares y estelas se examinan; las estelas lisas cambian su texto tras la misión del asentamiento.
        static void AddMonumentInscription(Placement placement, SpriteRenderer renderer, StringEventChannel messageChannel)
        {
            var isAltar = placement.sprite.StartsWith("Altar_Trono");
            var isBrokenStela = placement.sprite == "Estela_Grande_03";
            var isStela = !isBrokenStela && placement.sprite.StartsWith("Estela");
            if (!isAltar && !isBrokenStela && !isStela)
                return;

            AddInscription(renderer.transform.parent, $"Inscripcion_{placement.sprite}",
                ToWorld(placement.x, placement.y, placement.z) + Vector3.up * 0.8f,
                isAltar ? AltarText : isBrokenStela ? BrokenStelaText : StelaText, messageChannel,
                examineId: isAltar ? "altar_trono" : "estela",
                alternateFlag: isStela ? StoryEvents.QuestCompletedFlag(OlmecaNarrativeAssets.SettlementQuestId)
                    : isAltar ? OlmecaNarrativeAssets.ElderRememberedFlag : null,
                alternateText: isStela ? StelaAfterSettlementText : isAltar ? AltarWithMemoryText : null);
        }

        // El reflejo es una inscripción sin sprite en la orilla; la misión empieza sola al cargar si no se empezó.
        static void BuildAwakening(Transform parent, OlmecaNarrativeAssets.Content narrative, StringEventChannel messageChannel)
        {
            AddInscription(parent, "Reflejo", ToWorld(ReflectionPoint) + Vector3.up * 0.5f,
                OlmecaNarrativeAssets.ReflectionText, messageChannel, examineId: OlmecaNarrativeAssets.ReflectionId,
                prompt: "Mirar el agua");
            parent.gameObject.AddComponent<StartQuestOnLoad>().Quest = narrative.AwakeningQuest;
        }

        static void PlaceSettlers(Transform parent, OlmecaNarrativeAssets.Content narrative, StringEventChannel messageChannel)
        {
            var characters = new Dictionary<string, OlmecaNarrativeAssets.Character>
            {
                [OlmecaNarrativeAssets.SpiritId] = narrative.Spirit,
                [OlmecaNarrativeAssets.ArtisanId] = narrative.Artisan,
                [OlmecaNarrativeAssets.ElderId] = narrative.Elder,
                [OlmecaNarrativeAssets.WarriorId] = narrative.Warrior,
                [OlmecaNarrativeAssets.MerchantId] = narrative.Merchant,
            };
            var palettes = new Dictionary<string, PlaceholderSpriteFactory.CharacterPalette>
            {
                ["Figura"] = PlaceholderSpriteFactory.Spirit,
                ["Artesano"] = PlaceholderSpriteFactory.Artisan,
                ["Anciana"] = PlaceholderSpriteFactory.Elder,
                ["Guerrero"] = PlaceholderSpriteFactory.Warrior,
                ["Comerciante"] = PlaceholderSpriteFactory.Merchant,
            };

            foreach (var (id, spritesName, points) in Settlers)
            {
                var spriteSet = OlmecaPrototypeGenerator.CreateSpriteSet(spritesName, palettes[spritesName]);
                var settler = CreateSettler(parent, characters[id], spriteSet, messageChannel, points);
                // El guerrero se adelanta a la entrada de la estructura al dar MP_OLM_03 (lo espera en la selva). Solo
                // se comprueba al cargar, para que no desaparezca en mitad de la conversación.
                if (id == OlmecaNarrativeAssets.WarriorId)
                    ShowWhen(settler, null, new[] { OlmecaNarrativeAssets.WarriorInJungleFlag }, liveUpdate: false);
            }
        }

        // Taller del artesano y puesto del comerciante (M5): abren Fabricar y Comerciar en el menú. Junto al NPC, hacia la
        // cámara, aparte de su conversación.
        static readonly (string name, string verb, string tab, Vector2 point)[] Services =
        {
            ("Taller_Artesano", "Fabricar", MenuTabs.Crafting, new Vector2(14.0f, 1.7f)),
            ("Puesto_Comerciante", "Comerciar", MenuTabs.Trading, new Vector2(19.0f, -4.3f)),
        };

        static void BuildServices(Transform parent)
        {
            var channel = OlmecaPrototypeGenerator.LoadOrCreateAsset<StringEventChannel>(OlmecaPrototypeGenerator.MenuChannelPath);
            foreach (var (name, verb, tab, point) in Services)
            {
                var service = new GameObject(name) { layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.InteractableLayerName) };
                service.transform.SetParent(parent, false);
                service.transform.position = ToWorld(point) + Vector3.up * 0.5f;
                var trigger = service.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.7f;
                service.AddComponent<ServicePoint>().Configure(verb, tab, channel);
            }
        }

        // Sendero del oeste (MP_OLM_03): portal a la selva en el borde del mapa y punto de llegada al volver de ella.
        static void BuildPathToJungle(Transform parent, RegionPoints points)
        {
            AddPortal(parent, "Portal_Selva", points.salida_selva.Value, new Vector3(0.6f, 3.2f, 2f),
                OlmecaJungleGenerator.SceneName, OlmecaJungleGenerator.FromCitySpawnId);
            AddSpawnPoint(parent, FromJungleSpawnId, points.llegada_selva.Value);
        }
    }
}
