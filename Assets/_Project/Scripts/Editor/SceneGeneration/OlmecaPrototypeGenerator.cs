using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Interaction;
using CronicasDeMexico.Gameplay.Npc;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.UI.Hud;
using CronicasDeMexico.World.Rendering;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using BindingMode = Unity.Cinemachine.TargetTracking.BindingMode;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Genera la escena del prototipo olmeca (plaza inspirada en La Venta): prefabs del jugador, el artesano y el
    /// espíritu jaguar con sus sprites provisionales, materiales, inscripciones, canales de eventos y HUD.
    /// Desde consola: Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.SceneGeneration.OlmecaPrototypeGenerator.Generate
    ///
    /// ESCENA CONGELADA (Fase 1): Olmeca_Prototipo se edita a mano desde ahora y el generador se niega a
    /// sobrescribirla. Regenerar reemplaza la escena entera y también los prefabs de personajes, así que se
    /// perderían los cambios manuales y las modificaciones de las instancias. Para regenerar a propósito hay que
    /// borrar o renombrar antes la escena.
    /// </summary>
    public static class OlmecaPrototypeGenerator
    {
        public const string SceneName = "Olmeca_Prototipo";
        public const string ScenePath = "Assets/_Project/Scenes/Regions/Olmeca/" + SceneName + ".unity";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Jugador.prefab";
        public const string ArtisanPrefabPath = "Assets/_Project/Prefabs/Characters/Artesano.prefab";
        public const string JaguarPrefabPath = "Assets/_Project/Prefabs/Enemies/EspirituJaguar.prefab";

        const string PlaceholderFolder = "Assets/_Project/Art/Characters/Placeholder";
        // El material de sprites lo comparten todos los personajes (el destello va por MaterialPropertyBlock).
        const string SpriteMaterialPath = PlaceholderFolder + "/M_Jugador_Placeholder.mat";
        const string CharacterDataFolder = "Assets/_Project/Data/Characters";
        const string EnvironmentMaterialsFolder = "Assets/_Project/Art/Environment/Olmeca/Materials";
        const string SpriteShaderPath = "Assets/_Project/Art/Shaders/HD2D_SpriteLit.shader";
        const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        const string VolumeProfilePath = "Assets/_Project/Settings/Rendering/Volume_HD2D_Base.asset";
        const string MoveActionPath = "Player/Move";
        const string InteractActionPath = "Player/Interact";
        const string OptionsActionPath = "Player/Options";
        const string AttackActionPath = "Player/Attack";
        const string SimpleLitShaderName = "Universal Render Pipeline/Simple Lit";
        const string EventsFolder = "Assets/_Project/Data/Events";
        internal const string PromptChannelPath = EventsFolder + "/EVT_Interaccion_Aviso.asset";
        internal const string MessageChannelPath = EventsFolder + "/EVT_Interaccion_Mensaje.asset";
        internal const string PlayerHealthChannelPath = EventsFolder + "/EVT_Jugador_Vida.asset";
        const string HudLayoutPath = "Assets/_Project/Art/UI/Hud/Hud.uxml";
        const string HudThemePath = "Assets/_Project/Art/UI/Hud/HudTheme.tss";
        const string PanelSettingsPath = "Assets/_Project/Settings/UI/PanelSettings_Hud.asset";
        internal const string InteractableLayerName = "Interactuable";
        const string EnemyLayerName = "Enemigo";

        const string ColossalHeadText =
            "Una cabeza colosal de basalto. Los olmecas tallaron estos retratos, probablemente de sus gobernantes, " +
            "en piedra traída desde la sierra de los Tuxtlas, a más de 80 km de aquí.";
        const string AltarText =
            "Un altar-trono de piedra. En su nicho frontal, una figura sentada sostiene una cuerda que rodea la base, " +
            "como en el Altar 4 de La Venta.";
        const string EnclosureStelaText =
            "Dentro del recinto de columnas de basalto se enterraron ofrendas de jade y grandes mosaicos de serpentina.";
        const string WestStelaText = "Una estela muy gastada. Apenas se distingue una figura de perfil con un gran tocado.";
        const string EastStelaText = "Otra estela. Sus relieves muestran manos que sostienen una ofrenda.";

        const string ArtisanName = "Artesano de jade";
        static readonly string[] ArtisanLines =
        {
            "¡Bienvenido a la plaza! Trabajo el jade que llega de muy lejos.",
            "Lo pulo durante días con arena y agua, hasta que brilla como el pantano al amanecer.",
            "Ten cuidado entre los árboles del este y del oeste: dicen que ahí rondan espíritus jaguar.",
        };

        const int PixelsPerUnit = 32;
        const float PlazaHalfSize = 18f;
        static readonly Vector3 PlayerStart = new(0f, 0f, -13f);
        // Ruta libre entre la cabeza colosal, las estelas y el altar.
        static readonly Vector3[] ArtisanPatrol =
        {
            new(0f, 0f, -1f), new(4.5f, 0f, -1.5f), new(4.5f, 0f, 2.5f), new(0f, 0f, 2.5f),
        };
        static readonly Vector3[] JaguarSpawns = { new(12f, 0f, -9f), new(-13f, 0f, -11f) };
        static readonly Vector3 CameraOffset = new(0f, 6f, -9f);
        const float CameraPitch = 30f;
        const float CameraFieldOfView = 30f;
        static readonly Color FogColor = new(0.73f, 0.75f, 0.70f);

        sealed class Palette
        {
            public Material Earth, Clay, Basalt, Stone, Water, Bark, Foliage;
        }

        [MenuItem("Crónicas/Generar prototipo olmeca")]
        static void GenerateFromMenu()
        {
            if (SceneExists())
            {
                EditorUtility.DisplayDialog("Escena congelada", FrozenMessage, "Entendido");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!EditorUtility.DisplayDialog("Generar prototipo olmeca",
                    "Se creará la escena y se sobrescribirán los prefabs de personajes, sus sprites, los materiales, " +
                    "los canales de eventos y la configuración del HUD generados.",
                    "Generar", "Cancelar"))
                return;

            Generate();
        }

        static bool SceneExists() => AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;

        static string FrozenMessage =>
            $"{ScenePath} está congelada: se edita a mano y el generador no la sobrescribe.\n\n" +
            "Para regenerarla a propósito, bórrala o renómbrala antes. Se reemplazarán también los prefabs de " +
            "personajes y se perderán sus cambios manuales.";

        public static void Generate()
        {
            if (SceneExists())
                throw new System.InvalidOperationException(FrozenMessage);

            EnsureFolder(PlaceholderFolder);
            EnsureFolder(EnvironmentMaterialsFolder);
            EnsureFolder(System.IO.Path.GetDirectoryName(PlayerPrefabPath).Replace('\\', '/'));
            EnsureFolder(System.IO.Path.GetDirectoryName(ArtisanPrefabPath).Replace('\\', '/'));
            EnsureFolder(System.IO.Path.GetDirectoryName(JaguarPrefabPath).Replace('\\', '/'));
            EnsureFolder(CharacterDataFolder);
            EnsureFolder(EventsFolder);
            EnsureFolder("Assets/_Project/Settings/UI");

            // La escena se crea antes que los assets: NewScene descarga los ScriptableObjects recién creados que
            // nada referencia todavía, y las referencias a ellos se guardarían vacías.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var palette = CreatePalette();
            var spriteMaterial = LoadOrCreateMaterial(SpriteMaterialPath, AssetDatabase.LoadAssetAtPath<Shader>(SpriteShaderPath));
            var playerSprites = CreateSpriteSet("Jugador", PlaceholderSpriteFactory.Player);
            var artisanSprites = CreateSpriteSet("Artesano", PlaceholderSpriteFactory.Artisan);
            var jaguarSprites = CreateSpriteSet("Jaguar", PlaceholderSpriteFactory.JaguarSpirit);
            var promptChannel = LoadOrCreateAsset<StringEventChannel>(PromptChannelPath);
            var messageChannel = LoadOrCreateAsset<StringEventChannel>(MessageChannelPath);
            var playerHealthChannel = LoadOrCreateAsset<HealthEventChannel>(PlayerHealthChannelPath);

            var playerPrefab = CreatePlayerPrefab(playerSprites, spriteMaterial, promptChannel, messageChannel, playerHealthChannel);
            var artisanPrefab = CreateArtisanPrefab(artisanSprites, spriteMaterial, messageChannel);
            var jaguarPrefab = CreateJaguarPrefab(jaguarSprites, spriteMaterial);

            ConfigureRenderSettings(CreateSun());
            BuildEnvironment(palette, messageChannel);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = PlayerStart;
            PlaceCharacters(scene, artisanPrefab, jaguarPrefab, player.transform);

            CreateCameraRig(player.transform);
            CreateGlobalVolume();
            CreateHud(promptChannel, messageChannel, playerHealthChannel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Generador] Prototipo olmeca generado en {ScenePath}.");
        }

        // ---------- Assets ----------

        static Palette CreatePalette()
        {
            Material Make(string name, Color color) =>
                LoadOrCreateMaterial($"{EnvironmentMaterialsFolder}/M_{name}.mat", Shader.Find(SimpleLitShaderName), color);

            return new Palette
            {
                Earth = Make("Tierra_Plaza", new Color(0.62f, 0.47f, 0.34f)),
                Clay = Make("Arcilla_Monticulo", new Color(0.66f, 0.38f, 0.26f)),
                Basalt = Make("Basalto", new Color(0.20f, 0.22f, 0.21f)),
                Stone = Make("Piedra_Monumento", new Color(0.46f, 0.47f, 0.42f)),
                Water = Make("Agua_Pantano", new Color(0.16f, 0.32f, 0.30f)),
                Bark = Make("Tronco", new Color(0.33f, 0.24f, 0.17f)),
                Foliage = Make("Follaje", new Color(0.18f, 0.38f, 0.17f)),
            };
        }

        static Material LoadOrCreateMaterial(string path, Shader shader, Color? baseColor = null)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (baseColor.HasValue)
            {
                material.SetColor("_BaseColor", baseColor.Value);
                material.SetFloat("_Smoothness", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        // Hoja de sprites provisional y su DirectionalSpriteSet. El cuadro 0 es la pose quieta y el resto la caminata.
        static DirectionalSpriteSet CreateSpriteSet(string characterName, PlaceholderSpriteFactory.CharacterPalette palette)
        {
            var sprites = PlaceholderSpriteFactory.CreateOrUpdate(
                $"{PlaceholderFolder}/{characterName}_Placeholder.png", characterName, palette, PixelsPerUnit);
            var clips = sprites
                .Select(frames => new DirectionalSpriteSet.DirectionClip(frames[0], frames.Skip(1).ToArray()))
                .ToArray();

            var spriteSet = LoadOrCreateAsset<DirectionalSpriteSet>($"{CharacterDataFolder}/DSS_{characterName}.asset");
            spriteSet.SetClips(clips);
            EditorUtility.SetDirty(spriteSet);
            return spriteSet;
        }

        static int Layer(string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                throw new System.InvalidOperationException($"Falta el layer '{layerName}' en Project Settings > Tags and Layers.");
            return layer;
        }

        // Raíz con CharacterController y un hijo "Sprite" que mira a la cámara. Todos los personajes miden lo mismo.
        static GameObject CreateCharacterRoot(string name, DirectionalSpriteSet spriteSet, Material spriteMaterial,
            out SpriteRenderer spriteRenderer)
        {
            var root = new GameObject(name);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.4f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.7f, 0f);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.03f;
            controller.minMoveDistance = 0f; // con el valor por defecto se pierden pasos pequeños a FPS altos

            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(root.transform, false);

            spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = spriteSet.GetIdle(DirectionalSpriteSet.DirectionCount - 1); // Sur: de frente
            spriteRenderer.sharedMaterial = spriteMaterial;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.On;
            spriteRenderer.receiveShadows = true;

            spriteObject.AddComponent<SpriteBillboard>();
            return root;
        }

        static void AddSpriteAnimator(GameObject root, MonoBehaviour motionSource, SpriteRenderer spriteRenderer, DirectionalSpriteSet spriteSet)
        {
            var animator = root.AddComponent<DirectionalSpriteAnimator>();
            SetReference(animator, "motionSource", motionSource);
            SetReference(animator, "spriteRenderer", spriteRenderer);
            SetReference(animator, "spriteSet", spriteSet);
        }

        static Health AddHealth(GameObject root, int maxHealth, float invulnerabilityTime, SpriteRenderer spriteRenderer,
            HealthEventChannel changedChannel = null)
        {
            var health = root.AddComponent<Health>();
            SetProperty(health, "maxHealth", property => property.intValue = maxHealth);
            SetProperty(health, "invulnerabilityTime", property => property.floatValue = invulnerabilityTime);
            SetReference(health, "changedChannel", changedChannel);

            var flash = root.AddComponent<DamageFlash>();
            SetReference(flash, "health", health);
            SetReference(flash, "spriteRenderer", spriteRenderer);
            return health;
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject CreatePlayerPrefab(DirectionalSpriteSet spriteSet, Material spriteMaterial,
            StringEventChannel promptChannel, StringEventChannel messageChannel, HealthEventChannel healthChannel)
        {
            var root = CreateCharacterRoot("Jugador", spriteSet, spriteMaterial, out var spriteRenderer);
            root.tag = "Player"; // así lo encuentran los enemigos

            var movement = root.AddComponent<PlayerMovement>();
            SetReference(movement, "moveAction", FindInputActionReference(MoveActionPath));
            AddSpriteAnimator(root, movement, spriteRenderer, spriteSet);

            var interactor = root.AddComponent<PlayerInteractor>();
            SetReference(interactor, "interactAction", FindInputActionReference(InteractActionPath));
            SetProperty(interactor, "interactableLayers", property => property.intValue = 1 << Layer(InteractableLayerName));
            SetReference(interactor, "promptChannel", promptChannel);
            SetReference(interactor, "messageChannel", messageChannel);

            AddHealth(root, maxHealth: 5, invulnerabilityTime: 1f, spriteRenderer, healthChannel);
            root.AddComponent<PlayerDamageResponse>();

            var attack = root.AddComponent<PlayerAttack>();
            SetReference(attack, "attackAction", FindInputActionReference(AttackActionPath));
            SetProperty(attack, "targetLayers", property => property.intValue = 1 << Layer(EnemyLayerName));

            return SavePrefab(root, PlayerPrefabPath);
        }

        static GameObject CreateArtisanPrefab(DirectionalSpriteSet spriteSet, Material spriteMaterial, StringEventChannel messageChannel)
        {
            var root = CreateCharacterRoot("Artesano", spriteSet, spriteMaterial, out var spriteRenderer);
            var patrol = root.AddComponent<NpcPatrol>();
            AddSpriteAnimator(root, patrol, spriteRenderer, spriteSet);

            // El trigger de conversación va en un hijo: el CharacterController de la raíz queda en Default.
            var talk = new GameObject("Conversacion") { layer = Layer(InteractableLayerName) };
            talk.transform.SetParent(root.transform, false);
            var trigger = talk.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.7f, 0f);
            trigger.radius = 1.2f;

            var dialogue = talk.AddComponent<DialogueInteractable>();
            SetProperty(dialogue, "speakerName", property => property.stringValue = ArtisanName);
            SetProperty(dialogue, "lines", property =>
            {
                property.arraySize = ArtisanLines.Length;
                for (var i = 0; i < ArtisanLines.Length; i++)
                    property.GetArrayElementAtIndex(i).stringValue = ArtisanLines[i];
            });
            SetReference(dialogue, "messageChannel", messageChannel);
            SetReference(dialogue, "patrol", patrol);

            return SavePrefab(root, ArtisanPrefabPath);
        }

        static GameObject CreateJaguarPrefab(DirectionalSpriteSet spriteSet, Material spriteMaterial)
        {
            var root = CreateCharacterRoot("EspirituJaguar", spriteSet, spriteMaterial, out var spriteRenderer);
            root.layer = Layer(EnemyLayerName); // solo la raíz tiene collider: es lo que golpea PlayerAttack

            AddHealth(root, maxHealth: 3, invulnerabilityTime: 0.2f, spriteRenderer);
            var chaser = root.AddComponent<EnemyChaser>();
            AddSpriteAnimator(root, chaser, spriteRenderer, spriteSet);

            return SavePrefab(root, JaguarPrefabPath);
        }

        static void PlaceCharacters(UnityEngine.SceneManagement.Scene scene, GameObject artisanPrefab, GameObject jaguarPrefab, Transform player)
        {
            var artisan = (GameObject)PrefabUtility.InstantiatePrefab(artisanPrefab, scene);
            artisan.transform.position = ArtisanPatrol[0];
            SetProperty(artisan.GetComponent<NpcPatrol>(), "patrolPoints", property =>
            {
                property.arraySize = ArtisanPatrol.Length;
                for (var i = 0; i < ArtisanPatrol.Length; i++)
                    property.GetArrayElementAtIndex(i).vector3Value = ArtisanPatrol[i];
            });

            for (var i = 0; i < JaguarSpawns.Length; i++)
            {
                var jaguar = (GameObject)PrefabUtility.InstantiatePrefab(jaguarPrefab, scene);
                jaguar.name = $"EspirituJaguar_{i + 1}";
                jaguar.transform.position = JaguarSpawns[i];
                SetReference(jaguar.GetComponent<EnemyChaser>(), "target", player);
            }
        }

        static InputActionReference FindInputActionReference(string actionPath)
        {
            var reference = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && $"{r.action.actionMap.name}/{r.action.name}" == actionPath);

            if (reference == null)
                throw new System.InvalidOperationException($"No se encontró la acción {actionPath} en {InputActionsPath}.");
            return reference;
        }

        // ---------- Iluminación, cámara y volumen ----------

        static Light CreateSun()
        {
            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft; // el URP Asset de cada nivel decide si se suavizan
            sun.shadowStrength = 0.85f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            // Luz desde el fondo y de lado: las sombras caen hacia la cámara y se ven. Con el sol a la espalda
            // de la cámara, la sombra de un sprite vertical queda oculta detrás de él.
            sun.transform.rotation = Quaternion.Euler(50f, 150f, 0f);
            return sun;
        }

        static void ConfigureRenderSettings(Light sun)
        {
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.50f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.21f, 0.16f);

            // Niebla lineal: casi gratis y da profundidad de diorama.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;
        }

        static void CreateCameraRig(Transform player)
        {
            var position = player.position + CameraOffset;
            var rotation = Quaternion.Euler(CameraPitch, 0f, 0f);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetPositionAndRotation(position, rotation);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = CameraFieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FogColor;

            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.None; // el AA difumina el pixel art
            cameraObject.AddComponent<CameraQualityApplier>(); // en Bajo apaga el post-procesado al iniciar

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CinemachineBrain>();

            // Sin componente de rotación: la cámara conserva su inclinación fija y solo sigue la posición.
            var virtualCameraObject = new GameObject("CM_Jugador");
            virtualCameraObject.transform.SetPositionAndRotation(position, rotation);

            var virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = player;
            var lens = virtualCamera.Lens;
            lens.FieldOfView = CameraFieldOfView;
            lens.NearClipPlane = 0.3f;
            lens.FarClipPlane = 150f;
            virtualCamera.Lens = lens;

            var follow = virtualCameraObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = CameraOffset;
            var tracker = follow.TrackerSettings;
            tracker.BindingMode = BindingMode.WorldSpace;
            tracker.PositionDamping = new Vector3(0.4f, 0.4f, 0.4f);
            follow.TrackerSettings = tracker;
        }

        static void CreateGlobalVolume()
        {
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        }

        // ---------- HUD ----------

        internal static void CreateHud(StringEventChannel promptChannel, StringEventChannel messageChannel, HealthEventChannel healthChannel)
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(HudThemePath)
                ?? throw new System.InvalidOperationException($"No se encontró el tema {HudThemePath}.");
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudLayoutPath)
                ?? throw new System.InvalidOperationException($"No se encontró el diseño {HudLayoutPath}.");

            var panelSettings = LoadOrCreateAsset<PanelSettings>(PanelSettingsPath);
            panelSettings.themeStyleSheet = theme;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);

            var hud = new GameObject("HUD");
            var document = hud.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = layout;

            var controller = hud.AddComponent<HudController>();
            SetReference(controller, "promptChannel", promptChannel);
            SetReference(controller, "messageChannel", messageChannel);
            SetReference(controller, "healthChannel", healthChannel);

            var options = hud.AddComponent<QualityOptionsPanel>();
            SetReference(options, "optionsAction", FindInputActionReference(OptionsActionPath));

            // Navegación con teclado o mando en los botones. Usa el mapa UI del asset del proyecto: las acciones
            // por defecto del módulo no son un asset y no se guardarían en la escena.
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            var inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            inputModule.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            inputModule.point = FindInputActionReference("UI/Point");
            inputModule.leftClick = FindInputActionReference("UI/Click");
            inputModule.rightClick = FindInputActionReference("UI/RightClick");
            inputModule.middleClick = FindInputActionReference("UI/MiddleClick");
            inputModule.scrollWheel = FindInputActionReference("UI/ScrollWheel");
            inputModule.move = FindInputActionReference("UI/Navigate");
            inputModule.submit = FindInputActionReference("UI/Submit");
            inputModule.cancel = FindInputActionReference("UI/Cancel");
            inputModule.trackedDevicePosition = FindInputActionReference("UI/TrackedDevicePosition");
            inputModule.trackedDeviceOrientation = FindInputActionReference("UI/TrackedDeviceOrientation");
        }

        // ---------- Escenario ----------
        // Inspirado en La Venta (Tabasco): plaza de tierra en una isla del pantano, montículo de arcilla,
        // recinto de columnas de basalto (Complejo A), cabeza colosal, altar-trono y estelas.
        // No es un plano a escala del sitio.

        static void BuildEnvironment(Palette palette, StringEventChannel messageChannel)
        {
            var root = Group("--- Entorno ---", null);

            var plaza = Group("Plaza", root);
            Primitive(PrimitiveType.Cube, "Suelo", plaza, new Vector3(0f, -0.5f, 0f),
                new Vector3(PlazaHalfSize * 2f, 1f, PlazaHalfSize * 2f), palette.Earth);
            var water = Primitive(PrimitiveType.Plane, "Pantano", plaza, new Vector3(0f, -0.4f, 0f),
                new Vector3(14f, 1f, 14f), palette.Water);
            RemoveCollider(water);

            BuildMound(Group("Monticulo", root), palette);
            BuildColumnEnclosure(Group("RecintoColumnas", root), palette, new Vector3(-7f, 0f, -1f), 8f, 10f, 2.4f);
            BuildAltar(Group("Altar", root), palette, new Vector3(8f, 0f, 0f), messageChannel);
            BuildColossalHead(Group("CabezaColosal", root), palette, new Vector3(3.5f, 0f, -7f), messageChannel);

            var stelae = Group("Estelas", root);
            void Stela(string name, Vector3 basePosition, Vector3 size, string text)
            {
                Primitive(PrimitiveType.Cube, name, stelae, basePosition + Vector3.up * (size.y * 0.5f), size, palette.Stone);
                AddInscription(stelae, $"Inscripcion_{name}", basePosition + Vector3.up, 1.2f, text, messageChannel);
            }

            Stela("Estela_Oeste", new Vector3(-2f, 0f, -4f), new Vector3(1f, 2.4f, 0.35f), WestStelaText);
            Stela("Estela_Este", new Vector3(2f, 0f, -4f), new Vector3(1f, 2.4f, 0.35f), EastStelaText);
            Stela("Estela_Recinto", new Vector3(-7f, 0f, 1.5f), new Vector3(0.9f, 2.2f, 0.3f), EnclosureStelaText);

            BuildVegetation(Group("Vegetacion", root), palette);
            BuildBounds(Group("Limites", root));
        }

        // Montículo de tierra escalonado (en La Venta es de arcilla, no de piedra).
        static void BuildMound(Transform parent, Palette palette)
        {
            float[] diameters = { 14f, 10f, 6f };
            for (var i = 0; i < diameters.Length; i++)
            {
                var tier = Primitive(PrimitiveType.Cylinder, $"Nivel_{i + 1}", parent, new Vector3(0f, 1f + i * 2f, 11f),
                    new Vector3(diameters[i], 1f, diameters[i]), palette.Clay);
                UseMeshCollider(tier);
            }
        }

        // Cerca de columnas de basalto con una entrada al sur. Las columnas no tienen collider:
        // cada lado usa una sola caja, mucho más barata para la física.
        static void BuildColumnEnclosure(Transform parent, Palette palette, Vector3 center, float width, float depth, float gateWidth)
        {
            const float spacing = 0.5f;
            const float colliderHeight = 2.6f;
            var index = 0;

            void Column(float x, float z)
            {
                var height = 2f + 0.6f * Hash01(index++);
                var column = Primitive(PrimitiveType.Cylinder, $"Columna_{index:00}", parent,
                    new Vector3(x, height * 0.5f, z), new Vector3(0.35f, height * 0.5f, 0.35f), palette.Basalt);
                RemoveCollider(column);
            }

            float minX = center.x - width * 0.5f, maxX = center.x + width * 0.5f;
            float minZ = center.z - depth * 0.5f, maxZ = center.z + depth * 0.5f;

            for (var x = minX; x <= maxX + 0.01f; x += spacing)
            {
                Column(x, maxZ);
                if (Mathf.Abs(x - center.x) > gateWidth * 0.5f)
                    Column(x, minZ);
            }

            for (var z = minZ + spacing; z < maxZ - 0.01f; z += spacing)
            {
                Column(minX, z);
                Column(maxX, z);
            }

            var sideLength = (width - gateWidth) * 0.5f;
            Wall("Muro_Norte", parent, new Vector3(center.x, 0f, maxZ), new Vector3(width, colliderHeight, 0.5f));
            Wall("Muro_Oeste", parent, new Vector3(minX, 0f, center.z), new Vector3(0.5f, colliderHeight, depth));
            Wall("Muro_Este", parent, new Vector3(maxX, 0f, center.z), new Vector3(0.5f, colliderHeight, depth));
            Wall("Muro_SurOeste", parent, new Vector3(minX + sideLength * 0.5f, 0f, minZ), new Vector3(sideLength, colliderHeight, 0.5f));
            Wall("Muro_SurEste", parent, new Vector3(maxX - sideLength * 0.5f, 0f, minZ), new Vector3(sideLength, colliderHeight, 0.5f));
        }

        // Altar-trono con nicho frontal, al estilo del Altar 4 de La Venta.
        static void BuildAltar(Transform parent, Palette palette, Vector3 position, StringEventChannel messageChannel)
        {
            Primitive(PrimitiveType.Cube, "Cuerpo", parent, position + new Vector3(0f, 0.8f, 0f), new Vector3(3.4f, 1.6f, 2.2f), palette.Stone);
            Primitive(PrimitiveType.Cube, "Cornisa", parent, position + new Vector3(0f, 1.75f, 0f), new Vector3(3.8f, 0.3f, 2.6f), palette.Stone);
            var niche = Primitive(PrimitiveType.Cube, "Nicho", parent, position + new Vector3(0f, 0.6f, -1.12f), new Vector3(1.2f, 0.9f, 0.1f), palette.Basalt);
            RemoveCollider(niche);
            AddInscription(parent, "Inscripcion", position + Vector3.up, 2.4f, AltarText, messageChannel);
        }

        static void BuildColossalHead(Transform parent, Palette palette, Vector3 position, StringEventChannel messageChannel)
        {
            Primitive(PrimitiveType.Cube, "Base", parent, position + new Vector3(0f, 0.15f, 0f), new Vector3(2.6f, 0.3f, 2.4f), palette.Basalt);
            Primitive(PrimitiveType.Sphere, "Rostro", parent, position + new Vector3(0f, 1.6f, 0f), new Vector3(2.2f, 2.6f, 2f), palette.Stone);
            var helmet = Primitive(PrimitiveType.Sphere, "Casco", parent, position + new Vector3(0f, 2.35f, 0.05f), new Vector3(2.35f, 1.4f, 2.15f), palette.Stone);
            RemoveCollider(helmet);
            AddInscription(parent, "Inscripcion", position + Vector3.up, 2.2f, ColossalHeadText, messageChannel);
        }

        // Trigger de interacción: no bloquea al CharacterController y solo lo encuentra PlayerInteractor.
        static void AddInscription(Transform parent, string name, Vector3 position, float radius, string text, StringEventChannel messageChannel)
        {
            var inscriptionObject = new GameObject(name) { layer = Layer(InteractableLayerName) };
            inscriptionObject.transform.SetParent(parent, false);
            inscriptionObject.transform.localPosition = position;
            var trigger = inscriptionObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;

            var inscription = inscriptionObject.AddComponent<InscriptionInteractable>();
            SetProperty(inscription, "text", property => property.stringValue = text);
            SetReference(inscription, "messageChannel", messageChannel);
        }

        static void BuildVegetation(Transform parent, Palette palette)
        {
            Vector2[] positions =
            {
                new(-14f, -14f), new(14f, -13f), new(-15f, 6f), new(15f, 8f),
                new(-12f, 15f), new(13f, 15f), new(16f, -3f), new(-16f, -4f),
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var basePosition = new Vector3(positions[i].x, 0f, positions[i].y);
                var scale = 0.85f + 0.3f * Hash01(100 + i);
                Primitive(PrimitiveType.Cylinder, $"Tronco_{i + 1}", parent, basePosition + new Vector3(0f, 1.5f * scale, 0f),
                    new Vector3(0.4f, 1.5f * scale, 0.4f), palette.Bark);
                var canopy = Primitive(PrimitiveType.Sphere, $"Copa_{i + 1}", parent, basePosition + new Vector3(0f, 3.6f * scale, 0f),
                    new Vector3(3f, 2.4f, 3f) * scale, palette.Foliage);
                RemoveCollider(canopy);
            }
        }

        static void BuildBounds(Transform parent)
        {
            const float height = 3f, thickness = 1f;
            var span = PlazaHalfSize * 2f + thickness * 2f;
            var edge = PlazaHalfSize + thickness * 0.5f;
            Wall("Limite_Norte", parent, new Vector3(0f, 0f, edge), new Vector3(span, height, thickness));
            Wall("Limite_Sur", parent, new Vector3(0f, 0f, -edge), new Vector3(span, height, thickness));
            Wall("Limite_Este", parent, new Vector3(edge, 0f, 0f), new Vector3(thickness, height, span));
            Wall("Limite_Oeste", parent, new Vector3(-edge, 0f, 0f), new Vector3(thickness, height, span));
        }

        // ---------- Utilidades ----------

        static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(group, StaticEditorFlags.BatchingStatic);
            return group.transform;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(primitive, StaticEditorFlags.BatchingStatic);
            return primitive;
        }

        // Collider invisible con la base en el suelo.
        static void Wall(string name, Transform parent, Vector3 basePosition, Vector3 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = basePosition + Vector3.up * (size.y * 0.5f);
            wall.AddComponent<BoxCollider>().size = size;
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.BatchingStatic);
        }

        static void RemoveCollider(GameObject gameObject) => Object.DestroyImmediate(gameObject.GetComponent<Collider>());

        // El cilindro primitivo trae una cápsula, que no sirve para piezas anchas y bajas.
        static void UseMeshCollider(GameObject gameObject)
        {
            RemoveCollider(gameObject);
            gameObject.AddComponent<MeshCollider>().sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        // Valor pseudoaleatorio estable en [0, 1): la escena sale igual cada vez que se genera.
        static float Hash01(int seed)
        {
            var value = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        static void SetReference(Object target, string propertyName, Object value) =>
            SetProperty(target, propertyName, property => property.objectReferenceValue = value);

        static void SetProperty(Object target, string propertyName, System.Action<SerializedProperty> assign)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName)
                ?? throw new System.InvalidOperationException($"{target.GetType().Name} no tiene el campo '{propertyName}'.");
            assign(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
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
