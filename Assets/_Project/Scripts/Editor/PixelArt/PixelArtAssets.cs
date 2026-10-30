using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace CronicasDeMexico.Editor.PixelArt
{
    /// <summary>
    /// Crea o actualiza los materiales y el Sprite Atlas de los sprites pixel art del capítulo olmeca.
    /// Desde consola (con el proyecto cerrado en el Editor): Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// CronicasDeMexico.Editor.PixelArt.PixelArtAssets.CreateAll
    /// </summary>
    public static class PixelArtAssets
    {
        public const string ShaderPath = "Assets/_Project/Art/Shaders/PixelArt_Unlit.shader";
        public const string MaterialsFolder = "Assets/_Project/Art/Environment/Olmeca/Materials/PixelArt";
        public const string ObjectMaterialPath = MaterialsFolder + "/M_PixelArt_Objeto.mat";
        public const string GroundMaterialPath = MaterialsFolder + "/M_PixelArt_Suelo.mat";
        public const string WaterMaterialPath = MaterialsFolder + "/M_PixelArt_Agua.mat";
        /// <summary>Avisos de ataque en el suelo (TelegraphMarker).</summary>
        public const string AlertMaterialPath = MaterialsFolder + "/M_PixelArt_Aviso.mat";
        public const string AtlasPath = PixelArtSpriteImporter.SpritesFolder + "/Atlas_Olmeca.spriteatlasv2";

        // Orden de dibujo: agua animada, suelo horneado encima (lo transparente deja ver el agua), los avisos de ataque
        // sobre el suelo y, al final, los objetos, que se ordenan entre sí por profundidad. Todas caen en la pasada opaca.
        public const int WaterQueue = 1900;
        public const int GroundQueue = 1950;
        public const int AlertQueue = 1960;
        public const int ObjectQueue = 2450;

        [MenuItem("Crónicas/Pixel Art/Crear materiales y atlas")]
        public static void CreateAll()
        {
            CreateMaterials();
            CreateAtlas();
            AssetDatabase.SaveAssets();
            Debug.Log("[PixelArt] Materiales y atlas listos.");
        }

        public static void CreateMaterials()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"[PixelArt] No se encontró {ShaderPath}");
                return;
            }

            EnsureFolder(MaterialsFolder);
            CreateMaterial(ObjectMaterialPath, shader, zWrite: true, ObjectQueue);
            CreateMaterial(GroundMaterialPath, shader, zWrite: false, GroundQueue);
            CreateMaterial(WaterMaterialPath, shader, zWrite: false, WaterQueue);
            CreateMaterial(AlertMaterialPath, shader, zWrite: false, AlertQueue);
        }

        static void CreateMaterial(string path, Shader shader, bool zWrite, int queue)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetFloat("_ZWrite", zWrite ? 1f : 0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.renderQueue = queue;
            // Sin instancing: un SpriteRenderer instanciado comparte la malla de otro sprite de distinto tamaño.
            material.enableInstancing = false;
            EditorUtility.SetDirty(material);
        }

        /// <summary>Atlas de objetos y agua (el suelo horneado va en sus propias texturas).</summary>
        public static void CreateAtlas()
        {
            // Antes de importar el atlas: sin el empaquetador V2 activo no se genera y cada sprite usa su textura.
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            if (AssetDatabase.LoadMainAssetAtPath(AtlasPath) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[]
                {
                    AssetDatabase.LoadAssetAtPath<DefaultAsset>(PixelArtSpriteImporter.SpritesFolder + "/Objetos"),
                    AssetDatabase.LoadAssetAtPath<DefaultAsset>(PixelArtSpriteImporter.SpritesFolder + "/Agua"),
                });
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
            }

            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = false,
                padding = 4,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Point,
                generateMipMaps = false,
                sRGB = true,
                readable = false,
                anisoLevel = 0,
            };
            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 4096;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformSettings(platform);
            importer.includeInBuild = true;
            importer.SaveAndReimport();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
