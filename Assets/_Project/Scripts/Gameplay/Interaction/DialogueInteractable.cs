using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Gameplay.Npc;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Conversación de varias líneas con un personaje. Si tiene NpcPatrol, el personaje se detiene y mira al
    /// jugador mientras dura.
    /// </summary>
    public sealed class DialogueInteractable : MonoBehaviour, IConversation
    {
        [SerializeField] string prompt = "Hablar";

        [SerializeField, Tooltip("Nombre que encabeza cada línea. Vacío = sin nombre.")]
        string speakerName;

        [SerializeField, TextArea(2, 5)] string[] lines;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Opcional: se detiene y mira al jugador mientras habla.")]
        NpcPatrol patrol;

        int lineIndex;

        public string Prompt => prompt;

        public void Interact(Transform interactor)
        {
            if (lines == null || lines.Length == 0)
                return;

            lineIndex = 0;
            if (patrol != null)
                patrol.Hold(interactor.position);
            ShowLine();
        }

        public bool Advance()
        {
            if (lines == null || lineIndex + 1 >= lines.Length)
                return false;

            lineIndex++;
            ShowLine();
            return true;
        }

        public void End()
        {
            if (patrol != null)
                patrol.Release();
        }

        void ShowLine()
        {
            if (messageChannel == null)
                return;

            var line = lines[lineIndex];
            messageChannel.Raise(string.IsNullOrEmpty(speakerName) ? line : $"<b>{speakerName}</b>\n{line}");
        }
    }
}
