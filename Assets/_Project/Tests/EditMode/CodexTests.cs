using System.Collections.Generic;
using System.Linq;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Data.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace CronicasDeMexico.Tests
{
    /// <summary>E6: páginas del Códice escondidas (Docs/Sistemas/Codice.md).</summary>
    public class CodexTests
    {
        sealed class Catalog : ICodexCatalog
        {
            public IReadOnlyList<ICodexPage> Pages { get; } = new ICodexPage[]
            {
                new CodexCatalog.Page("evento", "Por aviso", "Texto", "Pista", "examinar:reflejo", Origin.Ficticio),
                new CodexCatalog.Page("bandera", "Por bandera", "Texto", "Pista", "ofrenda.p1_oculta", Origin.Documentado),
            };
        }

        [Test]
        public void Pages_AreFoundByTheirEventOrFlag_OnlyOnce_AndSavedAsFlags()
        {
            var story = new StoryState();
            var codex = new CodexService(story, new Catalog());
            var found = new List<string>();
            codex.PageFound += page => found.Add(page.Id);

            story.Report("examinar:otra_cosa");
            Assert.AreEqual(0, codex.FoundCount);

            story.Report("examinar:reflejo");
            story.Report("examinar:reflejo");
            CollectionAssert.AreEqual(new[] { "evento" }, found, "Se encuentra una sola vez.");
            Assert.IsTrue(story.HasFlag(CodexService.FlagFor("evento")), "Lo encontrado es una bandera: se guarda con la partida.");

            story.SetFlag("ofrenda.p1_oculta");
            CollectionAssert.AreEqual(new[] { "evento", "bandera" }, found);
            Assert.AreEqual(2, codex.FoundCount);
        }

        [Test]
        public void Loading_RecoversFlagPagesSilently_ButClearingAFlagRevealsNothing()
        {
            var story = new StoryState();
            var codex = new CodexService(story, new Catalog());
            var found = 0;
            codex.PageFound += _ => found++;

            story.SetFlag("ofrenda.p1_oculta");
            story.ClearFlag(CodexService.FlagFor("bandera"));
            story.ClearFlag("ofrenda.p1_oculta");
            Assert.AreEqual(0, codex.FoundCount, "Quitar la bandera no revela la página.");

            // Una partida de antes del Códice: la ofrenda ya estaba abierta.
            var data = new SaveData();
            data.flags.Add("ofrenda.p1_oculta");
            found = 0;
            story.Restore(data);
            codex.Restore(data);
            Assert.AreEqual(1, codex.FoundCount, "Al cargar se recupera la página de la ofrenda ya abierta.");
            Assert.AreEqual(0, found, "Sin aviso: no es un hallazgo nuevo.");
        }

        [Test]
        public void Catalog_InResources_HasCompletePages_WithUniqueIdsAndTriggers()
        {
            var catalog = Resources.Load<CodexCatalog>(CodexCatalog.ResourcePath);
            Assert.IsNotNull(catalog, "Falta Resources/Narrativa/CatalogoCodice (Crónicas → Crear catálogo del Códice).");
            Assert.GreaterOrEqual(catalog.Pages.Count, 8);
            foreach (var page in catalog.Pages)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Id));
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Title), page.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Text), page.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Hint), page.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(page.Trigger), page.Id);
            }
            Assert.AreEqual(catalog.Pages.Count, catalog.Pages.Select(page => page.Id).Distinct().Count(), "Ids repetidos.");
            Assert.AreEqual(catalog.Pages.Count, catalog.Pages.Select(page => page.Trigger).Distinct().Count(),
                "Dos páginas con el mismo disparador aparecerían a la vez.");
        }
    }
}
