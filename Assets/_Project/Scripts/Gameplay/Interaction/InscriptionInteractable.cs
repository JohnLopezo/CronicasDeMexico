using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Monumento o inscripción que muestra un texto al examinarlo. Con un id, avisa "examinar:id" a las misiones.
    /// Puede mostrar otro texto cuando la historia activa una bandera (p. ej. tras una misión).
    /// </summary>
    public sealed class InscriptionInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Examinar";

        [SerializeField, TextArea(3, 8)] string text;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Opcional: id para las misiones (aviso \"examinar:id\").")]
        string examineId;

        [SerializeField, Tooltip("Opcional: con esta bandera activa se muestra el texto alternativo.")]
        string alternateFlag;

        [SerializeField, TextArea(3, 8)] string alternateText;

        public string Prompt => prompt;

        public void Interact(Transform interactor)
        {
            ServiceLocator.TryGet(out StoryState story);
            var useAlternate = story != null && !string.IsNullOrEmpty(alternateText) && story.HasFlag(alternateFlag);
            if (messageChannel != null)
                messageChannel.Raise(useAlternate ? alternateText : text);
            if (!string.IsNullOrEmpty(examineId) && story != null)
                story.Report(StoryEvents.Examine(examineId));
        }
    }
}
