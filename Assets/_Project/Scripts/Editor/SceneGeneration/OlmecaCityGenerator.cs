using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Genera la escena jugable de la ciudad olmeca a partir de Ciudad.json, que exporta el flujo de arte
    /// (Tools/ArtPipeline/generar_olmeca.py, planificar_ciudad). Se coloca el suelo horneado, el agua animada y los
    /// sprites de objetos orientados al plano de la cámara y alineados a la rejilla de píxeles. Las colisiones son
    /// cajas invisibles: suelo, agua, niveles del montículo, rampas, monumentos, casas y cercas.
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
        const string PresentationPanelSettingsPath = "Assets/_Project/Settings/UI/PanelSettings_PixelArt.asset";
        const string HudThemePath = "Assets/_Project/Art/UI/Hud/HudTheme.tss";

        const float PlatformHeight = 1f;          // H_PLATAFORMA del flujo
        const float BoundsInset = 7f;             // la cámara no se acerca tanto al borde en rombo del mapa
        const float PlaceholderPixelsPerUnit = 32f;
        static readonly Vector2 PlayerStart = new(0f, 1.5f); // en la plaza, coordenadas del flujo
        static readonly Color BackgroundColor = new(0.075f, 0.141f, 0.102f); // #13241A, el fondo de las vistas previas

        // Huella (x, y del flujo) y altura de los obstáculos. El primer prefijo que coincide gana.
        static readonly (string prefix, Vector3 size)[] Obstacles =
        {
            ("Casa_Bajareque_03", new Vector3(1.5f, 1.5f, 2f)),
            ("Casa_Bajareque", new Vector3(2.4f, 1.8f, 2f)),
            ("Altar_Trono", new Vector3(2.1f, 1.4f, 1.2f)),
            ("Estela_Grande", new Vector3(0.9f, 0.35f, 1.4f)),
            ("Estela_Lisa", new Vector3(0.55f, 0.25f, 1.1f)),
            ("Cerca_Basalto_Y", new Vector3(0.35f, 1.1f, 1.5f)),
            ("Cerca_Basalto", new Vector3(1.1f, 0.35f, 1.5f)),
            ("Palma_Corozo", new Vector3(0.3f, 0.3f, 2f)),
            ("Roca_Basalto", new Vector3(0.45f, 0.45f, 0.4f)),
            ("Escombros_Basalto", new Vector3(0.6f, 0.6f, 0.3f)),
            ("Fogon", new Vector3(0.6f, 0.6f, 0.3f)),
            ("Ceiba", new Vector3(0.6f, 0.6f, 2.5f)),          // solo el tronco: bajo la copa se camina
            ("Vasijas", new Vector3(0.5f, 0.5f, 0.4f)),
            ("Tronco_Caido_01", new Vector3(0.9f, 0.5f, 0.35f)),
            ("Tronco_Caido_02", new Vector3(0.45f, 0.45f, 0.35f)),
        };

        // Obstáculos de varias piezas: centro (x, y del flujo, respecto al objeto), huella y altura de cada pieza. La
        // enramada solo bloquea en los postes y los enseres: bajo el techo se camina.
        static readonly (string sprite, (Vector2 offset, Vector3 size)[] parts)[] CompoundObstacles =
        {
            ("Enramada_01", new[]
            {
                (new Vector2(-0.9f, -0.6f), new Vector3(0.15f, 0.15f, 1.3f)),
                (new Vector2(-0.9f, 0.6f), new Vector3(0.15f, 0.15f, 1.3f)),
                (new Vector2(0.9f, -0.6f), new Vector3(0.15f, 0.15f, 1.3f)),
                (new Vector2(0.9f, 0.6f), new Vector3(0.15f, 0.15f, 1.3f)),
                (new Vector2(-0.39f, 0.36f), new Vector3(0.45f, 0.3f, 0.3f)),  // olla y tecomate
                (new Vector2(0.45f, 0.38f), new Vector3(0.45f, 0.35f, 0.2f)),  // metate
            }),
        };

        // Capas que comparten lienzo y pivote con su objeto pero se ordenan por la profundidad de otro punto (x, y del
        // flujo, respecto al objeto): el poste delantero de la enramada (POSTE_FRENTE en generar_olmeca.py).
        static readonly (string sprite, Vector2 anchor)[] DepthAnchors = { ("Enramada_Poste", new Vector2(0.9f, -0.6f)) };

        // Techos bajo los que se pasa (RoofOverhead): media huella en x, y del flujo.
        static readonly (string sprite, Vector2 halfExtents)[] Roofs = { ("Enramada_Techo", new Vector2(1.3f, 1f)) };

        // Textos al examinar. D = documentado, I = interpretación (Docs/Arte_Olmeca_Demo.md §8).
        const string AltarText =
            "Un altar-trono de basalto. En La Venta, de nichos como este sale la figura de un gobernante sentado " +
            "(Altar 4). Este nicho está vacío.";
        const string StelaText = "Una estela de basalto. Si tuvo relieves, la lluvia y el tiempo los borraron.";
        const string BrokenStelaText = "Una estela partida. Su parte superior yace en el suelo, cubierta de musgo.";

        // ---------- Datos (Ciudad.json) ----------

        [Serializable] sealed class Cell { public int x, y; }
        [Serializable] sealed class GroundChunk { public string sprite; public float derecha, arriba; }
        [Serializable] sealed class Placement { public string sprite; public float x, y, z; }
        [Serializable] sealed class Level { public Cell[] celdas; public int z; }
        [Serializable] sealed class Pier { public float x0, x1, y, ancho, z; }

        [Serializable]
        sealed class CityData
        {
            public Cell[] agua;
            public GroundChunk[] suelo;
            public Placement[] objetos;
            public Level[] niveles;
            public Placement[] rampas;
            public Cell[] agua_bloqueo; // el agua sin las celdas bajo los muelles
            public Pier[] muelles;
            public float[] limites; // x0, x1, y0, y1 del flujo
        }

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
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CityDataPath)
                ?? throw new InvalidOperationException($"No se encontró {CityDataPath}: ejecuta el flujo de arte.");
            var data = JsonUtility.FromJson<CityData>(dataAsset.text);

            PixelArtAssets.CreateMaterials();
            var objectMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);
            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.GroundMaterialPath);
            var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.WaterMaterialPath);
            var sprites = LoadSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var messageChannel = AssetDatabase.LoadAssetAtPath<StringEventChannel>(OlmecaPrototypeGenerator.MessageChannelPath);

            var root = Group("--- Ciudad ---", null);
            BuildGround(Group("Suelo", root), data, sprites, groundMaterial);
            BuildWater(Group("Agua", root), data, sprites, waterMaterial);
            BuildObjects(Group("Objetos", root), data, sprites, objectMaterial, messageChannel);
            BuildColliders(Group("Colisiones", root), data);

            ConfigureLighting();
            var player = PlacePlayer(scene);
            foreach (var roof in root.GetComponentsInChildren<RoofOverhead>())
                SetProperty(roof, "target", property => property.objectReferenceValue = player.transform);
            CreateCamera(player.transform, data.limites);
            OlmecaPrototypeGenerator.CreateHud(
                AssetDatabase.LoadAssetAtPath<StringEventChannel>(OlmecaPrototypeGenerator.PromptChannelPath),
                messageChannel,
                AssetDatabase.LoadAssetAtPath<HealthEventChannel>(OlmecaPrototypeGenerator.PlayerHealthChannelPath));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Generador] Ciudad olmeca generada en {ScenePath}: {data.objetos.Length} objetos.");
        }

        static Dictionary<string, Sprite> LoadSprites()
        {
            var folders = new[] { "Objetos", "Agua", "Suelo" }
                .Select(folder => $"{PixelArtSpriteImporter.SpritesFolder}/{folder}")
                .ToArray();
            return AssetDatabase.FindAssets("t:Sprite", folders)
                .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(sprite => sprite != null)
                .ToDictionary(sprite => sprite.name);
        }

        static Vector3 World(float x, float y, float z) => IsometricProjection.FromPipeline(x, y, z);

        static SpriteRenderer SpriteObject(string name, Transform parent, Sprite sprite, Material material, Vector3 position)
        {
            var spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.SetPositionAndRotation(position, IsometricProjection.CameraRotation);
            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        static Sprite Require(Dictionary<string, Sprite> sprites, string name) =>
            sprites.TryGetValue(name, out var sprite)
                ? sprite
                : throw new InvalidOperationException($"Falta el sprite '{name}' en {PixelArtSpriteImporter.SpritesFolder}.");

        // ---------- Capas ----------

        // Los trozos del suelo horneado ya están en píxeles enteros respecto al origen: no se realinean.
        // Cada trozo lleva su propio material con la textura asignada: con un material compartido, los
        // SpriteRenderers de texturas distintas se dibujaban todos con la textura del primero (los objetos y el agua no
        // lo sufren porque comparten la textura del atlas).
        static void BuildGround(Transform parent, CityData data, Dictionary<string, Sprite> sprites, Material material)
        {
            var right = IsometricProjection.CameraRotation * Vector3.right;
            var up = IsometricProjection.CameraRotation * Vector3.up;
            foreach (var chunk in data.suelo)
            {
                var sprite = Require(sprites, chunk.sprite);
                SpriteObject(chunk.sprite, parent, sprite, GroundChunkMaterial(material, sprite), right * chunk.derecha + up * chunk.arriba);
            }
        }

        static Material GroundChunkMaterial(Material template, Sprite sprite)
        {
            var folder = PixelArtAssets.MaterialsFolder + "/Suelo";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(PixelArtAssets.MaterialsFolder, "Suelo");

            var path = $"{folder}/M_{sprite.name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(template);
                AssetDatabase.CreateAsset(material, path);
            }

            material.CopyPropertiesFromMaterial(template);
            material.renderQueue = template.renderQueue;
            material.mainTexture = sprite.texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void BuildWater(Transform parent, CityData data, Dictionary<string, Sprite> sprites, Material material)
        {
            var frames = Enumerable.Range(1, 4).Select(i => Require(sprites, $"Tile_Agua_Anim_{i:00}")).ToArray();
            var renderers = data.agua
                .Select(cell => SpriteObject($"Agua_{cell.x}_{cell.y}", parent, frames[0], material,
                    IsometricProjection.SnapToPixelGrid(World(cell.x, cell.y, 0f), 1f / IsometricProjection.PixelsPerUnit)))
                .ToArray();
            parent.gameObject.AddComponent<SpriteFrameCycler>().Configure(frames, renderers);
        }

        static void BuildObjects(Transform parent, CityData data, Dictionary<string, Sprite> sprites, Material material,
            StringEventChannel messageChannel)
        {
            var groups = new Dictionary<string, Transform>();
            foreach (var placement in data.objetos.Concat(data.rampas))
            {
                var category = placement.sprite.Split('_')[0];
                if (!groups.TryGetValue(category, out var group))
                    groups[category] = group = Group(category, parent);

                var position = IsometricProjection.SnapToPixelGrid(World(placement.x, placement.y, placement.z),
                    1f / IsometricProjection.PixelsPerUnit);
                // A lo largo de la mirada de la cámara: cambia la profundidad, no el sitio en pantalla
                var forward = IsometricProjection.CameraRotation * Vector3.forward;
                var anchor = DepthAnchors.FirstOrDefault(a => placement.sprite.StartsWith(a.sprite)).anchor;
                position += forward * Vector3.Dot(World(anchor.x, anchor.y, 0f), forward);
                var renderer = SpriteObject(placement.sprite, group, Require(sprites, placement.sprite), material, position);

                var roof = Roofs.FirstOrDefault(r => placement.sprite.StartsWith(r.sprite)).halfExtents;
                if (roof != Vector2.zero)
                {
                    var overhead = renderer.gameObject.AddComponent<RoofOverhead>();
                    SetProperty(overhead, "halfExtents", property => property.vector2Value = new Vector2(roof.y, roof.x));
                }

                var text = placement.sprite.StartsWith("Altar_Trono") ? AltarText
                    : placement.sprite == "Estela_Grande_03" ? BrokenStelaText
                    : placement.sprite.StartsWith("Estela") ? StelaText
                    : null;
                if (text != null)
                    AddInscription(renderer.transform.parent, $"Inscripcion_{placement.sprite}",
                        World(placement.x, placement.y, placement.z) + Vector3.up * 0.8f, text, messageChannel);
            }
        }

        static void AddInscription(Transform parent, string name, Vector3 position, string text, StringEventChannel messageChannel)
        {
            var layer = LayerMask.NameToLayer(OlmecaPrototypeGenerator.InteractableLayerName);
            var inscriptionObject = new GameObject(name) { layer = layer };
            inscriptionObject.transform.SetParent(parent, false);
            inscriptionObject.transform.position = position;
            var trigger = inscriptionObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;

            var inscription = inscriptionObject.AddComponent<InscriptionInteractable>();
            SetProperty(inscription, "text", property => property.stringValue = text);
            SetProperty(inscription, "messageChannel", property => property.objectReferenceValue = messageChannel);
        }

        // ---------- Colisiones ----------

        static void BuildColliders(Transform parent, CityData data)
        {
            float x0 = data.limites[0] - 0.5f, x1 = data.limites[1] + 0.5f, y0 = data.limites[2] - 0.5f, y1 = data.limites[3] + 0.5f;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, sx = x1 - x0, sy = y1 - y0;

            Box("Suelo", parent, World(cx, cy, -0.5f), new Vector3(sy, 1f, sx));
            const float wallHeight = 3f;
            Box("Limite_Sur", parent, World(cx, y0 - 0.5f, wallHeight * 0.5f), new Vector3(1f, wallHeight, sx + 2f));
            Box("Limite_Norte", parent, World(cx, y1 + 0.5f, wallHeight * 0.5f), new Vector3(1f, wallHeight, sx + 2f));
            Box("Limite_Oeste", parent, World(x0 - 0.5f, cy, wallHeight * 0.5f), new Vector3(sy + 2f, wallHeight, 1f));
            Box("Limite_Este", parent, World(x1 + 0.5f, cy, wallHeight * 0.5f), new Vector3(sy + 2f, wallHeight, 1f));

            var water = Group("Agua", parent);
            var blockingWater = data.agua_bloqueo != null && data.agua_bloqueo.Length > 0 ? data.agua_bloqueo : data.agua;
            foreach (var (x, y, length) in Runs(blockingWater))
                Box($"Agua_{x}_{y}", water, World(x, y, 0.5f), new Vector3(1f, 2f, length));

            // Tablero de los muelles: se camina sobre él (el agua de debajo no bloquea).
            var piers = Group("Muelles", parent);
            foreach (var pier in data.muelles ?? Array.Empty<Pier>())
                Box($"Muelle_{pier.y}", piers, World((pier.x0 + pier.x1) * 0.5f, pier.y, pier.z - 0.1f),
                    new Vector3(pier.ancho, 0.2f, pier.x1 - pier.x0));

            var levels = Group("Montículo", parent);
            foreach (var level in data.niveles)
            {
                foreach (var (x, y, length) in Runs(level.celdas))
                    Box($"Nivel{level.z}_{x}_{y}", levels, World(x, y, level.z + PlatformHeight * 0.5f),
                        new Vector3(1f, PlatformHeight, length));
            }

            var ramps = Group("Rampas", parent);
            foreach (var ramp in data.rampas)
                Ramp(ramp, ramps);

            var obstacles = Group("Obstáculos", parent);
            foreach (var placement in data.objetos)
            {
                if (placement.z > 0.01f)
                    continue; // plantas sobre el montículo
                var parts = CompoundObstacles.FirstOrDefault(o => placement.sprite == o.sprite).parts;
                if (parts != null)
                {
                    foreach (var (offset, partSize) in parts)
                        Box(placement.sprite, obstacles,
                            World(placement.x + offset.x, placement.y + offset.y, placement.z + partSize.z * 0.5f),
                            new Vector3(partSize.y, partSize.z, partSize.x));
                    continue;
                }
                var size = Obstacles.FirstOrDefault(o => placement.sprite.StartsWith(o.prefix)).size;
                if (size == Vector3.zero)
                    continue; // vegetación baja y tiles: se atraviesan
                Box(placement.sprite, obstacles, World(placement.x, placement.y, placement.z + size.z * 0.5f),
                    new Vector3(size.y, size.z, size.x));
            }
        }

        /// <summary>Celdas agrupadas en tramos contiguos a lo largo de x del flujo: (centro x, y, largo).</summary>
        static IEnumerable<(float x, int y, int length)> Runs(IEnumerable<Cell> cells)
        {
            foreach (var row in cells.GroupBy(c => c.y))
            {
                var xs = row.Select(c => c.x).OrderBy(x => x).ToArray();
                var start = 0;
                for (var i = 1; i <= xs.Length; i++)
                {
                    if (i < xs.Length && xs[i] == xs[i - 1] + 1)
                        continue;
                    yield return ((xs[start] + xs[i - 1]) * 0.5f, row.Key, i - start);
                    start = i;
                }
            }
        }

        // Rampa de 2 m que sube 1 m: una losa inclinada que entra un poco en el suelo y en el nivel de arriba.
        // Variante 01 sube hacia +Y del flujo (+X de Unity); 02 sube hacia -X del flujo (+Z de Unity).
        static void Ramp(Placement ramp, Transform parent)
        {
            const float thickness = 0.2f, length = 2.6f;
            var angle = Mathf.Atan2(PlatformHeight, 2f) * Mathf.Rad2Deg;
            var risesAlongUnityX = ramp.sprite.EndsWith("01");
            var rotation = risesAlongUnityX ? Quaternion.Euler(0f, 0f, angle) : Quaternion.Euler(-angle, 0f, 0f);
            var surfaceMiddle = World(ramp.x, ramp.y, ramp.z + PlatformHeight * 0.5f);
            var normal = rotation * Vector3.up;
            var size = risesAlongUnityX ? new Vector3(length, thickness, 1f) : new Vector3(1f, thickness, length);

            var rampObject = new GameObject(ramp.sprite);
            rampObject.transform.SetParent(parent, false);
            rampObject.transform.SetPositionAndRotation(surfaceMiddle - normal * (thickness * 0.5f), rotation);
            rampObject.AddComponent<BoxCollider>().size = size;
        }

        static void Box(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.transform.position = center;
            box.AddComponent<BoxCollider>().size = size;
        }

        // ---------- Jugador, luz y cámara ----------

        // El arte del escenario lleva la luz horneada. Esta luz solo ilumina a los personajes (HD2D_SpriteLit), sin
        // sombras en tiempo real: en esta escena las sombras también van pintadas.
        static void ConfigureLighting()
        {
            var sun = new GameObject("Sol (personajes)").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(55f, 15f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.52f);
            RenderSettings.fog = false;
        }

        static GameObject PlacePlayer(UnityEngine.SceneManagement.Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OlmecaPrototypeGenerator.PlayerPrefabPath)
                ?? throw new InvalidOperationException($"No se encontró {OlmecaPrototypeGenerator.PlayerPrefabPath}.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.position = World(PlayerStart.x, PlayerStart.y, 0f);
            // El sprite provisional está a 32 px por unidad: se escala para que cada píxel coincida con los del
            // escenario (45,25 px por unidad). La escala va en la raíz porque el animador voltea el sprite con su
            // escala X; también reduce el CharacterController a la talla del arte (~1 m).
            player.transform.localScale = Vector3.one * (PlaceholderPixelsPerUnit / IsometricProjection.PixelsPerUnit);

            var sprite = player.GetComponentInChildren<SpriteBillboard>();
            // Orientado al plano de la cámara: escribe la profundidad de sus pies, igual que los objetos.
            SetProperty(sprite, "mode", property => property.enumValueIndex = (int)SpriteBillboard.BillboardMode.CameraPlane);
            sprite.gameObject.AddComponent<PixelGridSnap>();
            sprite.GetComponent<SpriteRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return player;
        }

        static void CreateCamera(Transform player, float[] limits)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = IsometricProjection.OrthographicSize(1080);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            cameraObject.transform.SetPositionAndRotation(player.position - IsometricProjection.CameraRotation * Vector3.forward * 60f,
                IsometricProjection.CameraRotation);

            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None; // el AA difumina el pixel art
            cameraData.renderShadows = false;
            cameraObject.AddComponent<AudioListener>();

            // Dibuja a la resolución del arte y un panel por debajo del HUD lo amplía (ver PixelArtScreen).
            var presentationObject = new GameObject("Pantalla pixel art");
            var presentation = presentationObject.AddComponent<UIDocument>();
            presentation.panelSettings = PresentationPanelSettings();
            cameraObject.AddComponent<PixelArtScreen>().Presentation = presentation;

            // Con la cámara del escenario dibujando en una textura, esta limpia la pantalla bajo el panel. Va antes que
            // la del escenario por si esta vuelve a dibujar en pantalla (PixelArtScreen desactivado).
            var clearCamera = new GameObject("Cámara de fondo").AddComponent<Camera>();
            clearCamera.cullingMask = 0;
            clearCamera.depth = -10f;
            clearCamera.clearFlags = CameraClearFlags.SolidColor;
            clearCamera.backgroundColor = BackgroundColor;
            // No dibuja nada, pero URP le aplicaría a 1080 p lo que pida el nivel de calidad (en Alto, HDR, texturas de
            // profundidad y color, y el SSAO del renderer): se le quita todo y usa el renderer sin efectos.
            clearCamera.allowHDR = false;
            clearCamera.allowMSAA = false;
            var clearCameraData = clearCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            clearCameraData.SetRenderer(PixelArtScreen.ClearRendererIndex);
            clearCameraData.renderPostProcessing = false;
            clearCameraData.renderShadows = false;
            clearCameraData.antialiasing = AntialiasingMode.None;
            clearCameraData.requiresDepthOption = CameraOverrideOption.Off;
            clearCameraData.requiresColorOption = CameraOverrideOption.Off;

            // Límites del punto que mira, en XZ de Unity: x = y del flujo, z = -x del flujo.
            var follow = cameraObject.AddComponent<IsometricCameraFollow>();
            follow.Target = player;
            SetProperty(follow, "boundsMin", property =>
                property.vector2Value = new Vector2(limits[2] + BoundsInset, -limits[1] + BoundsInset));
            SetProperty(follow, "boundsMax", property =>
                property.vector2Value = new Vector2(limits[3] - BoundsInset, -limits[0] - BoundsInset));
        }

        // Píxeles de pantalla sin escalar (la imagen ya llega al tamaño exacto) y por debajo del HUD.
        static PanelSettings PresentationPanelSettings()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PresentationPanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PresentationPanelSettingsPath);
            }

            panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(HudThemePath);
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.scale = 1f;
            panelSettings.sortingOrder = -100;
            panelSettings.clearColor = false;
            EditorUtility.SetDirty(panelSettings);
            return panelSettings;
        }

        // ---------- Utilidades ----------

        static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        static void SetProperty(Object target, string propertyName, Action<SerializedProperty> assign)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo '{propertyName}'.");
            assign(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == path))
                return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
