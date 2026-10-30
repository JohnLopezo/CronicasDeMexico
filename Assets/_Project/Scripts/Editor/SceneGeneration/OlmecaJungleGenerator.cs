using System.Collections.Generic;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.Gameplay.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Genera la selva al oeste de la ciudad olmeca (MP_OLM_03, «Rastros en la selva») a partir de Selva.json, que
    /// exporta el flujo de arte (Tools/ArtPipeline/generar_olmeca.py, planificar_selva). Se llega desde el sendero del
    /// oeste de la ciudad; en cada uno de los tres claros acecha un espíritu corrompido que, al caer, deja un fragmento
    /// de memoria, y al final del sendero espera el joven guerrero ante la entrada a la antigua estructura.
    ///
    /// Los espíritus no reaparecen: su bandera los retira para siempre (ActiveWhenFlags), también al volver a la escena.
    /// La escena se deriva por completo de los datos: regenerarla la reemplaza (no editarla a mano).
    /// Desde consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.OlmecaJungleGenerator.Generate
    /// </summary>
    public static class OlmecaJungleGenerator
    {
        public const string SceneName = "Olmeca_Selva";
        public const string ScenePath = "Assets/_Project/Scenes/Regions/Olmeca/" + SceneName + ".unity";
        public const string JungleDataPath = "Assets/_Project/Data/World/Olmeca/Selva.json";
        /// <summary>Punto de llegada al venir de la ciudad (SpawnPoint).</summary>
        public const string FromCitySpawnId = "desde_ciudad";
        /// <summary>Punto de llegada al subir del piso 1 de la mazmorra.</summary>
        public const string FromDungeonSpawnId = "desde_mazmorra";
        const string FragmentSprite = "Fragmento_Memoria_01";

        const string EntranceText =
            "Una abertura oscura entre columnas de basalto, bajo un montículo que la selva ha cubierto. En La Venta, " +
            "un recinto de columnas así guardaba una tumba (Tumba A). De dentro sale un aire frío que huele a piedra mojada.";

        [MenuItem("Crónicas/Generar selva olmeca")]
        static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Generar selva olmeca",
                    $"{ScenePath} ya existe y se reemplazará entera (se genera desde Selva.json).", "Reemplazar", "Cancelar"))
                return;

            Generate();
        }

        public static void Generate()
        {
            var data = LoadData(JungleDataPath);
            var points = data.puntos;
            var sprites = LoadSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var messageChannel = MessageChannel();
            // Después de NewScene, que descarga los ScriptableObjects recién creados que nada referencia.
            var narrative = OlmecaNarrativeAssets.EnsureAssets();

            var root = BuildScenery("--- Selva ---", data, sprites,
                (placement, renderer) => AddRuinInscription(placement, renderer, messageChannel));

            ConfigureLighting();
            // Al pulsar Play aquí directamente el jugador empieza donde llega desde la ciudad.
            var player = PlacePlayer(scene, points.llegada.Value);
            LinkRoofs(root, player.transform);
            BuildTravel(Group("--- Sendero a la ciudad ---", null), points);
            BuildSpirits(Group("--- Espíritus corrompidos ---", null), points, sprites, messageChannel);
            BuildEntrance(Group("--- Entrada ---", null), points, narrative, messageChannel);
            CreateCamera(player.transform, data.limites);
            CreateHud(messageChannel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Generador] Selva olmeca generada en {ScenePath}: {data.objetos.Length} objetos.");
        }

        // Las estelas de la explanada se examinan, como las de la ciudad; la entrada tiene su propio texto.
        static void AddRuinInscription(Placement placement, SpriteRenderer renderer, StringEventChannel messageChannel)
        {
            var position = ToWorld(placement.x, placement.y, placement.z) + Vector3.up * 0.8f;
            if (placement.sprite.StartsWith("Entrada_Estructura"))
                AddInscription(renderer.transform.parent, "Inscripcion_Entrada", ToWorld(placement.x + 1.9f, placement.y, 0.8f),
                    EntranceText, messageChannel, examineId: "entrada_estructura");
            else if (placement.sprite == "Estela_Grande_03")
                AddInscription(renderer.transform.parent, $"Inscripcion_{placement.sprite}", position,
                    OlmecaCityGenerator.BrokenStelaText, messageChannel, examineId: "estela");
            else if (placement.sprite.StartsWith("Estela"))
                AddInscription(renderer.transform.parent, $"Inscripcion_{placement.sprite}", position,
                    OlmecaCityGenerator.StelaText, messageChannel, examineId: "estela");
        }

        static void BuildTravel(Transform parent, RegionPoints points)
        {
            AddSpawnPoint(parent, FromCitySpawnId, points.llegada.Value);
            AddPortal(parent, "Portal_Ciudad", points.salida.Value, new Vector3(0.6f, 3.2f, 2f),
                OlmecaCityGenerator.SceneName, OlmecaCityGenerator.FromJungleSpawnId);
            // Cuenta cada vez que se entra: así vale aunque la misión empiece después de pasar por aquí.
            AddZone(parent, OlmecaNarrativeAssets.JungleZoneId, points.zona_selva.Value, new Vector3(1.5f, 4f, 2f));

            // La abertura de la entrada lleva al piso 1 cuando el guerrero ya reconoció el lugar (MP_OLM_03 completada).
            // Volver aquí termina la incursión: los santuarios de Vida se reencienden.
            AddSpawnPoint(parent, FromDungeonSpawnId, points.desde_mazmorra.Value);
            var dungeon = Group("Entrada_Mazmorra", parent);
            var portal = AddPortal(dungeon, "Portal_Mazmorra", points.mazmorra.Value, new Vector3(0.4f, 1.4f, 2f),
                OlmecaDungeonGenerator.SceneName(1), OlmecaDungeonGenerator.FromAboveSpawnId);
            portal.gameObject.SetActive(false); // hasta que ActiveWhenFlags vea la bandera
            ShowWhen(portal.gameObject, new[] { StoryEvents.QuestCompletedFlag(OlmecaNarrativeAssets.JungleQuestId) }, null,
                liveUpdate: true);
            parent.gameObject.AddComponent<IncursionReset>();
        }

        // ---------- Espíritus corrompidos y fragmentos ----------

        static void BuildSpirits(Transform parent, RegionPoints points, Dictionary<string, Sprite> sprites,
            StringEventChannel messageChannel)
        {
            var spriteSet = OlmecaPrototypeGenerator.CreateSpriteSet("EspirituCorrompido",
                PlaceholderSpriteFactory.CorruptedJaguarSpirit);
            for (var i = 1; i <= OlmecaNarrativeAssets.JungleSpiritCount; i++)
            {
                var clearing = Group($"Claro_{i}", parent);
                var defeated = OlmecaNarrativeAssets.SpiritDefeatedFlag(i);
                var collected = OlmecaNarrativeAssets.FragmentFlag(i);

                var spirit = CreateSpirit(clearing, $"EspirituCorrompido_{i}", spriteSet, points.claros[i - 1].Value, defeated);
                ShowWhen(spirit, null, new[] { defeated }, liveUpdate: true);

                var fragment = CreateFragment(clearing, i, sprites, points.fragmentos[i - 1].Value, collected, messageChannel);
                fragment.SetActive(false); // aparece al vencer a su espíritu
                ShowWhen(fragment, new[] { defeated }, new[] { collected }, liveUpdate: true);
            }
        }

        // Como el espíritu jaguar del prototipo (EnemyChaser), con arte corrompido, a la escala de esta escena y sin
        // reaparecer: al caer activa su bandera, que lo retira.
        static GameObject CreateSpirit(Transform parent, string name, DirectionalSpriteSet spriteSet, Vector2 home, string defeatedFlag) =>
            CreateCorruptedSpirit(parent, name, spriteSet, home, defeatedFlag);

        /// <summary>
        /// Espíritu corrompido a la escala de las escenas de pixel art. Con bandera, al caer la activa; sin ella (en la
        /// mazmorra) queda vencido hasta que se vuelva a cargar la escena.
        /// </summary>
        internal static GameObject CreateCorruptedSpirit(Transform parent, string name, DirectionalSpriteSet spriteSet, Vector2 home,
            string defeatedFlag)
        {
            var root = CreateCharacter(name, spriteSet, out var spriteRenderer);
            root.transform.SetParent(parent, false);
            root.transform.position = ToWorld(home);
            root.layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.EnemyLayerName); // lo que golpea PlayerAttack

            OlmecaPrototypeGenerator.AddHealth(root, maxHealth: 3, invulnerabilityTime: 0.2f, spriteRenderer);
            var chaser = root.AddComponent<EnemyChaser>();
            SetProperty(chaser, "detectionRadius", property => property.floatValue = 4.5f); // a la medida de un claro
            SetProperty(chaser, "loseRadius", property => property.floatValue = 7f);
            if (defeatedFlag == null)
                SetProperty(chaser, "respawns", property => property.boolValue = false);
            OlmecaPrototypeGenerator.AddSpriteAnimator(root, chaser, spriteRenderer, spriteSet);

            var report = root.AddComponent<ReportOnDefeat>();
            report.EnemyId = OlmecaNarrativeAssets.CorruptedSpiritId;
            report.FlagOnDefeat = defeatedFlag;
            root.AddComponent<CorruptionPulse>().SpriteRenderer = spriteRenderer;
            return root;
        }

        static GameObject CreateFragment(Transform parent, int index, Dictionary<string, Sprite> sprites, Vector2 point,
            string collectedFlag, StringEventChannel messageChannel)
        {
            var renderer = PlacedSprite($"Fragmento_{index}", parent, sprites, FragmentSprite, point);
            var fragment = renderer.gameObject;

            var pickup = new GameObject("Recoger") { layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.InteractableLayerName) };
            pickup.transform.SetParent(fragment.transform, false);
            pickup.transform.position = ToWorld(point) + Vector3.up * 0.3f;
            var trigger = pickup.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;

            var interactable = pickup.AddComponent<MemoryFragmentInteractable>();
            SetProperty(interactable, "text", property => property.stringValue = OlmecaNarrativeAssets.FragmentTexts[index - 1]);
            SetProperty(interactable, "messageChannel", property => property.objectReferenceValue = messageChannel);
            SetProperty(interactable, "examineId", property => property.stringValue = OlmecaNarrativeAssets.FragmentId);
            SetProperty(interactable, "collectedFlag", property => property.stringValue = collectedFlag);
            return fragment;
        }

        // ---------- Entrada ----------

        // El guerrero espera ante la entrada desde que da MP_OLM_03 (en la ciudad) y reconoce el lugar cuando el jugador
        // trae los tres fragmentos. La zona avisa "llegar:entrada_estructura" para pasos futuros (la mazmorra).
        static void BuildEntrance(Transform parent, RegionPoints points, OlmecaNarrativeAssets.Content narrative,
            StringEventChannel messageChannel)
        {
            AddZone(parent, OlmecaNarrativeAssets.EntranceZoneId, points.entrada.Value, new Vector3(4f, 4f, 2f));

            var spriteSet = OlmecaPrototypeGenerator.CreateSpriteSet("Guerrero", PlaceholderSpriteFactory.Warrior);
            var warrior = CreateSettler(Group("Guerrero", parent), narrative.WarriorAtEntrance, spriteSet, messageChannel,
                new[] { points.guerrero.Value });
            ShowWhen(warrior, new[] { OlmecaNarrativeAssets.WarriorInJungleFlag }, null, liveUpdate: true);
        }
    }
}
