using CronicasDeMexico.Core.Events;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Monumento o inscripción que muestra un texto al examinarlo.
    /// </summary>
    public sealed class InscriptionInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Examinar";

        [SerializeField, TextArea(3, 8)] string text;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        public string Prompt => prompt;

        public void Interact()
        {
            if (messageChannel != null)
                messageChannel.Raise(text);
        }
    }
}
