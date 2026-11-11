using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Gameplay.Npc;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Conversación de varias líneas con un personaje. Con un DialogueData, la rama depende del estado de la
    /// historia y, al terminarla, activa sus banderas, empieza su misión y avisa "hablar:id". Sin él, usa las
    /// líneas fijas. Si tiene NpcPatrol, el personaje se detiene y mira al jugador mientras dura.
    /// </summary>
    public sealed class DialogueInteractable : MonoBehaviour, IPathActionTarget
    {
        [SerializeField] string prompt = "Hablar";

        [SerializeField, Tooltip("Opcional: identidad del personaje (nombre y id para misiones).")]
        NpcData npc;

        [SerializeField, Tooltip("Opcional: diálogo según el estado de la historia. Si falta, se usan las líneas fijas.")]
        DialogueData dialogue;

        [SerializeField, Tooltip("Nombre que encabeza cada línea si no hay NpcData. Vacío = sin nombre.")]
        string speakerName;

        [SerializeField, TextArea(2, 5)] string[] lines;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Opcional: se detiene y mira al jugador mientras habla.")]
        NpcPatrol patrol;

        DialogueBranch branch;
        PathActionCatalog.Response pathResponse; // conversación de la acción de senda en curso
        int lineIndex;
        int lineCount;
        SpriteRenderer portraitRenderer;

        public string Prompt => prompt;

        public NpcData Npc => npc;

        /// <summary>Verbo de la acción de senda del Guardián elegido si este habitante tiene algo para ella y aún no la hizo.</summary>
        public string PathVerb => AvailablePathResponse() is { } response ? response.action.ToString() : null;

        PathActionCatalog.Response AvailablePathResponse()
        {
            if (npc == null || !ServiceLocator.TryGet(out Progression progression) || progression.Profile is not GuardianData guardian)
                return null;
            var response = PathActionCatalog.Load()?.Find(npc.Id, guardian.PathAction);
            if (response == null || response.lines.Length == 0)
                return null;
            return ServiceLocator.TryGet(out StoryState story) && story.HasFlag(response.DoneKey) ? null : response;
        }

        /// <summary>Acción de senda (mantener pulsado Hablar): sus líneas y, al terminarlas, su recompensa, una sola vez.</summary>
        public void PathInteract(Transform interactor)
        {
            pathResponse = AvailablePathResponse();
            if (pathResponse == null)
            {
                Interact(interactor);
                return;
            }
            branch = null;
            lineCount = pathResponse.lines.Length;
            lineIndex = 0;
            if (patrol != null)
                patrol.Hold(interactor.position);
            ShowLine();
        }

        public void Interact(Transform interactor)
        {
            // Los servicios se buscan aquí y no en Awake: al pulsar Play desde una escena, Boot se carga después.
            ServiceLocator.TryGet(out StoryState story);
            pathResponse = null;
            branch = dialogue != null ? dialogue.Select(flag => story != null && story.HasFlag(flag)) : null;
            lineCount = branch != null ? branch.Lines.Length : lines?.Length ?? 0;
            if (lineCount == 0)
                return;

            lineIndex = 0;
            if (patrol != null)
                patrol.Hold(interactor.position);
            ShowLine();
        }

        public bool Advance()
        {
            if (lineIndex + 1 >= lineCount)
            {
                Finish();
                return false;
            }

            lineIndex++;
            ShowLine();
            return true;
        }

        public void End()
        {
            if (patrol != null)
                patrol.Release();
            branch = null;
            pathResponse = null;
            lineCount = 0;
        }

        // Solo al pasar la última línea: alejarse a medias no cuenta como haber hablado.
        void Finish()
        {
            if (lineCount == 0 || !ServiceLocator.TryGet(out StoryState story))
                return;

            if (pathResponse != null)
            {
                FinishPathAction(story);
                return;
            }

            if (branch != null)
            {
                foreach (var flag in branch.FlagsOnFinish)
                    story.SetFlag(flag);
                if (branch.QuestToStart != null && ServiceLocator.TryGet(out QuestService quests))
                    quests.Start(branch.QuestToStart);
            }

            if (npc != null && !string.IsNullOrEmpty(npc.Id))
                story.Report(StoryEvents.Talk(npc.Id));
        }

        // La recompensa se da al terminar las líneas, como un diálogo: alejarse a medias no cuenta.
        void FinishPathAction(StoryState story)
        {
            var response = pathResponse;
            story.SetFlag(response.DoneKey);
            if (!ServiceLocator.TryGet(out RewardService rewards))
            {
                foreach (var flag in response.flags)
                    story.SetFlag(flag);
                return;
            }
            var package = new RewardPackage { Experience = response.experience };
            foreach (var item in response.items)
                if (item != null)
                    package.Add(item.id, item.count);
            foreach (var flag in response.flags)
                if (!string.IsNullOrEmpty(flag))
                    package.Flags.Add(flag);
            if (!package.IsEmpty)
                rewards.Grant(package, onceKey: response.DoneKey);
        }

        void ShowLine()
        {
            if (messageChannel == null)
                return;

            string text, speaker;
            if (pathResponse != null)
            {
                text = pathResponse.lines[lineIndex];
                speaker = SpeakerName;
            }
            else if (branch != null)
            {
                var line = branch.Lines[lineIndex];
                text = line.Text;
                speaker = line.Speaker != null ? line.Speaker.DisplayName : SpeakerName;
            }
            else
            {
                text = lines[lineIndex];
                speaker = SpeakerName;
            }

            // Retrato: el sprite de este personaje, solo si habla él (una línea puede ser de otro).
            var portrait = !string.IsNullOrEmpty(speaker) && speaker == SpeakerName ? PortraitRenderer() : null;
            if (portrait != null && portrait.sprite != null)
                SpeakerPortrait.Set(speaker, portrait.sprite);
            else
                SpeakerPortrait.Clear();
            messageChannel.Raise(string.IsNullOrEmpty(speaker) ? text : $"<b>{speaker}</b>\n{text}");
        }

        // El trigger puede ser el personaje o un hijo suyo; el sprite está en un hijo del personaje.
        SpriteRenderer PortraitRenderer()
        {
            if (portraitRenderer == null)
                portraitRenderer = GetComponentInChildren<SpriteRenderer>();
            if (portraitRenderer == null && transform.parent != null)
                portraitRenderer = transform.parent.GetComponentInChildren<SpriteRenderer>();
            return portraitRenderer;
        }

        string SpeakerName => npc != null && !string.IsNullOrEmpty(npc.DisplayName) ? npc.DisplayName : speakerName;
    }
}
