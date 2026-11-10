using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Data.Rewards;
using CronicasDeMexico.Editor.PixelArt;
using CronicasDeMexico.Gameplay.Dungeon;
using CronicasDeMexico.World.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static CronicasDeMexico.Editor.SceneGeneration.PixelArtSceneBuilder;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Piezas de la lámina «Mazmorras ancestrales» (D1 en generar_olmeca.py, D2 aquí) listas para los pisos: trampas de
    /// pinchos, placas de presión, cercas de estacas, vasijas rompibles, braseros y cristales animados y adornos. Las
    /// piezas planas de suelo (pinchos, placa) van con el material de los avisos: encima del suelo y debajo de los
    /// personajes, para que no les tapen los pies. Los braseros y cristales brillan con sus propios cuadros
    /// (SpriteFrameCycler), sin luces en tiempo real: lo pide el nivel Bajo y Android (Docs/Rendimiento.md).
    /// </summary>
    public static class DungeonPropAssets
    {
        static Material Floor() => AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.AlertMaterialPath);
        static Material Upright() => AssetDatabase.LoadAssetAtPath<Material>(PixelArtAssets.ObjectMaterialPath);

        static SpriteRenderer Sprite(string name, Transform parent, Dictionary<string, Sprite> sprites, string sprite, Vector2 point, Material material)
        {
            var renderer = SpriteObject(name, parent, Require(sprites, sprite), material,
                IsometricProjection.SnapToPixelGrid(ToWorld(point), 1f / IsometricProjection.PixelsPerUnit));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return renderer;
        }

        static BoxCollider Box(GameObject owner, Vector3 size, bool trigger)
        {
            var box = owner.AddComponent<BoxCollider>();
            box.isTrigger = trigger;
            box.size = size;
            box.center = new Vector3(0f, size.y * 0.5f, 0f);
            return box;
        }

        static GameObject Root(string name, Transform parent, Vector2 point)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = ToWorld(point);
            return root;
        }

        /// <summary>Pinchos de 1 × 1 m: periódicos (con desfase) o para una placa de presión.</summary>
        public static SpikeTrap Spikes(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point, bool periodic, float offset)
        {
            var root = Root("Trampa_Pinchos", parent, point);
            Box(root, new Vector3(0.9f, 0.6f, 0.9f), trigger: true);
            var renderer = Sprite("Sprite", root.transform, sprites, "Trampa_Pinchos_01", point, Floor());
            var trap = root.AddComponent<SpikeTrap>();
            trap.Configure(renderer, Require(sprites, "Trampa_Pinchos_01"), Require(sprites, "Trampa_Pinchos_02"), periodic, offset);
            return trap;
        }

        public static PressurePlate Plate(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point, SpikeTrap[] traps)
        {
            var root = Root("Placa_Presion", parent, point);
            Box(root, new Vector3(0.7f, 0.4f, 0.7f), trigger: true);
            var renderer = Sprite("Sprite", root.transform, sprites, "Placa_Presion_01", point, Floor());
            var plate = root.AddComponent<PressurePlate>();
            plate.Configure(renderer, Require(sprites, "Placa_Presion_01"), Require(sprites, "Placa_Presion_02"), traps);
            return plate;
        }

        /// <summary>Cerca de 1,6 m a lo ancho de la pantalla: bloquea y daña al tocarla.</summary>
        public static GameObject Fence(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point)
        {
            var root = Root("Cerca_Estacas", parent, point);
            Box(root, new Vector3(0.7f, 1f, 2f), trigger: true); // x del mundo = y del flujo: a lo ancho, algo mayor que la sólida
            root.AddComponent<StakeFence>();
            var solid = new GameObject("Solido");
            solid.transform.SetParent(root.transform, false);
            Box(solid, new Vector3(0.3f, 1f, 1.6f), trigger: false);
            Sprite("Sprite", root.transform, sprites, "Cerca_Estacas_01", point, Upright());
            return root;
        }

        /// <summary>Vasija rompible: 1 de vida en el layer Enemigo, con su botín.</summary>
        public static GameObject Pot(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point, LootTable loot, int zoneLevel)
        {
            var root = Root("Vasija", parent, point);
            root.layer = OlmecaPrototypeGenerator.Layer(OlmecaPrototypeGenerator.EnemyLayerName);
            Box(root, new Vector3(0.4f, 0.5f, 0.4f), trigger: false);
            var renderer = Sprite("Sprite", root.transform, sprites, "Vasija_Rompible_01", point, Upright());
            OlmecaPrototypeGenerator.AddHealth(root, maxHealth: 1, invulnerabilityTime: 0f, renderer);
            root.AddComponent<BreakablePot>().Configure(renderer, Require(sprites, "Vasija_Rompible_02"));
            if (loot != null)
                OlmecaRewardAssets.AddLoot(root, loot, zoneLevel);
            return root;
        }

        /// <summary>Brasero (3 cuadros de fuego) o cristal de memoria (2 de brillo), animados sin luces.</summary>
        public static GameObject Animated(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point, string type, int frames,
            float framesPerSecond, Vector3 solid)
        {
            var root = Root(type, parent, point);
            if (solid != Vector3.zero)
                Box(root, solid, trigger: false);
            var renderer = Sprite("Sprite", root.transform, sprites, $"{type}_01", point, Upright());
            var cycler = root.AddComponent<SpriteFrameCycler>();
            cycler.Configure(Enumerable.Range(1, frames).Select(i => Require(sprites, $"{type}_{i:00}")).ToArray(), new[] { renderer });
            SetProperty(cycler, "framesPerSecond", property => property.floatValue = framesPerSecond);
            return root;
        }

        public static GameObject Brazier(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point) =>
            Animated(parent, sprites, point, "Brasero_Piedra", 3, 6f, new Vector3(0.4f, 1f, 0.4f));

        public static GameObject Crystal(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point) =>
            Animated(parent, sprites, point, "Cristal_Memoria", 2, 1.5f, new Vector3(0.4f, 0.8f, 0.4f));

        /// <summary>Adorno quieto (cabeza de piedra, estera, estrado), con collider si estorba.</summary>
        public static GameObject Decor(Transform parent, Dictionary<string, Sprite> sprites, Vector2 point, string sprite, Vector3 solid)
        {
            var root = Root(sprite, parent, point);
            if (solid != Vector3.zero)
                Box(root, solid, trigger: false);
            Sprite("Sprite", root.transform, sprites, sprite, point, Upright());
            return root;
        }
    }
}
