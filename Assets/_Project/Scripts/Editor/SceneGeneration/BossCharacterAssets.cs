using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Enemies;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Jefes de la lámina "Jefes de Mazmorra" (Tools/ArtPipeline/recortar_jefes.py) para la arena de pruebas: arma sus
    /// DirectionalSpriteSet (DSS_Jefe_&lt;Clave&gt;_&lt;Pose&gt;), crea su BossData (BD_&lt;Clave&gt;) solo si falta y monta el jefe
    /// (Create). Son ecos ficticios inspirados en figuras de otras culturas, con nombres provisionales: el autor los fija
    /// al asignarlos a los arcos II–V (Docs/Enemigos/Jefes_Prueba.md).
    /// </summary>
    public static class BossCharacterAssets
    {
        const string SpritesFolder = PixelArtSpriteImporter.JefesFolder;
        const string DataFolder = "Assets/_Project/Data/Enemies";
        const string SetsFolder = "Assets/_Project/Data/Characters";
        const int CircleMarkers = 6;
        const int ProjectilePool = 10;

        static BossAbility A(string name, BossAbilityKind kind, int phase, float range, float radius, float windup, float cooldown,
            int damage, int count = 1, float length = 5f, float spread = 60f, float factor = 1f, float seconds = 2f) => new()
        {
            name = name, kind = kind, minPhase = phase, range = range, radius = radius, windup = windup, cooldown = cooldown,
            damage = damage, count = count, length = length, spread = spread, factor = factor, seconds = seconds,
        };

        // Valores iniciales (escala ×10 del M5: el Guardián de la Piedra Viva tiene 400). Solo se usan al crear su BossData.
        static readonly (string key, string name, string inspiration, int hp, float speed, float body, string summon, float resurrect,
            BossAbility[] abilities)[] Bosses =
        {
            ("SerpienteEmplumada", "Eco de la Serpiente Emplumada", "Inspirado en Quetzalcóatl (culturas del Posclásico; no olmeca).",
                300, 2.2f, 0.7f, null, 0f, new[]
                {
                    A("Suspiro Divino", BossAbilityKind.Beam, 1, 5f, 1.2f, 0.8f, 4f, 10, length: 6f),
                    A("Tormenta de Plumas", BossAbilityKind.Volley, 1, 7f, 0.4f, 0.6f, 5f, 6, count: 7, length: 7f, spread: 70f),
                    A("Luz Sagrada", BossAbilityKind.Nova, 1, 2.5f, 3.2f, 0.9f, 6f, 10),
                    A("Vuelo Majestuoso", BossAbilityKind.Leap, 2, 8f, 1.6f, 1.0f, 7f, 14),
                }),
            ("SenorLluvia", "Eco del Señor de la Lluvia", "Inspirado en Tláloc (Teotihuacan y mexicas; no olmeca).",
                320, 1.8f, 0.6f, null, 0f, new[]
                {
                    A("Lluvia Sagrada", BossAbilityKind.Rain, 1, 9f, 1.2f, 1.0f, 4f, 8, count: 4),
                    A("Rayo Acuático", BossAbilityKind.Beam, 1, 6f, 1.0f, 0.7f, 4f, 10, length: 7f),
                    A("Inundación", BossAbilityKind.Nova, 2, 3.5f, 4f, 1.2f, 8f, 6, factor: 0.5f, seconds: 3f),
                    A("Invocación de Nubes", BossAbilityKind.Rain, 3, 10f, 1.0f, 0.9f, 6f, 8, count: 6),
                }),
            ("MuroCraneos", "Eco del Muro de Cráneos", "Inspirado en el tzompantli, un muro de cráneos mexica: no es un ser, aquí es un eco ficticio.",
                280, 1.6f, 0.7f, "Murcielago", 0.4f, new[]
                {
                    A("Golpe Espectral", BossAbilityKind.Swipe, 1, 2f, 1.4f, 0.6f, 3f, 12),
                    A("Aura de Miedo", BossAbilityKind.Nova, 1, 3f, 3f, 0.8f, 7f, 4, factor: 0.5f, seconds: 2.5f),
                    A("Invocación de Calaveras", BossAbilityKind.Summon, 2, 12f, 0f, 1.0f, 12f, 0, count: 2),
                }),
            ("PerroFuego", "Eco del Perro de Fuego", "Inspirado en Xólotl (mexica; no olmeca).",
                260, 3.2f, 0.6f, null, 0f, new[]
                {
                    A("Furia Canina", BossAbilityKind.Swipe, 1, 1.8f, 1.2f, 0.45f, 3f, 9, count: 2),
                    A("Salto Infernal", BossAbilityKind.Leap, 1, 7f, 1.5f, 0.8f, 5f, 12),
                    A("Llamas Ancestrales", BossAbilityKind.Rain, 1, 8f, 1.1f, 0.9f, 6f, 8, count: 5),
                    A("Aullido Maldito", BossAbilityKind.Nova, 2, 3.5f, 3.5f, 0.8f, 8f, 5, factor: 0.6f, seconds: 2f),
                }),
            ("SenorMuertos", "Eco del Señor de los Muertos", "Inspirado en las deidades mexicas de la muerte (la lámina dice Tlacatecuhtli, que no está documentado).",
                320, 1.6f, 0.6f, "EspirituJaguar", 0f, new[]
                {
                    A("Abrazo Mortal", BossAbilityKind.Swipe, 1, 2.2f, 1.8f, 0.9f, 4f, 14),
                    A("Tormenta Espectral", BossAbilityKind.Volley, 1, 8f, 0.4f, 0.8f, 6f, 5, count: 10, length: 6f, spread: 360f),
                    A("Ritual de Huesos", BossAbilityKind.Shield, 2, 12f, 0f, 0f, 12f, 0, factor: 0f, seconds: 3f),
                    A("Invocación Funeraria", BossAbilityKind.Summon, 2, 12f, 0f, 1.0f, 14f, 0, count: 2),
                }),
            ("GuardianOfrenda", "Eco del Guardián de la Ofrenda", "Inspirado en el chac mool (escultura tolteca y maya; no es una deidad).",
                360, 1.4f, 0.8f, null, 0f, new[]
                {
                    A("Golpe Terrenal", BossAbilityKind.Swipe, 1, 2f, 1.5f, 0.7f, 3f, 12),
                    A("Lanzamiento de Ofrendas", BossAbilityKind.Volley, 1, 7f, 0.45f, 0.6f, 5f, 8, count: 3, length: 7f, spread: 30f),
                    A("Terremoto", BossAbilityKind.Nova, 2, 4f, 4f, 1.2f, 8f, 10),
                    A("Ritual de Protección", BossAbilityKind.Shield, 2, 12f, 0f, 0f, 12f, 0, factor: 0.3f, seconds: 4f),
                }),
        };

        public static IEnumerable<string> Keys => Bosses.Select(b => b.key);

        static Sprite SpriteFor(string key, string pose) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesFolder}/Jefe_{key}_{pose}1_03.png");

        public static bool SpritesExist() => Keys.All(key => SpriteFor(key, "Reposo") != null);

        public static BossData Build(string key)
        {
            var boss = Bosses.First(b => b.key == key);
            Sprite Get(string pose) => SpriteFor(key, pose)
                ?? throw new System.InvalidOperationException($"Falta Jefe_{key}_{pose}1_03 en {SpritesFolder}: ejecuta recortar_jefes.py.");
            var idle = Get("Reposo");
            DirectionalSpriteSet Sprites(string anim, float fps, params string[] poses)
            {
                var frames = poses.Select(Get).ToArray();
                var set = OlmecaPrototypeGenerator.LoadOrCreateAsset<DirectionalSpriteSet>($"{SetsFolder}/DSS_Jefe_{key}_{anim}.asset");
                set.SetClips(Enumerable.Range(0, DirectionalSpriteSet.DirectionCount)
                    .Select(_ => new DirectionalSpriteSet.DirectionClip(idle, frames)).ToArray());
                set.FramesPerSecond = fps;
                EditorUtility.SetDirty(set);
                return set;
            }

            var path = $"{DataFolder}/BD_{key}.asset";
            var isNew = AssetDatabase.LoadAssetAtPath<BossData>(path) == null;
            var data = OlmecaPrototypeGenerator.LoadOrCreateAsset<BossData>(path);
            if (isNew)
            {
                data.id = key;
                data.displayName = boss.name;
                data.inspiration = boss.inspiration;
                data.maxHealth = boss.hp;
                data.moveSpeed = boss.speed;
                data.bodyRadius = boss.body;
                data.resurrectFraction = boss.resurrect;
                data.abilities = boss.abilities;
                data.summon = boss.summon != null ? EnemyCharacterAssets.Build(boss.summon) : null;
            }
            data.walk = Sprites("Avance", 3f, "Reposo", "Avance");
            data.attack = Sprites("Ataque", 1f, "Ataque");
            data.special = Sprites("Especial", 1f, "Especial");
            data.death = Sprites("Muerte", 2.5f, "Caida", "Desplome"); // 0,8 s y se queda tumbado
            EditorUtility.SetDirty(data);
            return data;
        }

        public static List<BossData> BuildAll()
        {
            var all = Keys.Select(Build).ToList();
            AssetDatabase.SaveAssets();
            return all;
        }

        /// <summary>
        /// Jefe listo para la arena: cápsula a su medida en el layer Enemigo, vida con la barra del HUD, TrialBoss con sus
        /// avisos (6 círculos y una franja), proyectiles e invocados (en <paramref name="minionParent"/>, fuera del jefe
        /// para que no lo sigan). Sin botín ni aviso de historia: es una prueba.
        /// </summary>
        public static GameObject Create(BossData data, Transform parent, Transform minionParent, Vector3 position,
            StringEventChannel nameChannel, HealthEventChannel healthChannel)
        {
            var root = OlmecaPrototypeGenerator.CreateCharacterRoot($"Jefe_{data.id}", data.walk, CharacterMaterial(), out var spriteRenderer);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.EnemyLayerName);
            var controller = root.GetComponent<CharacterController>();
            controller.radius = data.bodyRadius;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            SetProperty(spriteRenderer.GetComponent<SpriteBillboard>(), "mode",
                property => property.enumValueIndex = (int)SpriteBillboard.BillboardMode.CameraPlane);
            spriteRenderer.transform.rotation = IsometricProjection.CameraRotation;
            spriteRenderer.gameObject.AddComponent<PixelGridSnap>();

            OlmecaPrototypeGenerator.AddHealth(root, data.maxHealth, 0.2f, spriteRenderer, healthChannel);
            var boss = root.AddComponent<TrialBoss>();
            OlmecaPrototypeGenerator.AddSpriteAnimator(root, boss, spriteRenderer, data.walk);
            SetProperty(boss, "data", property => property.objectReferenceValue = data);
            SetProperty(boss, "spriteRenderer", property => property.objectReferenceValue = spriteRenderer);
            SetProperty(boss, "spriteAnimator", property => property.objectReferenceValue = root.GetComponent<DirectionalSpriteAnimator>());
            SetProperty(boss, "bossNameChannel", property => property.objectReferenceValue = nameChannel);

            var circles = Enumerable.Range(1, CircleMarkers)
                .Select(i => Marker(root.transform, $"Aviso_Circulo_{i}", EffectSpriteFactory.TelegraphCircle(), TelegraphMarker.MarkerShape.Circle))
                .ToArray();
            Assign(boss, "circles", circles);
            SetProperty(boss, "line", property => property.objectReferenceValue =
                Marker(root.transform, "Aviso_Franja", EffectSpriteFactory.TelegraphLine(), TelegraphMarker.MarkerShape.Line));

            var shard = EffectSpriteFactory.SymbolShard(); // shortcut: el fragmento del eco como proyectil; dibujar uno por jefe
            var material = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);
            Assign(boss, "projectiles", Enumerable.Range(1, ProjectilePool).Select(i =>
            {
                var shot = SpriteObject($"Proyectil_{i}", root.transform, shard, material, position);
                shot.gameObject.SetActive(false);
                return shot.gameObject.AddComponent<Projectile>();
            }).ToArray());

            var summons = data.abilities.Where(a => a.kind == BossAbilityKind.Summon).Select(a => a.count).DefaultIfEmpty(0).Max();
            if (data.summon != null && summons > 0)
                Assign(boss, "minions", Enumerable.Range(1, summons).Select(i =>
                {
                    var minion = EnemyCharacterAssets.Create(data.summon, minionParent, position, respawns: false);
                    minion.name = $"{root.name}_Invocado_{i}";
                    minion.SetActive(false); // hasta que lo invoque
                    return minion;
                }).ToArray());
            return root;
        }

        static void Assign(Object target, string property, Object[] values) =>
            SetProperty(target, property, p =>
            {
                p.arraySize = values.Length;
                for (var i = 0; i < values.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });

        static TelegraphMarker Marker(Transform owner, string name, Sprite sprite, TelegraphMarker.MarkerShape shape)
        {
            var markerObject = new GameObject(name);
            markerObject.transform.SetParent(owner, false);
            var markerRenderer = markerObject.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = sprite;
            markerRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.AlertMaterialPath);
            markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
            markerRenderer.enabled = false;
            var marker = markerObject.AddComponent<TelegraphMarker>();
            SetProperty(marker, "shape", property => property.enumValueIndex = (int)shape);
            return marker;
        }
    }
}
