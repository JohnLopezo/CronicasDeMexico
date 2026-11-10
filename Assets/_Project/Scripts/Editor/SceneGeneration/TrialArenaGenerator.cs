using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Enemies;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Arena de pruebas de los jefes de la lámina del autor (Docs/Enemigos/Jefes_Prueba.md). Reutiliza el escenario del
    /// piso 3 (Mazmorra_P3.json) sin su historia: la losa sellada se quita y la arena del Guardián queda abierta. En la
    /// antesala hay seis placas, una por jefe, con una inscripción que lo presenta; al pisar una, ese jefe aparece en el
    /// centro de la arena (BossTrial). No se enlaza con el resto del juego: se abre con su escena. No se edita a mano: se
    /// regenera (menú Crónicas → Generar arena de pruebas de jefes).
    /// </summary>
    public static class TrialArenaGenerator
    {
        public const string SceneName = "Arena_Pruebas_Jefes";
        public const string ScenePath = "Assets/_Project/Scenes/Regions/Pruebas/" + SceneName + ".unity";
        const string SealSprite = "Estela_Grande_01";

        [MenuItem("Crónicas/Generar arena de pruebas de jefes")]
        public static void Generate()
        {
            if (!BossCharacterAssets.SpritesExist() || !EnemyCharacterAssets.SpritesExist())
                throw new System.InvalidOperationException("Faltan los sprites: ejecuta recortar_jefes.py y recortar_enemigos.py.");
            var data = LoadData(OlmecaDungeonGenerator.DataPath(3));
            var points = data.puntos;
            var sprites = LoadSprites();

            OlmecaPrototypeGenerator.EnsureFolder("Assets/_Project/Scenes/Regions/Pruebas");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var messageChannel = MessageChannel();
            var nameChannel = OlmecaPrototypeGenerator.LoadOrCreateAsset<StringEventChannel>(OlmecaPrototypeGenerator.BossNameChannelPath);
            var healthChannel = OlmecaPrototypeGenerator.LoadOrCreateAsset<HealthEventChannel>(OlmecaPrototypeGenerator.BossHealthChannelPath);
            var bosses = BossCharacterAssets.BuildAll();

            SpriteRenderer seal = null;
            var scenery = BuildScenery("--- Escenario (piso 3) ---", data, sprites, (placement, renderer) =>
            {
                if (placement.sprite == SealSprite)
                    seal = renderer;
            });
            // Sin la historia del piso 3: la losa que cierra la arena no está.
            if (seal != null)
                Object.DestroyImmediate(seal.gameObject);
            var obstacle = scenery.Find($"Colisiones/Obstáculos/{SealSprite}");
            if (obstacle != null)
                Object.DestroyImmediate(obstacle.gameObject);

            ConfigureLighting(0.85f, new Color(0.4f, 0.42f, 0.4f));
            var player = PlacePlayer(scene, points.llegada_arriba.Value);

            var arena = Group("--- Jefes ---", null);
            var minions = Group("--- Invocados ---", null);
            var spawn = new GameObject("Centro_Arena").transform;
            spawn.SetParent(arena, false);
            spawn.position = ToWorld(points.guardian.Value);
            var trialObject = new GameObject("Arena_Pruebas");
            trialObject.transform.SetParent(arena, false);
            var trial = trialObject.AddComponent<BossTrial>();
            var created = bosses.Select(boss =>
            {
                var root = BossCharacterAssets.Create(boss, arena, minions, spawn.position, nameChannel, healthChannel);
                root.SetActive(false); // hasta que se pise su placa
                return root.GetComponent<TrialBoss>();
            }).ToArray();
            SetProperty(trial, "bosses", property =>
            {
                property.arraySize = created.Length;
                for (var i = 0; i < created.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = created[i];
            });
            SetProperty(trial, "spawn", property => property.objectReferenceValue = spawn);
            SetProperty(trial, "bossNameChannel", property => property.objectReferenceValue = nameChannel);

            BuildPads(Group("--- Placas ---", null), trial, bosses, points, messageChannel);
            CreateCamera(player.transform, data.limites);
            CreateHud(messageChannel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Generador] Arena de pruebas generada en {ScenePath}: {created.Length} jefes.");
        }

        // Seis placas en fila por la antesala, entre la llegada y el santuario, cada una solo donde hay suelo.
        static void BuildPads(Transform parent, BossTrial trial, IReadOnlyList<BossData> bosses,
            RegionPoints points, StringEventChannel messageChannel)
        {
            var from = points.llegada_arriba.Value + new Vector2(1.5f, 0f);
            var to = points.memoria_llegada.Value;
            var material = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.AlertMaterialPath);
            Physics.SyncTransforms();
            for (var i = 0; i < bosses.Count; i++)
            {
                var point = Vector2.Lerp(from, to, (i + 0.5f) / bosses.Count) + new Vector2(0f, i % 2 == 0 ? -0.5f : 0.5f);
                if (!Physics.Raycast(ToWorld(point) + Vector3.up * 3f, Vector3.down, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    point = Vector2.Lerp(from, to, (i + 0.5f) / bosses.Count);
                var pad = TriggerBox($"Placa_{bosses[i].id}", parent, point, new Vector3(0.9f, 0.9f, 1f));
                var trigger = pad.AddComponent<BossTrialPad>();
                var index = i;
                SetProperty(trigger, "trial", property => property.objectReferenceValue = trial);
                SetProperty(trigger, "index", property => property.intValue = index);

                var mark = SpriteObject($"Marca_{bosses[i].id}", pad.transform, EffectSpriteFactory.TelegraphCircle(), material, ToWorld(point));
                mark.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                mark.transform.localScale = Vector3.one * 0.5f;
                mark.shadowCastingMode = ShadowCastingMode.Off;
                AddInscription(parent, $"Inscripcion_{bosses[i].id}", ToWorld(point) + Vector3.up * 0.8f,
                    $"{bosses[i].displayName}. {bosses[i].inspiration} Eco ficticio de prueba. Pisa la placa para llamarlo a la arena.",
                    messageChannel);
            }
        }
    }
}
