using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Narrative
{
    /// <summary>
    /// Páginas del Códice de la Memoria (Docs/Sistemas/Codice.md). GameBootstrapper lo carga de Resources
    /// (<see cref="ResourcePath"/>) como ICodexCatalog, porque Core no puede referenciar a Data. Se edita en el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Narrativa/Catálogo del Códice", fileName = "CatalogoCodice")]
    public sealed class CodexCatalog : ScriptableObject, ICodexCatalog
    {
        public const string ResourcePath = "Narrativa/CatalogoCodice";

        [Serializable]
        public sealed class Page : ICodexPage
        {
            [SerializeField, Tooltip("Identificador estable; la bandera guardada es codice.<id>.")] string id;
            [SerializeField] string title;
            [SerializeField, TextArea(3, 8)] string text;
            [SerializeField, Tooltip("Lo que se ve mientras falta.")] string hint;
            [SerializeField, Tooltip("Aviso (\"examinar:reflejo\", \"llegar:mazmorra_piso1\") o bandera (\"ofrenda.p1_oculta\").")]
            string trigger;
            [SerializeField, Tooltip("Separar lo documentado, lo interpretado y lo ficticio (Diseno.md §3).")] Origin origin;

            public string Id => id;
            public string Title => title;
            public string Text => text;
            public string Hint => hint;
            public string Trigger => trigger;
            public Origin Origin => origin;

            public Page(string id, string title, string text, string hint, string trigger, Origin origin)
            {
                this.id = id;
                this.title = title;
                this.text = text;
                this.hint = hint;
                this.trigger = trigger;
                this.origin = origin;
            }
        }

        [SerializeField] Page[] pages = Array.Empty<Page>();

        public IReadOnlyList<ICodexPage> Pages => pages;

        public void SetPages(Page[] values) => pages = values ?? Array.Empty<Page>();
    }
}
