using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Narrative;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Genera los pisos de la mazmorra olmeca, «La ciudad bajo la selva», a partir de Mazmorra_P&lt;n&gt;.json, que exporta el
    /// flujo de arte (Tools/ArtPipeline/generar_olmeca.py, planificar_piso). Cada piso es una escena: se sube por la
    /// escalera del muro (al piso de arriba o a la selva) y se baja por la del suelo.
    /// - Piso 1, El Umbral (M3): tutorial con pistas (TutorialHint), santuario de Memoria y espíritus corrompidos.
    /// - Piso 2, El Pantano (M3): agua baja que frena (SlowZone), ecos borrados, santuario de Vida y la arena del Jaguar
    ///   de Piedra; la bajada queda tapada por escombros hasta vencerlo.
    /// - Piso 3 (M2): la antesala sin enemigos, con otro santuario de Memoria ante la losa sellada del Guardián (M4).
    /// Los enemigos no reaparecen mientras se está en el piso, pero vuelven al cargarlo de nuevo; el Jaguar, una vez
    /// vencido, no vuelve (bandera). Al caer, DefeatReturn devuelve al jugador al último santuario de Memoria.
    ///
    /// Las escenas se derivan por completo de los datos: regenerarlas las reemplaza (no editarlas a mano).
    /// Desde consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.OlmecaDungeonGenerator.Generate
    /// </summary>
    public static class OlmecaDungeonGenerator
    {
        public const int FloorCount = 3;
        /// <summary>Punto de llegada al bajar desde arriba (delante de la escalera de subida).</summary>
        public const string FromAboveSpawnId = "desde_arriba";
        /// <summary>Punto de llegada al subir desde el piso de abajo (junto a la escalera de bajada).</summary>
        public const string FromBelowSpawnId = "desde_abajo";
        static readonly Color Background = new(0.043f, 0.051f, 0.043f); // #0B0D0B, el vacío bajo el piso
        // El agua del Pantano usa los cuadros del río de la ciudad, más turbios.
        static readonly Color SwampWaterTint = new(0.5f, 0.58f, 0.42f);
        const string CharactersFolder = PixelArtSpriteImporter.SpritesFolder + "/Personajes";
        const string CharacterDataFolder = "Assets/_Project/Data/Characters";
        const int EchoAnchorCount = 3;

        public static string SceneName(int floor) => $"Olmeca_Mazmorra_P{floor}";
        public static string ScenePath(int floor) => $"Assets/_Project/Scenes/Regions/Olmeca/{SceneName(floor)}.unity";
        static string DataPath(int floor) => $"Assets/_Project/Data/World/Olmeca/Mazmorra_P{floor}.json";

        /// <summary>Id del santuario de Memoria del piso, que también es el de su punto de aparición.</summary>
        public static string MemoryShrineId(int floor) => $"memoria_p{floor}";
        public static string LifeShrineId(int floor) => $"vida_p{floor}";
        /// <summary>Aviso "llegar:mazmorra_pisoN" al entrar en el piso desde arriba (MP_OLM_04).</summary>
        public static string FloorZoneId(int floor) => $"mazmorra_piso{floor}";

        const string ExpeditionText =
            "Al pie de la estela hay una lanza partida y huellas de sandalia en el polvo. Alguien llegó hasta aquí... y dio media vuelta.";

        const string ErasedStelaText =
            "La estela está lisa, pero el polvo de su base guarda la forma de un relieve que ya no existe.";

        const string SealText =
            "Una estela de basalto, puesta como losa, tapa un pórtico en el muro. De su base salen grietas violetas, como " +
            "raíces. Del otro lado algo respira despacio, y es la piedra misma.";

        const string JaguarDefeatText =
            "La piedra deja de temblar. Las grietas violetas se apagan y el jaguar se sienta, quieto, como lo tallaron. Por un " +
            "instante recuerdas manos que lo pulían con arena y agua: lo hicieron para vigilar, no para odiar. Al fondo, los " +
            "escombros que tapaban la escalera se desmoronan.";

        // Pistas del tutorial por piso: id, centro y huella (x, y del flujo) de la zona, y texto.
        static readonly (int floor, string id, Vector2 point, Vector2 size, string text)[] Hints =
        {
            (1, "moverse", new Vector2(-12.2f, 0f), new Vector2(1.6f, 1.6f),
                "Muévete con WASD o el stick izquierdo. Con E (o el botón Norte) examinas, hablas y cierras los mensajes."),
            (1, "santuario", new Vector2(-12.6f, -2.3f), new Vector2(1.4f, 1.4f),
                "El nicho del hacha de jade es un santuario de Memoria. Descansa en él para guardar la partida: si caes, " +
                "volverás aquí. Descansar no cura."),
            (1, "atacar", new Vector2(-3.5f, 0f), new Vector2(1f, 3f),
                "Un espíritu corrompido. Ataca con clic izquierdo (o el botón Oeste). Esquiva con Espacio (o el botón Sur): " +
                "mientras esquivas, nada te daña."),
            (1, "grupo", new Vector2(5.5f, 0f), new Vector2(1f, 3f),
                "Dos a la vez. No dejes que te rodeen: golpea y apártate con la esquiva."),
            (1, "bajar", new Vector2(14.5f, 0f), new Vector2(1f, 3f),
                "La escalera del suelo baja al Pantano. Aquí abajo la vida no se recupera sola: cuida la que te queda."),
            (2, "pantano", new Vector2(-4.5f, 1f), new Vector2(1f, 3f),
                "El agua baja te frena, a ti y a lo que te persigue; la esquiva no. Las siluetas borradas disparan desde " +
                "lejos y avisan con un brillo."),
            (2, "arena", new Vector2(8.5f, 1f), new Vector2(1f, 3f),
                "Algo de piedra se mueve al fondo. Mira el suelo: antes de cada golpe aparece una marca."),
        };

        [MenuItem("Crónicas/Generar mazmorra olmeca (pisos 1–3)")]
        static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath(1)) != null &&
                !EditorUtility.DisplayDialog("Generar mazmorra olmeca",
                    "Los pisos ya existen y se reemplazarán enteros (se generan desde Mazmorra_P<n>.json).", "Reemplazar", "Cancelar"))
                return;

            Generate();
        }

        [MenuItem("Crónicas/Borrar partida guardada")]
        static void DeleteSaveFromMenu()
        {
            var save = new SaveService(Application.persistentDataPath);
            if (!save.HasSave)
            {
                EditorUtility.DisplayDialog("Borrar partida guardada", "No hay ninguna partida guardada.", "Aceptar");
                return;
            }
            if (!EditorUtility.DisplayDialog("Borrar partida guardada",
                    $"Se borrará {save.MainPath} y su copia. Al pulsar Play en Boot se empezará en la ciudad.", "Borrar", "Cancelar"))
                return;

            save.Delete();
            Debug.Log("[Guardado] Partida borrada.");
        }

        public static void Generate()
        {
            for (var floor = 1; floor <= FloorCount; floor++)
                GenerateFloor(floor);
        }

        static void GenerateFloor(int floor)
        {
            var data = LoadData(DataPath(floor));
            var points = data.puntos;
            var sprites = LoadSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var messageChannel = MessageChannel();
            // Después de NewScene, que descarga los ScriptableObjects recién creados que nada referencia (MP_OLM_04).
            OlmecaNarrativeAssets.EnsureAssets();
            var bossNameChannel = OlmecaPrototypeGenerator.LoadOrCreateAsset<StringEventChannel>(OlmecaPrototypeGenerator.BossNameChannelPath);
            var bossHealthChannel = OlmecaPrototypeGenerator.LoadOrCreateAsset<HealthEventChannel>(OlmecaPrototypeGenerator.BossHealthChannelPath);

            var placed = new Dictionary<string, SpriteRenderer>();
            var root = BuildScenery($"--- Piso {floor} ---", data, sprites, (placement, renderer) =>
            {
                placed[placement.sprite] = renderer;
                AddFloorInscription(floor, placement, renderer, messageChannel);
            });
            foreach (var water in root.Find("Agua")?.GetComponentsInChildren<SpriteRenderer>() ?? new SpriteRenderer[0])
                water.color = SwampWaterTint;

            // Más oscuro que la selva: solo ilumina a los personajes (el escenario lleva su luz pintada).
            ConfigureLighting(0.85f, new Color(0.4f, 0.42f, 0.4f));
            var player = PlacePlayer(scene, points.llegada_arriba.Value);
            BuildTravel(Group("--- Escaleras ---", null), floor, points, sprites);
            BuildShrines(Group("--- Santuarios ---", null), floor, points, sprites, placed, messageChannel);
            BuildEnemies(Group("--- Espíritus corrompidos ---", null), points);
            BuildEchoes(Group("--- Ecos borrados ---", null), points);
            if (RegionPoints.Has(points.jaguar))
                BuildStoneJaguar(Group("--- Jaguar de Piedra ---", null), points.jaguar.Value, messageChannel, bossNameChannel,
                    bossHealthChannel);
            BuildHints(Group("--- Tutorial ---", null), floor, messageChannel);

            var defeat = new GameObject("Derrota").AddComponent<DefeatReturn>();
            defeat.Player = player.GetComponent<Health>();

            CreateCamera(player.transform, data.limites, Background);
            OlmecaPrototypeGenerator.CreateHud(
                AssetDatabase.LoadAssetAtPath<StringEventChannel>(OlmecaPrototypeGenerator.PromptChannelPath), messageChannel,
                AssetDatabase.LoadAssetAtPath<HealthEventChannel>(OlmecaPrototypeGenerator.PlayerHealthChannelPath),
                bossNameChannel, bossHealthChannel);

            EditorSceneManager.SaveScene(scene, ScenePath(floor));
            AddSceneToBuildSettings(ScenePath(floor));
            AssetDatabase.SaveAssets();
            Debug.Log($"[Generador] Piso {floor} de la mazmorra olmeca generado en {ScenePath(floor)}: {data.objetos.Length} objetos.");
        }

        static void AddFloorInscription(int floor, Placement placement, SpriteRenderer renderer, StringEventChannel messageChannel)
        {
            var position = ToWorld(placement.x, placement.y, placement.z) + Vector3.up * 0.8f;
            if (placement.sprite == "Estela_Grande_01" && floor == FloorCount)
                AddInscription(renderer.transform.parent, "Inscripcion_Sello", position, SealText, messageChannel, examineId: "sello_guardian");
            else if (placement.sprite.StartsWith("Estela_Lisa"))
                AddInscription(renderer.transform.parent, $"Inscripcion_{placement.sprite}_{placement.x}", position,
                    floor == 1 ? ExpeditionText : ErasedStelaText, messageChannel, examineId: "estela");
        }

        // ---------- Escaleras ----------

        static void BuildTravel(Transform parent, int floor, RegionPoints points, Dictionary<string, Sprite> sprites)
        {
            // Subida: en los peldaños, al pie del pórtico. Del piso 1 se sale a la selva.
            var up = points.subida.Value + new Vector2(0.1f, 0f);
            var (upScene, upSpawn) = floor == 1
                ? (OlmecaJungleGenerator.SceneName, OlmecaJungleGenerator.FromDungeonSpawnId)
                : (SceneName(floor - 1), FromBelowSpawnId);
            AddPortal(parent, "Portal_Subida", up, new Vector3(0.5f, 1f, 2f), upScene, upSpawn);
            AddSpawnPoint(parent, FromAboveSpawnId, points.llegada_arriba.Value);
            AddZone(parent, FloorZoneId(floor), points.llegada_arriba.Value, new Vector3(1.5f, 2f, 2f));

            if (!RegionPoints.Has(points.bajada))
                return;
            AddSpawnPoint(parent, FromBelowSpawnId, points.llegada_abajo.Value);
            var down = Group("Bajada", parent);
            var portal = AddPortal(down, "Portal_Bajada", points.bajada.Value, new Vector3(0.9f, 1.3f, 2f), SceneName(floor + 1),
                FromAboveSpawnId);
            if (!RegionPoints.Has(points.jaguar))
                return;

            // Con el Jaguar de Piedra en el piso, unos escombros tapan la escalera hasta vencerlo.
            var defeated = OlmecaNarrativeAssets.StoneJaguarDefeatedFlag;
            portal.gameObject.SetActive(false);
            ShowWhen(portal.gameObject, new[] { defeated }, null, liveUpdate: true);
            var rubble = Group("Escombros", down);
            PlacedSprite("Escombros_Basalto_02", rubble, sprites, "Escombros_Basalto_02", points.bajada.Value + new Vector2(0.15f, 0.35f));
            PlacedSprite("Escombros_Basalto_01", rubble, sprites, "Escombros_Basalto_01", points.bajada.Value + new Vector2(-0.1f, -0.35f));
            ShowWhen(rubble.gameObject, null, new[] { defeated }, liveUpdate: true);
        }

        // ---------- Santuarios ----------

        // El sprite de los datos (variante 01) es el estado inicial; la variante 02 (jade despierto, fuego apagado) se
        // pone en el mismo sitio y ActiveWhenFlags alterna entre ambas. Lo que se toca es un trigger aparte, que nunca
        // se desactiva (PlayerInteractor perdería de vista el mensaje abierto).
        static void BuildShrines(Transform parent, int floor, RegionPoints points, Dictionary<string, Sprite> sprites,
            Dictionary<string, SpriteRenderer> placed, StringEventChannel messageChannel)
        {
            if (RegionPoints.Has(points.memoria))
            {
                var id = MemoryShrineId(floor);
                var flag = DungeonFlags.MemoryShrineAwake(id);
                SwapWithVariant(placed["Santuario_Memoria_01"], Require(sprites, "Santuario_Memoria_02"), flag);

                var shrine = Touchable("Santuario_Memoria", parent, points.memoria.Value).AddComponent<MemoryShrine>();
                shrine.ShrineId = id;
                shrine.SpawnId = id;
                SetProperty(shrine, "messageChannel", property => property.objectReferenceValue = messageChannel);
                AddSpawnPoint(parent, id, points.memoria_llegada.Value);
            }

            if (RegionPoints.Has(points.vida))
            {
                var id = LifeShrineId(floor);
                SwapWithVariant(placed["Santuario_Vida_01"], Require(sprites, "Santuario_Vida_02"), DungeonFlags.LifeShrineUsed(id));

                var shrine = Touchable("Santuario_Vida", parent, points.vida.Value).AddComponent<LifeShrine>();
                shrine.ShrineId = id;
                SetProperty(shrine, "messageChannel", property => property.objectReferenceValue = messageChannel);
            }
        }

        static void SwapWithVariant(SpriteRenderer initial, Sprite variant, string flag)
        {
            var other = SpriteObject(variant.name, initial.transform.parent, variant, initial.sharedMaterial, initial.transform.position);
            other.gameObject.SetActive(false);
            ShowWhen(initial.gameObject, null, new[] { flag }, liveUpdate: true);
            ShowWhen(other.gameObject, new[] { flag }, null, liveUpdate: true);
        }

        static GameObject Touchable(string name, Transform parent, Vector2 point)
        {
            var touch = new GameObject(name) { layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.InteractableLayerName) };
            touch.transform.SetParent(parent, false);
            touch.transform.position = ToWorld(point) + Vector3.up * 0.8f;
            var trigger = touch.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;
            return touch;
        }

        // ---------- Tutorial ----------

        static void BuildHints(Transform parent, int floor, StringEventChannel messageChannel)
        {
            foreach (var (hintFloor, id, point, size, text) in Hints)
            {
                if (hintFloor != floor)
                    continue;
                var hint = TriggerBox($"Pista_{id}", parent, point, new Vector3(size.x, size.y, 2f)).AddComponent<TutorialHint>();
                hint.HintId = id;
                hint.Text = text;
                hint.MessageChannel = messageChannel;
            }
        }

        // ---------- Enemigos ----------

        static void BuildEnemies(Transform parent, RegionPoints points)
        {
            if (points.enemigos is not { Length: > 0 })
                return;
            var spriteSet = OlmecaPrototypeGenerator.CreateSpriteSet("EspirituCorrompido", PlaceholderSpriteFactory.CorruptedJaguarSpirit);
            for (var i = 0; i < points.enemigos.Length; i++)
                OlmecaJungleGenerator.CreateCorruptedSpirit(parent, $"EspirituCorrompido_{i + 1}", spriteSet, points.enemigos[i].Value, null);
        }

        // Cada eco borrado aparece en el primero de sus tres puntos y salta entre ellos. Sus fragmentos son hijos
        // inactivos que se reutilizan.
        static void BuildEchoes(Transform parent, RegionPoints points)
        {
            if (points.ecos is not { Length: >= EchoAnchorCount })
                return;
            var spriteSet = OlmecaPrototypeGenerator.CreateSpriteSet("EcoBorrado", PlaceholderSpriteFactory.ErasedEcho);
            var shard = EffectSpriteFactory.SymbolShard();
            var shardMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);
            for (var e = 0; e < points.ecos.Length / EchoAnchorCount; e++)
            {
                var anchors = Enumerable.Range(0, EchoAnchorCount).Select(i => ToWorld(points.ecos[e * EchoAnchorCount + i].Value)).ToArray();
                var root = CreateCharacter($"EcoBorrado_{e + 1}", spriteSet, out var spriteRenderer);
                root.transform.SetParent(parent, false);
                root.transform.position = anchors[0];
                root.layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.EnemyLayerName);
                OlmecaPrototypeGenerator.AddHealth(root, maxHealth: 2, invulnerabilityTime: 0.2f, spriteRenderer);

                var projectiles = new Projectile[3];
                for (var i = 0; i < projectiles.Length; i++)
                {
                    var projectile = SpriteObject($"Fragmento_{i + 1}", root.transform, shard, shardMaterial, anchors[0]);
                    // Hijo del eco: hereda su escala, que pone los 32 px por unidad del sprite a la rejilla del escenario.
                    projectile.gameObject.SetActive(false);
                    projectiles[i] = projectile.gameObject.AddComponent<Projectile>();
                }

                var echo = root.AddComponent<ErasedEcho>();
                echo.Anchors = anchors;
                SetProperty(echo, "spriteRenderer", property => property.objectReferenceValue = spriteRenderer);
                SetProperty(echo, "projectiles", property =>
                {
                    property.arraySize = projectiles.Length;
                    for (var i = 0; i < projectiles.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = projectiles[i];
                });
                OlmecaPrototypeGenerator.AddSpriteAnimator(root, echo, spriteRenderer, spriteSet);
                root.AddComponent<ReportOnDefeat>().EnemyId = OlmecaNarrativeAssets.ErasedEchoId;
            }
        }

        // ---------- Jaguar de Piedra ----------

        static readonly string[] JaguarPoses = { "Acecho", "Zancada", "Agazapado", "Salto", "Zarpazo" };

        static void BuildStoneJaguar(Transform parent, Vector2 home, StringEventChannel messageChannel,
            StringEventChannel bossNameChannel, HealthEventChannel bossHealthChannel)
        {
            var poses = LoadJaguarSprites();
            DirectionalSpriteSet Pose(string pose, string walkPose = null)
            {
                var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{CharacterDataFolder}/DSS_JaguarPiedra_{pose}.asset");
                set.SetClips(Enumerable.Range(0, DirectionalSpriteSet.DirectionCount)
                    .Select(d => new DirectionalSpriteSet.DirectionClip(poses[pose][d],
                        walkPose == null ? System.Array.Empty<Sprite>() : new[] { poses[walkPose][d], poses[pose][d] }))
                    .ToArray());
                EditorUtility.SetDirty(set);
                return set;
            }
            var stalk = Pose("Acecho", "Zancada");
            var crouch = Pose("Agazapado");
            var leap = Pose("Salto");
            var swipe = Pose("Zarpazo");

            // Raíz sin escalar: los sprites del flujo ya están a la resolución del escenario.
            var root = new GameObject("JaguarDePiedra") { layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.EnemyLayerName) };
            root.transform.SetParent(parent, false);
            root.transform.position = ToWorld(home);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.7f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            controller.stepOffset = 0.3f;
            controller.minMoveDistance = 0f;

            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(root.transform, false);
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = stalk.GetIdle(DirectionalSpriteSet.DirectionCount - 1);
            spriteRenderer.sharedMaterial = CharacterMaterial();
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var billboard = spriteObject.AddComponent<SpriteBillboard>();
            SetProperty(billboard, "mode", property => property.enumValueIndex = (int)SpriteBillboard.BillboardMode.CameraPlane);
            spriteObject.AddComponent<PixelGridSnap>();

            OlmecaPrototypeGenerator.AddHealth(root, maxHealth: 16, invulnerabilityTime: 0.15f, spriteRenderer, bossHealthChannel);

            var markerObject = new GameObject("Aviso");
            markerObject.transform.SetParent(root.transform, false);
            var markerRenderer = markerObject.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = EffectSpriteFactory.TelegraphCircle();
            markerRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.AlertMaterialPath);
            markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
            markerRenderer.enabled = false; // lo enciende TelegraphMarker.Show
            var marker = markerObject.AddComponent<TelegraphMarker>();

            var jaguar = root.AddComponent<StoneJaguar>();
            OlmecaPrototypeGenerator.AddSpriteAnimator(root, jaguar, spriteRenderer, stalk);
            SetProperty(jaguar, "spriteAnimator", property => property.objectReferenceValue = root.GetComponent<DirectionalSpriteAnimator>());
            SetProperty(jaguar, "marker", property => property.objectReferenceValue = marker);
            SetProperty(jaguar, "stalkPose", property => property.objectReferenceValue = stalk);
            SetProperty(jaguar, "crouchPose", property => property.objectReferenceValue = crouch);
            SetProperty(jaguar, "leapPose", property => property.objectReferenceValue = leap);
            SetProperty(jaguar, "swipePose", property => property.objectReferenceValue = swipe);
            SetProperty(jaguar, "bossNameChannel", property => property.objectReferenceValue = bossNameChannel);
            SetProperty(jaguar, "messageChannel", property => property.objectReferenceValue = messageChannel);
            SetProperty(jaguar, "defeatText", property => property.stringValue = JaguarDefeatText);

            var report = root.AddComponent<ReportOnDefeat>();
            report.EnemyId = OlmecaNarrativeAssets.StoneJaguarId;
            report.FlagOnDefeat = OlmecaNarrativeAssets.StoneJaguarDefeatedFlag;
            ShowWhen(root, null, new[] { OlmecaNarrativeAssets.StoneJaguarDefeatedFlag }, liveUpdate: true);

            // Liberado: la escultura queda sentada en el centro de la arena.
            var freed = Group("Liberado", parent);
            var statue = SpriteObject("Jaguar_Piedra_Liberado", freed, poses["Liberado"][0],
                AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath),
                IsometricProjection.SnapToPixelGrid(ToWorld(home), 1f / IsometricProjection.PixelsPerUnit));
            statue.gameObject.SetActive(false);
            ShowWhen(statue.gameObject, new[] { OlmecaNarrativeAssets.StoneJaguarDefeatedFlag }, null, liveUpdate: true);
        }

        // Jaguar_Piedra_<Pose>_<dirección 01–05> (N, NE, E, SE, S); Liberado solo tiene 01.
        static Dictionary<string, Sprite[]> LoadJaguarSprites()
        {
            var all = AssetDatabase.FindAssets("t:Sprite", new[] { CharactersFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(sprite => sprite != null)
                .ToDictionary(sprite => sprite.name);
            Sprite Get(string name) => all.TryGetValue(name, out var sprite)
                ? sprite
                : throw new System.InvalidOperationException($"Falta el sprite '{name}' en {CharactersFolder}: ejecuta el flujo de arte.");

            var result = JaguarPoses.ToDictionary(pose => pose,
                pose => Enumerable.Range(1, DirectionalSpriteSet.DirectionCount).Select(d => Get($"Jaguar_Piedra_{pose}_{d:00}")).ToArray());
            result["Liberado"] = new[] { Get("Jaguar_Piedra_Liberado_01") };
            return result;
        }
    }
}
