using System.Collections.Generic;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Genera los pisos de la mazmorra olmeca, «La ciudad bajo la selva» (M2: la base), a partir de Mazmorra_P&lt;n&gt;.json,
    /// que exporta el flujo de arte (Tools/ArtPipeline/generar_olmeca.py, planificar_piso). Cada piso es una escena: se
    /// sube por la escalera del muro (al piso de arriba o a la selva) y se baja por la del suelo. El piso 1 tiene un
    /// santuario de Memoria (punto de control y guardado), el 2 uno de Vida (cura una vez por incursión) y el 3, la
    /// antesala sin enemigos, otro de Memoria ante la losa sellada del Guardián (su arena llega en M4). Los enemigos no
    /// reaparecen mientras se está en el piso, pero vuelven al cargarlo de nuevo. Al caer, DefeatReturn devuelve al
    /// jugador al último santuario de Memoria.
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

        public static string SceneName(int floor) => $"Olmeca_Mazmorra_P{floor}";
        public static string ScenePath(int floor) => $"Assets/_Project/Scenes/Regions/Olmeca/{SceneName(floor)}.unity";
        static string DataPath(int floor) => $"Assets/_Project/Data/World/Olmeca/Mazmorra_P{floor}.json";

        /// <summary>Id del santuario de Memoria del piso, que también es el de su punto de aparición.</summary>
        public static string MemoryShrineId(int floor) => $"memoria_p{floor}";
        public static string LifeShrineId(int floor) => $"vida_p{floor}";
        /// <summary>Aviso "llegar:mazmorra_pisoN" al entrar en el piso desde arriba (para misiones futuras).</summary>
        public static string FloorZoneId(int floor) => $"mazmorra_piso{floor}";

        const string ExpeditionText =
            "Al pie de la estela hay una lanza partida y huellas de sandalia en el polvo. Alguien llegó hasta aquí... y dio media vuelta.";

        const string ErasedStelaText =
            "La estela está lisa, pero el polvo de su base guarda la forma de un relieve que ya no existe.";

        const string SealText =
            "Una estela de basalto, puesta como losa, tapa un pórtico en el muro. De su base salen grietas violetas, como " +
            "raíces. Del otro lado algo respira despacio, y es la piedra misma.";

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

            var placed = new Dictionary<string, SpriteRenderer>();
            BuildScenery($"--- Piso {floor} ---", data, sprites, (placement, renderer) =>
            {
                placed[placement.sprite] = renderer;
                AddFloorInscription(floor, placement, renderer, messageChannel);
            });

            // Más oscuro que la selva: solo ilumina a los personajes (el escenario lleva su luz pintada).
            ConfigureLighting(0.85f, new Color(0.4f, 0.42f, 0.4f));
            var player = PlacePlayer(scene, points.llegada_arriba.Value);
            BuildTravel(Group("--- Escaleras ---", null), floor, points);
            BuildShrines(Group("--- Santuarios ---", null), floor, points, sprites, placed, messageChannel);
            BuildEnemies(Group("--- Espíritus corrompidos ---", null), points);

            var defeat = new GameObject("Derrota").AddComponent<DefeatReturn>();
            defeat.Player = player.GetComponent<Health>();

            CreateCamera(player.transform, data.limites, Background);
            CreateHud(messageChannel);

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
                AddInscription(renderer.transform.parent, $"Inscripcion_{placement.sprite}", position,
                    floor == 1 ? ExpeditionText : ErasedStelaText, messageChannel, examineId: "estela");
        }

        // ---------- Escaleras ----------

        static void BuildTravel(Transform parent, int floor, RegionPoints points)
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
            AddPortal(parent, "Portal_Bajada", points.bajada.Value, new Vector3(0.9f, 1.3f, 2f), SceneName(floor + 1), FromAboveSpawnId);
            AddSpawnPoint(parent, FromBelowSpawnId, points.llegada_abajo.Value);
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

        // ---------- Enemigos ----------

        static void BuildEnemies(Transform parent, RegionPoints points)
        {
            if (points.enemigos is not { Length: > 0 })
                return;
            var spriteSet = OlmecaPrototypeGenerator.CreateSpriteSet("EspirituCorrompido", PlaceholderSpriteFactory.CorruptedJaguarSpirit);
            for (var i = 0; i < points.enemigos.Length; i++)
                OlmecaJungleGenerator.CreateCorruptedSpirit(parent, $"EspirituCorrompido_{i + 1}", spriteSet, points.enemigos[i].Value, null);
        }
    }
}
