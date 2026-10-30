using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
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

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
