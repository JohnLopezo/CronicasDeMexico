using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Monumento o inscripción que muestra un texto al examinarlo. Con un id, avisa "examinar:id" a las misiones.
    /// </summary>
    public sealed class InscriptionInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Examinar";

        [SerializeField, TextArea(3, 8)] string text;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Opcional: id para las misiones (aviso \"examinar:id\").")]
        string examineId;

        public string Prompt => prompt;

        public void Interact(Transform interactor)
        {
            if (messageChannel != null)
                messageChannel.Raise(text);
            if (!string.IsNullOrEmpty(examineId) && ServiceLocator.TryGet(out StoryState story))
                story.Report(StoryEvents.Examine(examineId));
        }
    }
}
