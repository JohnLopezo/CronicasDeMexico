using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;

namespace CronicasDeMexico.Core.Narrative
{
    /// <summary>Una página del Códice de la Memoria (Docs/Sistemas/Codice.md).</summary>
    public interface ICodexPage
    {
        string Id { get; }
        string Title { get; }
        string Text { get; }
        /// <summary>Pista que se ve mientras falta (vaga a propósito: es un secreto).</summary>
        string Hint { get; }
        /// <summary>Aviso de StoryEvents ("examinar:reflejo") o bandera ("ofrenda.p1_oculta") que la revela.</summary>
        string Trigger { get; }
        Origin Origin { get; }
    }

    public interface ICodexCatalog
    {
        IReadOnlyList<ICodexPage> Pages { get; }
    }

    /// <summary>
    /// Páginas del Códice escondidas por el mundo, al estilo de las páginas del manual de Tunic: enseñan mecánicas y
    /// cuentan lo que se sabe de cada cultura. Una página se revela con su disparador, que es un aviso de la historia o
    /// una bandera (las ofrendas ocultas ponen "ofrenda.&lt;id&gt;"), así se esconden en sitios que ya existen sin tocar
    /// escenas. Lo encontrado son banderas "codice.&lt;id&gt;" de StoryState, que ya se guardan con la partida. Al cargar,
    /// las páginas de disparador bandera que ya se cumplía se recuperan en silencio (partidas de antes del Códice).
    /// </summary>
    public sealed class CodexService : ISaveParticipant
    {
        public const string FlagPrefix = "codice.";

        readonly StoryState story;
        readonly IReadOnlyList<ICodexPage> pages;
        bool restoring;

        /// <summary>Se encontró una página (no al cargar).</summary>
        public event Action<ICodexPage> PageFound;

        public CodexService(StoryState story, ICodexCatalog catalog)
        {
            this.story = story;
            pages = catalog?.Pages ?? Array.Empty<ICodexPage>();
            story.EventReported += OnTrigger;
            story.FlagChanged += OnTrigger;
        }

        public IReadOnlyList<ICodexPage> Pages => pages;

        public static string FlagFor(string pageId) => FlagPrefix + pageId;

        public bool IsFound(ICodexPage page) => page != null && story.HasFlag(FlagFor(page.Id));

        public int FoundCount
        {
            get
            {
                var count = 0;
                foreach (var page in pages)
                    if (IsFound(page))
                        count++;
                return count;
            }
        }

        void OnTrigger(string trigger)
        {
            if (string.IsNullOrEmpty(trigger) || trigger.StartsWith(FlagPrefix))
                return;
            foreach (var page in pages)
            {
                // Una bandera que se quita no revela nada.
                if (page == null || page.Trigger != trigger || IsFound(page) || (!trigger.Contains(":") && !story.HasFlag(trigger)))
                    continue;
                story.SetFlag(FlagFor(page.Id));
                if (!restoring)
                    PageFound?.Invoke(page);
            }
        }

        public void Capture(SaveData data)
        {
            // Las páginas encontradas son banderas: las guarda StoryState.
        }

        public void Restore(SaveData data)
        {
            restoring = true;
            try
            {
                foreach (var page in pages)
                    if (page != null && !string.IsNullOrEmpty(page.Trigger) && !page.Trigger.Contains(":") && story.HasFlag(page.Trigger))
                        OnTrigger(page.Trigger);
            }
            finally
            {
                restoring = false;
            }
        }
    }
}
