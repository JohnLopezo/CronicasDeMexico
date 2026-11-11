using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Narrative;
using UnityEditor;
using UnityEngine;
using static CronicasDeMexico.Data.Narrative.PathActionCatalog;

namespace CronicasDeMexico.Editor.SceneGeneration
{
    /// <summary>
    /// Crea el catálogo de acciones de senda (Resources/Narrativa/AccionesSenda) con las respuestas de los habitantes de
    /// la ciudad olmeca, solo si falta: después se edita en el Inspector. **Las líneas son borradores de Claude**,
    /// pendientes del autor (Docs/Sistemas/Acciones_Senda.md); no añaden historia nueva a la biblia.
    /// Menú Crónicas → Crear acciones de senda, o -executeMethod CronicasDeMexico.Editor.SceneGeneration.PathActionAssets.Ensure.
    /// </summary>
    public static class PathActionAssets
    {
        const string Path = "Assets/_Project/Resources/" + ResourcePath + ".asset";

        [MenuItem("Crónicas/Crear acciones de senda")]
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<PathActionCatalog>(Path) != null)
            {
                Debug.Log($"[{nameof(PathActionAssets)}] {Path} ya existe: no se toca.");
                return;
            }
            var catalog = ScriptableObject.CreateInstance<PathActionCatalog>();
            catalog.SetResponses(OlmecaResponses());
            AssetDatabase.CreateAsset(catalog, Path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(PathActionAssets)}] Creado {Path}.");
        }

        static Response R(string npcId, PathAction action, string[] lines, int experience = 0, string[] flags = null,
            params (string id, int count)[] items)
        {
            var response = new Response { npcId = npcId, action = action, lines = lines, experience = experience, flags = flags ?? new string[0] };
            response.items = new Item[items.Length];
            for (var i = 0; i < items.Length; i++)
                response.items[i] = new Item { id = items[i].id, count = items[i].count };
            return response;
        }

        public static Response[] OlmecaResponses() => new[]
        {
            R(OlmecaNarrativeAssets.ArtisanId, PathAction.Indagar, new[]
            {
                "Te fijas en sus manos: tiene polvo verde bajo las uñas, pero no hay ninguna pieza terminada a su alrededor.",
                "—Llevo días sin acabar nada. Empiezo a tallar y se me olvida qué figura buscaba.",
                "—Toma esto. Si tú lo recuerdas mejor que yo, úsalo bien.",
            }, experience: 10, items: ("jade", 1)),
            R(OlmecaNarrativeAssets.ArtisanId, PathAction.Persuadir, new[]
            {
                "—¿Que te enseñe a atar un amuleto? Los jóvenes siempre con prisa...",
                "—Está bien. Dos piedras verdes y fibra fuerte. Que no se te olvide a ti también.",
            }, flags: new[] { "receta.amuleto_jade" }),
            R(OlmecaNarrativeAssets.ElderId, PathAction.Escuchar, new[]
            {
                "Te sientas a su lado sin decir nada. Al rato, ella habla.",
                "—Mi abuela decía que el río se acuerda de todo lo que cae en él. Por eso no le tengo miedo al agua: le tengo miedo al silencio.",
            }, experience: 20),
            R("curandera", PathAction.Escuchar, new[]
            {
                "—Casi nadie se queda a escuchar a una vieja que muele semillas.",
                "—Te contaré algo: el cacao no cura el cuerpo, pero devuelve las ganas. Muélelo con maíz y bébelo caliente.",
                "—Ya sabes prepararlo. Ven a mi fogón cuando tengas con qué.",
            }, flags: new[] { "receta.bebida_cacao" }),
            R("curandera", PathAction.Indagar, new[]
            {
                "Miras sus cestos: hay más hojas secas de las que una sola persona necesitaría.",
                "—Me preparo. Desde que las estelas callaron, llega gente con heridas que no recuerda cómo se hizo.",
            }, items: ("semillas", 3)),
            R("pescador", PathAction.Persuadir, new[]
            {
                "—¿Ayudarte? Si sacas tú la red, te doy parte de lo que traiga.",
                "Tiras de la red con fuerza. Sale cargada.",
                "—¡Ja! Tienes brazos de guerrero. Llévate estos.",
            }, items: ("pescado", 3)),
            R("pescador", PathAction.Escuchar, new[]
            {
                "—El río está raro. Hay remansos donde los peces ya no entran, como si el agua hubiera olvidado su camino.",
            }, experience: 10, items: ("pescado", 1)),
            R(OlmecaNarrativeAssets.MerchantId, PathAction.Persuadir, new[]
            {
                "—No le vendo cacao a cualquiera. Es caro de traer y la gente ya no recuerda para qué sirve.",
                "—Pero tú parece que sí. Está bien: desde hoy tengo cacao para ti.",
            }, flags: new[] { "oferta.cacao" }),
            R(OlmecaNarrativeAssets.MerchantId, PathAction.Indagar, new[]
            {
                "Notas que esconde algo bajo la manta del puesto.",
                "—Buen ojo. Dicen que en la antigua estructura de la selva, detrás de la piedra agrietada, hay ofrendas que nadie ha tocado.",
            }, experience: 15),
            R(OlmecaNarrativeAssets.VillageGuardId, PathAction.Indagar, new[]
            {
                "Sigues su mirada: no aparta los ojos del sendero del oeste.",
                "—Las criaturas siempre llegan por ahí. No buscan comida: buscan a la gente, como si quisieran borrarla.",
            }, experience: 10),
            R(OlmecaNarrativeAssets.WarriorId, PathAction.Persuadir, new[]
            {
                "—¿Entrenar contigo? Bien. Te enseño lo que repetía mi maestro: no golpees tres veces si no sabes cómo salir después.",
            }, experience: 10, items: ("semilla_preparada", 1)),
        };
    }
}
