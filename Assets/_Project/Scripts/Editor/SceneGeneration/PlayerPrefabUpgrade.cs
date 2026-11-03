using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Añade al prefab del jugador lo de M5 si le falta: PlayerStats (nivel, reliquias y Bendiciones a vida y ataque;
    /// consumibles) y PlayerAbility (golpe de tierra) con su círculo en el suelo. No toca lo que ya tiene, así que se
    /// puede llamar cada vez que se genera una escena.
    /// </summary>
    public static class PlayerPrefabUpgrade
    {
        const string AbilityVisualName = "GolpeTierra";
        static readonly Color EarthColor = new(0.85f, 0.62f, 0.3f, 1f);

        public static void Ensure()
        {
            var path = OlmecaPrototypeGenerator.PlayerPrefabPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var changed = false;
                if (root.GetComponent<PlayerStats>() == null)
                {
                    root.AddComponent<PlayerStats>();
                    changed = true;
                }

                if (root.GetComponent<PlayerAbility>() == null)
                {
                    var ability = root.AddComponent<PlayerAbility>();
                    var visual = root.transform.Find(AbilityVisualName);
                    if (visual == null)
                    {
                        var visualObject = new GameObject(AbilityVisualName);
                        visualObject.transform.SetParent(root.transform, false);
                        var renderer = visualObject.AddComponent<SpriteRenderer>();
                        renderer.sprite = EffectSpriteFactory.TelegraphCircle();
                        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.AlertMaterialPath);
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                        renderer.enabled = false; // lo enciende TelegraphMarker.Show
                        var marker = visualObject.AddComponent<TelegraphMarker>();
                        PixelArtSceneBuilder.SetProperty(marker, "color", property => property.colorValue = EarthColor);
                        visual = visualObject.transform;
                    }

                    PixelArtSceneBuilder.SetProperty(ability, "visual",
                        property => property.objectReferenceValue = visual.GetComponent<TelegraphMarker>());
                    PixelArtSceneBuilder.SetProperty(ability, "messageChannel", property => property.objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<StringEventChannel>(OlmecaPrototypeGenerator.MessageChannelPath));
                    changed = true;
                }

                // Etapa A del plan de personajes: el jugador es el jaguar (sprites del flujo, sin escalar la raíz).
                if (JaguarCharacterAssets.SpritesExist())
                {
                    ApplyJaguar(root);
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ApplyJaguar(GameObject root)
        {
            var sets = JaguarCharacterAssets.Player();
            var animator = root.GetComponent<DirectionalSpriteAnimator>();
            animator.SpriteSet = sets.Walk;
            animator.IdleSet = sets.Idle;
            var spriteRenderer = root.GetComponentInChildren<SpriteBillboard>().GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sets.Idle.GetIdle(DirectionalSpriteSet.DirectionCount - 1);
            spriteRenderer.sharedMaterial = PixelArtSceneBuilder.CharacterMaterial();
            JaguarCharacterAssets.ConfigureController(root.GetComponent<CharacterController>());

            var driver = root.GetComponent<CharacterAnimationDriver>() ?? root.AddComponent<CharacterAnimationDriver>();
            driver.Configure(animator, sets["Zarpazo"], sets["Correr"], null, null, sets["Golpeado"], sets["Muerte"]);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(driver);
        }
    }
}
