using System;
using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.Gameplay.Movement;
using CronicasDeMexico.Gameplay.Narrative;
using CronicasDeMexico.Gameplay.Npc;
using CronicasDeMexico.Gameplay.Travel;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Piezas comunes de las escenas de pixel art prerenderizado (la ciudad y la selva olmecas), a partir del
    /// &lt;Región&gt;.json que exporta el flujo de arte (Tools/ArtPipeline/generar_olmeca.py): suelo horneado, agua animada,
    /// objetos orientados al plano de la cámara y alineados a la rejilla de píxeles, colisiones como cajas invisibles,
    /// jugador, cámara, HUD, inscripciones, personajes y viaje entre escenas.
    /// </summary>
    internal static class PixelArtSceneBuilder
    {
        internal const float PlatformHeight = 1f;          // H_PLATAFORMA del flujo
        const float BoundsInset = 7f;                      // la cámara no se acerca tanto al borde en rombo del mapa
        internal const float PlaceholderPixelsPerUnit = 32f;
        const string PresentationPanelSettingsPath = "Assets/_Project/Settings/UI/PanelSettings_PixelArt.asset";
        const string HudThemePath = "Assets/_Project/Art/UI/Hud/HudTheme.tss";
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
            ("Entrada_Estructura", new Vector3(2.9f, 3.8f, 1.5f)), // el montículo con sus columnas
            // Mazmorra (los bordes de cada sala ya los cierra el vacío; el muro solo evita pisar sus columnas)
            ("Muro_Basalto_Y", new Vector3(0.3f, 1f, 1.7f)),
            ("Muro_Basalto", new Vector3(1f, 0.3f, 1.7f)),
            ("Santuario_Memoria", new Vector3(0.9f, 1.3f, 1.4f)),
            ("Santuario_Vida", new Vector3(0.8f, 0.8f, 0.4f)),
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
            // La escalera de subida bloquea en el pórtico; los peldaños quedan libres para pisar el portal.
            ("Escalera_Subida_01", new[] { (new Vector2(-0.65f, 0f), new Vector3(0.5f, 2f, 2f)) }),
        };

        // Capas que comparten lienzo y pivote con su objeto pero se ordenan por la profundidad de otro punto (x, y del
        // flujo, respecto al objeto): el poste delantero de la enramada (POSTE_FRENTE en generar_olmeca.py).
        static readonly (string sprite, Vector2 anchor)[] DepthAnchors = { ("Enramada_Poste", new Vector2(0.9f, -0.6f)) };

        // Techos bajo los que se pasa (RoofOverhead): media huella en x, y del flujo.
        static readonly (string sprite, Vector2 halfExtents)[] Roofs = { ("Enramada_Techo", new Vector2(1.3f, 1f)) };

        // ---------- Datos (<Región>.json) ----------

        [Serializable] internal sealed class Cell { public int x, y; }
        [Serializable] internal sealed class GroundChunk { public string sprite; public float derecha, arriba; }
        [Serializable] internal sealed class Placement { public string sprite; public float x, y, z; }
        [Serializable] internal sealed class Level { public Cell[] celdas; public int z; }
        [Serializable] internal sealed class Pier { public float x0, x1, y, ancho, z; }
        [Serializable] internal sealed class Point { public float x, y; public Vector2 Value => new(x, y); }

        /// <summary>Puntos de juego en x, y del flujo. Cada región usa los suyos; los demás quedan a cero.</summary>
        [Serializable]
        internal sealed class RegionPoints
        {
            public Point salida_selva, llegada_selva;                    // ciudad
            public Point llegada, salida, zona_selva, entrada, guerrero; // selva
            public Point[] claros, fragmentos;
            public Point mazmorra, desde_mazmorra;                       // selva: entrada al piso 1
            public Point subida, llegada_arriba, bajada, llegada_abajo;  // pisos de la mazmorra
            public Point memoria, memoria_llegada, vida, sello, jaguar, guardian;
            public Point[] enemigos;
            public Point[] ecos; // de tres en tres: los sitios de cada eco borrado
            // Mazmorra, recompensas (M5): ofrendas por tipo y el basalto agrietado que tapa la oculta
            public Point ofrenda_comun, ofrenda_especial, ofrenda_oculta, ofrenda_guardian, ofrenda_final, agrietado;

            /// <summary>Si el punto existe en los datos (JsonUtility deja a cero los que faltan).</summary>
            public static bool Has(Point point) => point != null && (point.x != 0f || point.y != 0f);
        }

        [Serializable]
        internal sealed class RegionData
        {
            public Cell[] agua;
            public GroundChunk[] suelo;
            public Placement[] objetos;
            public Level[] niveles;
            public Placement[] rampas;
            public Cell[] agua_bloqueo; // el agua sin las celdas bajo los muelles
            public Pier[] muelles;
            public float[] limites; // x0, x1, y0, y1 del flujo
            public Cell[] vacio;    // mazmorra: celdas sin piso, que no se atraviesan
            public bool agua_vadeable; // el agua baja del Pantano se cruza, más despacio (SlowZone)
            public Cell[] inundacion;  // arena del Guardián: agua que aparece en su fase 2 (no se hornea)
            public RegionPoints puntos;
        }

        internal static RegionData LoadData(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path)
                ?? throw new InvalidOperationException($"No se encontró {path}: ejecuta el flujo de arte.");
            return JsonUtility.FromJson<RegionData>(asset.text);
        }

        internal static Dictionary<string, Sprite> LoadSprites()
        {
            var folders = new[] { "Objetos", "Agua", "Suelo" }
                .Select(folder => $"{PixelArtSpriteImporter.SpritesFolder}/{folder}")
                .ToArray();
            return AssetDatabase.FindAssets("t:Sprite", folders)
                .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(sprite => sprite != null)
                .ToDictionary(sprite => sprite.name);
        }

        internal static Vector3 ToWorld(float x, float y, float z) => IsometricProjection.FromPipeline(x, y, z);

        internal static Vector3 ToWorld(Vector2 point) => ToWorld(point.x, point.y, 0f);

        internal static SpriteRenderer SpriteObject(string name, Transform parent, Sprite sprite, Material material, Vector3 position)
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

        internal static Sprite Require(Dictionary<string, Sprite> sprites, string name) =>
            sprites.TryGetValue(name, out var sprite)
                ? sprite
                : throw new InvalidOperationException($"Falta el sprite '{name}' en {PixelArtSpriteImporter.SpritesFolder}.");

        /// <summary>Un objeto del escenario alineado a la rejilla de píxeles, fuera de los datos (p. ej. un fragmento).</summary>
        internal static SpriteRenderer PlacedSprite(string name, Transform parent, Dictionary<string, Sprite> sprites, string sprite,
            Vector2 point)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);
            return SpriteObject(name, parent, Require(sprites, sprite), material,
                IsometricProjection.SnapToPixelGrid(ToWorld(point), 1f / IsometricProjection.PixelsPerUnit));
        }

        // ---------- Escenario ----------

        /// <summary>
        /// Suelo, agua, objetos y colisiones de la región bajo un grupo raíz. onObject permite añadir algo a cada objeto
        /// (p. ej. inscripciones a altares y estelas).
        /// </summary>
        internal static Transform BuildScenery(string rootName, RegionData data, Dictionary<string, Sprite> sprites,
            Action<Placement, SpriteRenderer> onObject = null)
        {
            PixelArtAssets.CreateMaterials();
            var objectMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);
            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.GroundMaterialPath);
            var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.WaterMaterialPath);

            var root = Group(rootName, null);
            BuildGround(Group("Suelo", root), data, sprites, groundMaterial);
            if (data.agua is { Length: > 0 })
                BuildWater(Group("Agua", root), data, sprites, waterMaterial);
            BuildObjects(Group("Objetos", root), data, sprites, objectMaterial, onObject);
            BuildColliders(Group("Colisiones", root), data);
            return root;
        }

        // Los trozos del suelo horneado ya están en píxeles enteros respecto al origen: no se realinean.
        // Cada trozo lleva su propio material con la textura asignada: con un material compartido, los
        // SpriteRenderers de texturas distintas se dibujaban todos con la textura del primero (los objetos y el agua no
        // lo sufren porque comparten la textura del atlas).
        static void BuildGround(Transform parent, RegionData data, Dictionary<string, Sprite> sprites, Material material)
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

        static void BuildWater(Transform parent, RegionData data, Dictionary<string, Sprite> sprites, Material material)
        {
            var frames = Enumerable.Range(1, 4).Select(i => Require(sprites, $"Tile_Agua_Anim_{i:00}")).ToArray();
            var renderers = data.agua
                .Select(cell => SpriteObject($"Agua_{cell.x}_{cell.y}", parent, frames[0], material,
                    IsometricProjection.SnapToPixelGrid(ToWorld(cell.x, cell.y, 0f), 1f / IsometricProjection.PixelsPerUnit)))
                .ToArray();
            parent.gameObject.AddComponent<SpriteFrameCycler>().Configure(frames, renderers);
        }

        static void BuildObjects(Transform parent, RegionData data, Dictionary<string, Sprite> sprites, Material material,
            Action<Placement, SpriteRenderer> onObject)
        {
            var groups = new Dictionary<string, Transform>();
            foreach (var placement in data.objetos.Concat(data.rampas ?? Array.Empty<Placement>()))
            {
                var category = placement.sprite.Split('_')[0];
                if (!groups.TryGetValue(category, out var group))
                    groups[category] = group = Group(category, parent);

                var position = IsometricProjection.SnapToPixelGrid(ToWorld(placement.x, placement.y, placement.z),
                    1f / IsometricProjection.PixelsPerUnit);
                // A lo largo de la mirada de la cámara: cambia la profundidad, no el sitio en pantalla
                var forward = IsometricProjection.CameraRotation * Vector3.forward;
                var anchor = DepthAnchors.FirstOrDefault(a => placement.sprite.StartsWith(a.sprite)).anchor;
                position += forward * Vector3.Dot(ToWorld(anchor.x, anchor.y, 0f), forward);
                var renderer = SpriteObject(placement.sprite, group, Require(sprites, placement.sprite), material, position);

                var roof = Roofs.FirstOrDefault(r => placement.sprite.StartsWith(r.sprite)).halfExtents;
                if (roof != Vector2.zero)
                {
                    var overhead = renderer.gameObject.AddComponent<RoofOverhead>();
                    SetProperty(overhead, "halfExtents", property => property.vector2Value = new Vector2(roof.y, roof.x));
                }

                onObject?.Invoke(placement, renderer);
            }
        }

        /// <summary>Los techos bajo los que se pasa se adelantan cuando el jugador está debajo.</summary>
        internal static void LinkRoofs(Transform root, Transform player)
        {
            foreach (var roof in root.GetComponentsInChildren<RoofOverhead>())
                SetProperty(roof, "target", property => property.objectReferenceValue = player);
        }

        // ---------- Colisiones ----------

        static void BuildColliders(Transform parent, RegionData data)
        {
            float x0 = data.limites[0] - 0.5f, x1 = data.limites[1] + 0.5f, y0 = data.limites[2] - 0.5f, y1 = data.limites[3] + 0.5f;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, sx = x1 - x0, sy = y1 - y0;

            Box("Suelo", parent, ToWorld(cx, cy, -0.5f), new Vector3(sy, 1f, sx));
            const float wallHeight = 3f;
            Box("Limite_Sur", parent, ToWorld(cx, y0 - 0.5f, wallHeight * 0.5f), new Vector3(1f, wallHeight, sx + 2f));
            Box("Limite_Norte", parent, ToWorld(cx, y1 + 0.5f, wallHeight * 0.5f), new Vector3(1f, wallHeight, sx + 2f));
            Box("Limite_Oeste", parent, ToWorld(x0 - 0.5f, cy, wallHeight * 0.5f), new Vector3(sy + 2f, wallHeight, 1f));
            Box("Limite_Este", parent, ToWorld(x1 + 0.5f, cy, wallHeight * 0.5f), new Vector3(sy + 2f, wallHeight, 1f));

            if (data.agua is { Length: > 0 } && data.agua_vadeable)
            {
                // Agua baja: no bloquea; frena a quien la pisa.
                var water = Group("Agua baja", parent);
                foreach (var (x, y, length) in Runs(data.agua))
                {
                    var zone = new GameObject($"AguaBaja_{x}_{y}");
                    zone.transform.SetParent(water, false);
                    zone.transform.position = ToWorld(x, y, 0.5f);
                    var trigger = zone.AddComponent<BoxCollider>();
                    trigger.isTrigger = true;
                    trigger.size = new Vector3(1f, 1f, length);
                    zone.AddComponent<SlowZone>();
                }
            }
            else if (data.agua is { Length: > 0 })
            {
                var water = Group("Agua", parent);
                var blockingWater = data.agua_bloqueo is { Length: > 0 } ? data.agua_bloqueo : data.agua;
                foreach (var (x, y, length) in Runs(blockingWater))
                    Box($"Agua_{x}_{y}", water, ToWorld(x, y, 0.5f), new Vector3(1f, 2f, length));
            }

            if (data.vacio is { Length: > 0 })
            {
                var voids = Group("Vacío", parent);
                foreach (var (x, y, length) in Runs(data.vacio))
                    Box($"Vacio_{x}_{y}", voids, ToWorld(x, y, wallHeight * 0.5f), new Vector3(1f, wallHeight, length));
            }

            // Tablero de los muelles: se camina sobre él (el agua de debajo no bloquea).
            if (data.muelles is { Length: > 0 })
            {
                var piers = Group("Muelles", parent);
                foreach (var pier in data.muelles)
                    Box($"Muelle_{pier.y}", piers, ToWorld((pier.x0 + pier.x1) * 0.5f, pier.y, pier.z - 0.1f),
                        new Vector3(pier.ancho, 0.2f, pier.x1 - pier.x0));
            }

            if (data.niveles is { Length: > 0 })
            {
                var levels = Group("Montículo", parent);
                foreach (var level in data.niveles)
                {
                    foreach (var (x, y, length) in Runs(level.celdas))
                        Box($"Nivel{level.z}_{x}_{y}", levels, ToWorld(x, y, level.z + PlatformHeight * 0.5f),
                            new Vector3(1f, PlatformHeight, length));
                }
            }

            if (data.rampas is { Length: > 0 })
            {
                var ramps = Group("Rampas", parent);
                foreach (var ramp in data.rampas)
                    Ramp(ramp, ramps);
            }

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
                            ToWorld(placement.x + offset.x, placement.y + offset.y, placement.z + partSize.z * 0.5f),
                            new Vector3(partSize.y, partSize.z, partSize.x));
                    continue;
                }
                var size = Obstacles.FirstOrDefault(o => placement.sprite.StartsWith(o.prefix)).size;
                if (size == Vector3.zero)
                    continue; // vegetación baja y tiles: se atraviesan
                Box(placement.sprite, obstacles, ToWorld(placement.x, placement.y, placement.z + size.z * 0.5f),
                    new Vector3(size.y, size.z, size.x));
            }
        }

        /// <summary>Celdas agrupadas en tramos contiguos a lo largo de x del flujo: (centro x, y, largo).</summary>
        internal static IEnumerable<(float x, int y, int length)> Runs(IEnumerable<Cell> cells)
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
            var surfaceMiddle = ToWorld(ramp.x, ramp.y, ramp.z + PlatformHeight * 0.5f);
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

        /// <summary>Caja trigger centrada en un punto del flujo; size = huella en x, y del flujo y altura.</summary>
        internal static GameObject TriggerBox(string name, Transform parent, Vector2 point, Vector3 size)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.position = ToWorld(point.x, point.y, size.z * 0.5f);
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(size.y, size.z, size.x);
            return zone;
        }

        // ---------- Inscripciones ----------

        internal static void AddInscription(Transform parent, string name, Vector3 position, string text, StringEventChannel messageChannel,
            string examineId = null, string alternateFlag = null, string alternateText = null, string prompt = null)
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
            SetProperty(inscription, "examineId", property => property.stringValue = examineId);
            if (prompt != null)
                SetProperty(inscription, "prompt", property => property.stringValue = prompt);
            if (alternateText != null)
            {
                SetProperty(inscription, "alternateFlag", property => property.stringValue = alternateFlag);
                SetProperty(inscription, "alternateText", property => property.stringValue = alternateText);
            }
        }

        // ---------- Personajes ----------

        internal static Material CharacterMaterial() =>
            AssetDatabase.LoadAssetAtPath<Material>(OlmecaPrototypeGenerator.SpriteMaterialPath)
            ?? throw new InvalidOperationException($"No se encontró {OlmecaPrototypeGenerator.SpriteMaterialPath}.");

        /// <summary>
        /// Raíz de personaje a la escala del jugador de estas escenas (los sprites provisionales están a 32 px por
        /// unidad), con el sprite orientado al plano de la cámara y alineado a los píxeles del escenario.
        /// </summary>
        internal static GameObject CreateCharacter(string name, DirectionalSpriteSet spriteSet, out SpriteRenderer spriteRenderer)
        {
            var root = OlmecaPrototypeGenerator.CreateCharacterRoot(name, spriteSet, CharacterMaterial(), out spriteRenderer);
            root.transform.localScale = Vector3.one * (PlaceholderPixelsPerUnit / IsometricProjection.PixelsPerUnit);
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var billboard = spriteRenderer.GetComponent<SpriteBillboard>();
            SetProperty(billboard, "mode", property => property.enumValueIndex = (int)SpriteBillboard.BillboardMode.CameraPlane);
            spriteRenderer.gameObject.AddComponent<PixelGridSnap>();
            return root;
        }

        /// <summary>Habitante con su NpcData y su DialogueData, quieto o patrullando entre puntos del flujo.</summary>
        internal static GameObject CreateSettler(Transform parent, OlmecaNarrativeAssets.Character character, DirectionalSpriteSet spriteSet,
            StringEventChannel messageChannel, IReadOnlyList<Vector2> points)
        {
            var root = CreateCharacter($"NPC_{character.Npc.Id}", spriteSet, out var spriteRenderer);
            root.transform.SetParent(parent, false);

            var patrol = root.AddComponent<NpcPatrol>();
            OlmecaPrototypeGenerator.AddSpriteAnimator(root, patrol, spriteRenderer, spriteSet);

            // El trigger va en un hijo: el CharacterController de la raíz queda en Default. Radio en unidades locales.
            var talk = new GameObject("Conversacion") { layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.InteractableLayerName) };
            talk.transform.SetParent(root.transform, false);
            var trigger = talk.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.7f, 0f);
            trigger.radius = 1.6f;

            var dialogue = talk.AddComponent<DialogueInteractable>();
            SetProperty(dialogue, "npc", property => property.objectReferenceValue = character.Npc);
            SetProperty(dialogue, "dialogue", property => property.objectReferenceValue = character.Dialogue);
            SetProperty(dialogue, "messageChannel", property => property.objectReferenceValue = messageChannel);
            SetProperty(dialogue, "patrol", property => property.objectReferenceValue = patrol);

            var world = points.Select(ToWorld).ToArray();
            root.transform.position = world[0];
            if (world.Length > 1)
                SetProperty(patrol, "patrolPoints", property =>
                {
                    property.arraySize = world.Length;
                    for (var i = 0; i < world.Length; i++)
                        property.GetArrayElementAtIndex(i).vector3Value = world[i];
                });
            return root;
        }

        /// <summary>Muestra u oculta target según banderas (ActiveWhenFlags va en el padre, que nunca se desactiva).</summary>
        internal static ActiveWhenFlags ShowWhen(GameObject target, string[] required, string[] forbidden, bool liveUpdate)
        {
            var condition = target.transform.parent.gameObject.AddComponent<ActiveWhenFlags>();
            condition.Target = target;
            SetProperty(condition, "requiredFlags", property => SetStrings(property, required));
            SetProperty(condition, "forbiddenFlags", property => SetStrings(property, forbidden));
            SetProperty(condition, "liveUpdate", property => property.boolValue = liveUpdate);
            return condition;
        }

        static void SetStrings(SerializedProperty property, string[] values)
        {
            values ??= Array.Empty<string>();
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        // ---------- Viaje y zonas ----------

        /// <summary>Zona que lleva a otra escena (ScenePortal). size = huella en x, y del flujo y altura.</summary>
        internal static ScenePortal AddPortal(Transform parent, string name, Vector2 point, Vector3 size, string targetScene, string targetSpawnId)
        {
            var portal = TriggerBox(name, parent, point, size).AddComponent<ScenePortal>();
            portal.TargetScene = targetScene;
            portal.TargetSpawnId = targetSpawnId;
            return portal;
        }

        internal static SpawnPoint AddSpawnPoint(Transform parent, string spawnId, Vector2 point)
        {
            var spawn = new GameObject($"Llegada_{spawnId}");
            spawn.transform.SetParent(parent, false);
            spawn.transform.position = ToWorld(point);
            var component = spawn.AddComponent<SpawnPoint>();
            component.SpawnId = spawnId;
            return component;
        }

        /// <summary>Zona que avisa "llegar:id" a las misiones (ReportOnEnter).</summary>
        internal static ReportOnEnter AddZone(Transform parent, string zoneId, Vector2 point, Vector3 size)
        {
            var zone = TriggerBox($"Zona_{zoneId}", parent, point, size).AddComponent<ReportOnEnter>();
            zone.ZoneId = zoneId;
            return zone;
        }

        // ---------- Jugador, luz, cámara y HUD ----------

        // El arte del escenario lleva la luz horneada. Esta luz solo ilumina a los personajes (HD2D_SpriteLit), sin
        // sombras en tiempo real: en estas escenas las sombras también van pintadas.
        internal static void ConfigureLighting() => ConfigureLighting(1.1f, new Color(0.55f, 0.57f, 0.52f));

        internal static void ConfigureLighting(float sunIntensity, Color ambient)
        {
            var sun = new GameObject("Sol (personajes)").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(55f, 15f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = false;
        }

        internal static GameObject PlacePlayer(UnityEngine.SceneManagement.Scene scene, Vector2 start)
        {
            PlayerPrefabUpgrade.Ensure(); // M5: estadísticas y golpe de tierra
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OlmecaPrototypeGenerator.PlayerPrefabPath)
                ?? throw new InvalidOperationException($"No se encontró {OlmecaPrototypeGenerator.PlayerPrefabPath}.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.position = ToWorld(start);
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

        internal static void CreateCamera(Transform player, float[] limits) => CreateCamera(player, limits, BackgroundColor);

        internal static void CreateCamera(Transform player, float[] limits, Color background)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = IsometricProjection.OrthographicSize(1080);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
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
            clearCamera.backgroundColor = background;
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

        internal static StringEventChannel MessageChannel() =>
            AssetDatabase.LoadAssetAtPath<StringEventChannel>(OlmecaPrototypeGenerator.MessageChannelPath);

        internal static void CreateHud(StringEventChannel messageChannel) =>
            OlmecaPrototypeGenerator.CreateHud(
                AssetDatabase.LoadAssetAtPath<StringEventChannel>(OlmecaPrototypeGenerator.PromptChannelPath),
                messageChannel,
                AssetDatabase.LoadAssetAtPath<HealthEventChannel>(OlmecaPrototypeGenerator.PlayerHealthChannelPath));

        // ---------- Utilidades ----------

        internal static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        internal static void SetProperty(Object target, string propertyName, Action<SerializedProperty> assign)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo '{propertyName}'.");
            assign(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == path))
                return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
