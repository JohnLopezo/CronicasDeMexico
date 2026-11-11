using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Data.Narrative;
using UnityEditor;
using UnityEngine;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Crea el catálogo del Códice (Resources/Narrativa/CatalogoCodice) con las páginas del arco olmeca, solo si falta:
    /// después se edita en el Inspector. Para volver a estos textos, se borra el asset.
    /// Menú Crónicas → Crear catálogo del Códice, o -executeMethod CronicasDeMexico.Editor.SceneGeneration.CodexAssets.Ensure.
    /// </summary>
    public static class CodexAssets
    {
        const string Path = "Assets/_Project/Resources/" + CodexCatalog.ResourcePath + ".asset";

        [MenuItem("Crónicas/Crear catálogo del Códice")]
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<CodexCatalog>(Path) != null)
            {
                Debug.Log($"[{nameof(CodexAssets)}] {Path} ya existe: no se toca.");
                return;
            }

            var catalog = ScriptableObject.CreateInstance<CodexCatalog>();
            catalog.SetPages(OlmecaPages());
            AssetDatabase.CreateAsset(catalog, Path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(CodexAssets)}] Creado {Path}.");
        }

        // Docs/Sistemas/Codice.md. Mecánicas: ficción del juego. Culturales: separar documentado e interpretado.
        public static CodexCatalog.Page[] OlmecaPages() => new[]
        {
            new CodexCatalog.Page("ruptura", "Escudos y afinidades",
                "Cada criatura del olvido lleva un escudo de rombos y teme ciertas afinidades. Golpéala con lo que teme y el " +
                "escudo se rompe: queda indefensa un momento y recibe más daño. El agua refleja lo que eres: tu afinidad es tu arma.",
                "El agua del río muestra más de lo que parece.", "examinar:reflejo", Origin.Ficticio),
            new CodexCatalog.Page("combo", "El golpe que se encadena",
                "Tres golpes seguidos, sin perder el ritmo, terminan en un remate que pega más fuerte. Esquivar rompe la cadena. " +
                "Tus golpes buscan solos a la criatura más cercana que tengas delante.",
                "Se aprende al cruzar el umbral de la antigua estructura.", "llegar:mazmorra_piso1", Origin.Ficticio),
            new CodexCatalog.Page("esquiva", "Esquivar en el último instante",
                "Esquivar justo cuando el golpe llega detiene el mundo un momento y te devuelve el aliento: puedes esquivar dos " +
                "veces más sin esperar.",
                "Una ofrenda de El Umbral la guarda.", "ofrenda.p1_especial", Origin.Ficticio),
            new CodexCatalog.Page("bloqueo", "Defender en el instante",
                "Alzar la defensa justo antes del golpe lo anula y deja aturdido a quien lo dio. Si la alzas una y otra vez sin " +
                "parar, el instante no llega. Cada bloqueo así le quita un rombo al escudo de tu enemigo, aunque no temas lo mismo que él.",
                "Detrás de una piedra agrietada del primer piso.", "ofrenda.p1_oculta", Origin.Ficticio),
            new CodexCatalog.Page("basalto", "La piedra que escucha",
                "El basalto agrietado cede ante el golpe de tierra. Donde la piedra se ve rota, la memoria suele esconder algo.",
                "La guarda el Pantano, detrás de la piedra.", "ofrenda.p2_oculta", Origin.Ficticio),
            new CodexCatalog.Page("altares", "Altares que eran tronos",
                "Las grandes piedras talladas de La Venta se llamaron altares durante mucho tiempo. Hoy muchos estudiosos las " +
                "interpretan como tronos donde se sentaba el gobernante. En la que se conoce como Altar 4, una figura sale de " +
                "un nicho, como de la boca de una cueva.",
                "Junto a una gran piedra tallada de la ciudad.", "examinar:altar_trono", Origin.Interpretado),
            new CodexCatalog.Page("cabezas", "Las cabezas colosales",
                "Los olmecas tallaron cabezas colosales de basalto. Se conocen diecisiete, de San Lorenzo, La Venta, Tres " +
                "Zapotes y sus alrededores. El basalto se trajo de la sierra de los Tuxtlas, a decenas de kilómetros. Se suele " +
                "pensar que retratan a gobernantes, pero eso es una interpretación.",
                "Junto al sello de un guardián de piedra.", "examinar:sello_guardian", Origin.Documentado),
            new CodexCatalog.Page("jade", "El jade azul-verde",
                "Los olmecas apreciaban la jadeíta azul-verde y la tallaban en hachas, máscaras y figurillas. Su origen se ha " +
                "rastreado hasta el valle del río Motagua, en la actual Guatemala, muy lejos de La Venta. Que el jade guarde " +
                "memorias es ficción de este juego.",
                "En lo más hondo de la estructura, tras la piedra.", "ofrenda.p3_oculta", Origin.Documentado),
        };
    }
}
