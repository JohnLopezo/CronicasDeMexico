using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Narrative;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.Gameplay.Rewards;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Enemigos comunes de la lámina "Enemigos y Mazmorra" (Tools/ArtPipeline/recortar_enemigos.py): arma sus
    /// DirectionalSpriteSet (DSS_Enemigo_&lt;Nombre&gt;_&lt;Pose&gt;), crea su EnemyData (ED_&lt;Nombre&gt;) solo si falta, para que
    /// los valores se editen en el Inspector, y monta el enemigo completo para las escenas (Create). La lámina trae una
    /// imagen por pose y solo de lado: las cinco direcciones usan la misma vista y el oeste la voltea el animador.
    /// </summary>
    public static class EnemyCharacterAssets
    {
        const string SpritesFolder = PixelArtSpriteImporter.EnemigosFolder;
        const string DataFolder = "Assets/_Project/Data/Enemies";
        const string SetsFolder = "Assets/_Project/Data/Characters";
        const float InvulnerabilityTime = 0.2f;

        /// <summary>Valores iniciales de cada enemigo (Docs/Enemigos/Olmeca.md). Solo se usan al crear su EnemyData.</summary>
        static readonly Dictionary<string, System.Action<EnemyData>> Defaults = new()
        {
            ["Serpiente"] = d => Set(d, "serpiente", "Serpiente", hp: 25, speed: 2.6f, EnemyMovement.Zigzag,
                EnemyAttackShape.Circle, range: 1.1f, size: 0.8f, reach: 0f, windup: 0.4f, recover: 0.5f, cooldown: 1.3f, damage: 8,
                EnemyDefense.GuardAfterHit, factor: 0.5f, seconds: 1.2f, defenseCooldown: 3f)
                .With(EnemySpecial.SlowSpit, range: 4f, radius: 0.45f, windup: 0.5f, cooldown: 5f, damage: 4, slow: 0.5f, slowSeconds: 2f),
            ["Cocodrilo"] = d => Set(d, "cocodrilo", "Cocodrilo", hp: 60, speed: 1.6f, EnemyMovement.Ambush,
                EnemyAttackShape.Line, range: 2.8f, size: 1.0f, reach: 2.5f, windup: 0.7f, recover: 1.0f, cooldown: 2.2f, damage: 14,
                EnemyDefense.FrontArmor, factor: 0.5f, seconds: 0f, defenseCooldown: 0f, body: 0.4f)
                .With(EnemySpecial.TailSweep, range: 1.6f, radius: 1.8f, windup: 0.5f, cooldown: 4f, damage: 10),
            ["EspirituJaguar"] = d => Set(d, "espiritu_jaguar", "Espíritu jaguar", hp: 35, speed: 3.0f, EnemyMovement.Chase,
                EnemyAttackShape.Line, range: 3.0f, size: 0.9f, reach: 3.2f, windup: 0.6f, recover: 0.7f, cooldown: 2.0f, damage: 12,
                EnemyDefense.Phase, factor: 1f, seconds: 1.0f, defenseCooldown: 4f)
                .With(EnemySpecial.DoubleSwipe, range: 1.3f, radius: 0.9f, windup: 0.35f, cooldown: 4f, damage: 7),
            ["Mono"] = d => Set(d, "mono", "Mono agresivo", hp: 22, speed: 3.0f, EnemyMovement.KeepDistance,
                EnemyAttackShape.Projectile, range: 6f, size: 0.45f, reach: 7f, windup: 0.5f, recover: 0.4f, cooldown: 1.6f, damage: 6,
                EnemyDefense.LeapBack, factor: 1f, seconds: 0f, defenseCooldown: 2.5f)
                .With(EnemySpecial.FanThrow, range: 6f, radius: 0.45f, windup: 0.6f, cooldown: 5f, damage: 5),
            ["Avispa"] = d => Set(d, "avispa", "Avispa gigante", hp: 12, speed: 3.2f, EnemyMovement.Erratic,
                EnemyAttackShape.Line, range: 2.5f, size: 0.6f, reach: 2.2f, windup: 0.45f, recover: 0.6f, cooldown: 1.8f, damage: 6,
                EnemyDefense.Dodge, factor: 1f, seconds: 0f, defenseCooldown: 2f, body: 0.22f, hover: 0.9f)
                .Swarm(radius: 4f),
            ["Guacamaya"] = d => Set(d, "guacamaya", "Guacamaya hostil", hp: 20, speed: 3.4f, EnemyMovement.Chase,
                EnemyAttackShape.Line, range: 3.2f, size: 0.8f, reach: 3f, windup: 0.7f, recover: 0.8f, cooldown: 2.4f, damage: 9,
                EnemyDefense.FlyHigh, factor: 1f, seconds: 1.5f, defenseCooldown: 4f, hover: 1.2f)
                .With(EnemySpecial.SlowScreech, range: 2.8f, radius: 3f, windup: 0.7f, cooldown: 6f, damage: 3, slow: 0.5f, slowSeconds: 2f),
            ["Murcielago"] = d => Set(d, "murcielago", "Murciélago vampiro", hp: 10, speed: 3.6f, EnemyMovement.Erratic,
                EnemyAttackShape.Circle, range: 0.9f, size: 0.6f, reach: 0f, windup: 0.3f, recover: 0.5f, cooldown: 1.4f, damage: 5,
                EnemyDefense.Dodge, factor: 1f, seconds: 0f, defenseCooldown: 1.5f, body: 0.22f, hover: 1.0f)
                .Drains()
                .With(EnemySpecial.SlowScreech, range: 2f, radius: 2.2f, windup: 0.5f, cooldown: 6f, damage: 2, slow: 0.6f, slowSeconds: 1.5f),
        };

        public static IEnumerable<string> Names => Defaults.Keys;

        static EnemyData Set(EnemyData d, string id, string name, int hp, float speed, EnemyMovement movement, EnemyAttackShape shape,
            float range, float size, float reach, float windup, float recover, float cooldown, int damage, EnemyDefense defense,
            float factor, float seconds, float defenseCooldown, float body = 0.3f, float hover = 0f)
        {
            d.id = id;
            d.displayName = name;
            d.maxHealth = hp;
            d.moveSpeed = speed;
            d.movement = movement;
            d.attackShape = shape;
            d.attackRange = range;
            d.attackSize = size;
            d.attackReach = reach;
            d.attackWindup = windup;
            d.attackRecover = recover;
            d.attackCooldown = cooldown;
            d.attackDamage = damage;
            d.defense = defense;
            d.defenseFactor = factor;
            d.defenseSeconds = seconds;
            d.defenseCooldown = defenseCooldown;
            d.bodyRadius = body;
            d.flies = hover > 0f;
            d.hoverHeight = hover;
            return d;
        }

        static EnemyData With(this EnemyData d, EnemySpecial special, float range, float radius, float windup, float cooldown, int damage,
            float slow = 1f, float slowSeconds = 0f)
        {
            d.special = special;
            d.specialRange = range;
            d.specialRadius = radius;
            d.specialWindup = windup;
            d.specialCooldown = cooldown;
            d.specialDamage = damage;
            d.slowFactor = slow;
            d.slowSeconds = slowSeconds;
            return d;
        }

        static EnemyData Swarm(this EnemyData d, float radius)
        {
            d.swarm = true;
            d.swarmRadius = radius;
            return d;
        }

        static EnemyData Drains(this EnemyData d)
        {
            d.attackDrains = true;
            return d;
        }

        static bool Flies(string name) => SpriteFor(name, "Volar") != null;

        static Sprite SpriteFor(string name, string pose) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/Enemigo_{name}_{pose}1_03.png");

        /// <summary>Si recortar_enemigos.py ya escribió los sprites.</summary>
        public static bool SpritesExist() => Names.All(name => SpriteFor(name, "Reposo") != null);

        /// <summary>
        /// EnemyData del enemigo con sus animaciones al día. Los valores de diseño solo se escriben al crearlo.
        /// </summary>
        public static EnemyData Build(string name)
        {
            if (!Defaults.TryGetValue(name, out var defaults))
                throw new System.ArgumentException($"Enemigo desconocido: {name}");
            Sprite Get(string pose) => SpriteFor(name, pose)
                ?? throw new System.InvalidOperationException($"Falta Enemigo_{name}_{pose}1_03 en {SpritesFolder}: ejecuta recortar_enemigos.py.");

            var idle = Get("Reposo");
            DirectionalSpriteSet Sprites(string anim, float fps, params string[] poses)
            {
                var frames = poses.Select(Get).ToArray();
                var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{SetsFolder}/DSS_Enemigo_{name}_{anim}.asset");
                set.SetClips(Enumerable.Range(0, DirectionalSpriteSet.DirectionCount)
                    .Select(_ => new DirectionalSpriteSet.DirectionClip(idle, frames)).ToArray());
                set.FramesPerSecond = fps;
                EditorUtility.SetDirty(set);
                return set;
            }

            EnsureFolder(DataFolder);
            var path = $"{DataFolder}/ED_{name}.asset";
            var isNew = AssetDatabase.LoadAssetAtPath<EnemyData>(path) == null;
            var data = OlmecaPrototypeGenerator.LoadOrCreateAsset<EnemyData>(path);
            if (isNew)
                defaults(data);

            // Una imagen por pose: caminar alterna reposo y paso; volar, alas arriba y abajo.
            var flies = Flies(name);
            data.walk = flies ? Sprites("Volar", 8f, "Reposo", "Volar") : Sprites("Caminar", 4f, "Reposo", "Caminar");
            data.run = flies ? Sprites("Picada", 1f, "Ataque") : Sprites("Correr", 8f, "Caminar", "Correr");
            data.attack = Sprites("Ataque", 1f, "Ataque");
            data.guard = Sprites("Defensa", 1f, "Defensa");
            data.hit = Sprites("Dano", 2.5f, "Dano"); // 0,4 s
            data.death = Sprites("Muerte", 2.5f, "Dano", "Muerte");
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Rehace los sprites y los datos de los siete sin regenerar escenas.
        /// -executeMethod CronicasDeMexico.Editor.SceneGeneration.EnemyCharacterAssets.BuildAll</summary>
        public static Dictionary<string, EnemyData> BuildAll()
        {
            var all = Names.ToDictionary(name => name, Build);
            AssetDatabase.SaveAssets();
            return all;
        }

        /// <summary>
        /// Enemigo listo para una escena de pixel art: cápsula a su medida en el layer Enemigo, vida, WildEnemy con su
        /// aviso o sus proyectiles, animación, botín y aviso de derrota (vencer:&lt;id&gt;). En la mazmorra no reaparece.
        /// </summary>
        public static GameObject Create(EnemyData data, Transform parent, Vector3 position, bool respawns)
        {
            var root = OlmecaPrototypeGenerator.CreateCharacterRoot($"Enemigo_{data.id}", data.walk, CharacterMaterial(), out var spriteRenderer);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.EnemyLayerName); // lo que golpea PlayerAttack
            var controller = root.GetComponent<CharacterController>();
            controller.radius = data.bodyRadius;
            controller.height = 0.9f;
            controller.center = new Vector3(0f, 0.45f, 0f);

            // Como el jaguar corrupto: sprites a la resolución del escenario, sin escalar la raíz.
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            SetProperty(spriteRenderer.GetComponent<SpriteBillboard>(), "mode",
                property => property.enumValueIndex = (int)SpriteBillboard.BillboardMode.CameraPlane);
            spriteRenderer.transform.rotation = IsometricProjection.CameraRotation;
            spriteRenderer.gameObject.AddComponent<PixelGridSnap>();

            Transform heightRoot = null;
            if (data.flies)
            {
                // PixelGridSnap pone el sprite en la posición de su padre: el que sube es este intermedio.
                heightRoot = new GameObject("Altura").transform;
                heightRoot.SetParent(root.transform, false);
                heightRoot.localPosition = Vector3.up * data.hoverHeight;
                spriteRenderer.transform.SetParent(heightRoot, true);
            }

            OlmecaPrototypeGenerator.AddHealth(root, data.maxHealth, InvulnerabilityTime, spriteRenderer);
            var enemy = root.AddComponent<WildEnemy>();
            OlmecaPrototypeGenerator.AddSpriteAnimator(root, enemy, spriteRenderer, data.walk);
            SetProperty(enemy, "data", property => property.objectReferenceValue = data);
            SetProperty(enemy, "respawns", property => property.boolValue = respawns);
            SetProperty(enemy, "spriteRenderer", property => property.objectReferenceValue = spriteRenderer);
            SetProperty(enemy, "spriteAnimator", property => property.objectReferenceValue = root.GetComponent<DirectionalSpriteAnimator>());
            SetProperty(enemy, "heightRoot", property => property.objectReferenceValue = heightRoot);

            if (data.attackShape == EnemyAttackShape.Projectile || data.special is EnemySpecial.SlowSpit or EnemySpecial.FanThrow)
            {
                var stone = EffectSpriteFactory.SymbolShard(); // shortcut: el fragmento del eco como piedra; dibujar una propia
                var material = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);
                var projectiles = Enumerable.Range(1, 3).Select(i =>
                {
                    var shot = SpriteObject($"Proyectil_{i}", root.transform, stone, material, position);
                    shot.gameObject.SetActive(false);
                    return shot.gameObject.AddComponent<Projectile>();
                }).ToArray();
                SetProperty(enemy, "projectiles", property =>
                {
                    property.arraySize = projectiles.Length;
                    for (var i = 0; i < projectiles.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = projectiles[i];
                });
            }
            if (data.attackShape != EnemyAttackShape.Projectile)
            {
                var circle = data.attackShape == EnemyAttackShape.Circle;
                SetProperty(enemy, "marker", property => property.objectReferenceValue = Marker(root.transform,
                    circle ? EffectSpriteFactory.TelegraphCircle() : EffectSpriteFactory.TelegraphLine(),
                    circle ? TelegraphMarker.MarkerShape.Circle : TelegraphMarker.MarkerShape.Line));
            }

            if (data.special is EnemySpecial.TailSweep or EnemySpecial.DoubleSwipe or EnemySpecial.SlowScreech)
            {
                var ring = data.special == EnemySpecial.SlowScreech;
                SetProperty(enemy, "specialMarker", property => property.objectReferenceValue = Marker(root.transform,
                    ring ? EffectSpriteFactory.TelegraphRing() : EffectSpriteFactory.TelegraphCircle(),
                    ring ? TelegraphMarker.MarkerShape.Ring : TelegraphMarker.MarkerShape.Circle));
            }

            if (data.loot != null)
            {
                var reward = root.AddComponent<EnemyReward>();
                reward.Table = data.loot;
                reward.ZoneLevel = data.zoneLevel;
            }
            root.AddComponent<ReportOnDefeat>().EnemyId = data.id;
            return root;
        }

        /// <summary>
        /// Coloca un enemigo en <paramref name="candidate"/> (coordenadas del flujo) si hay suelo debajo; si no, en
        /// <paramref name="fallback"/>, un punto del flujo que se sabe transitable. Los pisos de la mazmorra son losas
        /// sobre el vacío: un enemigo sin suelo caería. Su botín, con la tabla y el nivel de la zona.
        /// </summary>
        public static GameObject Place(string name, Transform parent, Vector2 candidate, Vector2 fallback, bool respawns,
            LootTable loot, int zoneLevel)
        {
            var position = ToWorld(candidate);
            Physics.SyncTransforms();
            if (!Physics.Raycast(position + Vector3.up * 3f, Vector3.down, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = ToWorld(fallback);
            var enemy = Create(Build(name), parent, position, respawns);
            if (loot != null)
                OlmecaRewardAssets.AddLoot(enemy, loot, zoneLevel);
            return enemy;
        }

        static TelegraphMarker Marker(Transform owner, Sprite sprite, TelegraphMarker.MarkerShape shape)
        {
            var markerObject = new GameObject("Aviso");
            markerObject.transform.SetParent(owner, false);
            var markerRenderer = markerObject.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = sprite;
            markerRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.AlertMaterialPath);
            markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
            markerRenderer.enabled = false; // lo enciende TelegraphMarker
            var marker = markerObject.AddComponent<TelegraphMarker>();
            SetProperty(marker, "shape", property => property.enumValueIndex = (int)shape);
            return marker;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
