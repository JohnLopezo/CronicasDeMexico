using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
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
    public sealed class DialogueInteractable : MonoBehaviour, IConversation
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
        int lineIndex;
        int lineCount;

        public string Prompt => prompt;

        public void Interact(Transform interactor)
        {
            // Los servicios se buscan aquí y no en Awake: al pulsar Play desde una escena, Boot se carga después.
            ServiceLocator.TryGet(out StoryState story);
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
            lineCount = 0;
        }

        // Solo al pasar la última línea: alejarse a medias no cuenta como haber hablado.
        void Finish()
        {
            if (lineCount == 0 || !ServiceLocator.TryGet(out StoryState story))
                return;

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

        void ShowLine()
        {
            if (messageChannel == null)
                return;

            string text, speaker;
            if (branch != null)
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

            messageChannel.Raise(string.IsNullOrEmpty(speaker) ? text : $"<b>{speaker}</b>\n{text}");
        }

        string SpeakerName => npc != null && !string.IsNullOrEmpty(npc.DisplayName) ? npc.DisplayName : speakerName;
    }
}
