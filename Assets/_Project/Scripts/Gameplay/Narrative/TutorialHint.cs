using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Narrative
{
    /// <summary>
    /// Pista del tutorial (El Umbral, Docs/Mazmorras/Olmeca.md): al entrar el jugador en la zona muestra un mensaje, una
    /// sola vez en toda la partida (bandera "tutorial.id"), y lo quita al salir. El jugador también puede cerrarlo con
    /// Interactuar, como cualquier mensaje.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TutorialHint : MonoBehaviour
    {
        public const string FlagPrefix = "tutorial.";

        [SerializeField, Tooltip("Id de la pista, único en el juego.")]
        string hintId;

        [SerializeField, TextArea(2, 6)] string text;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        bool showing;

        public string HintId
        {
            get => hintId;
            set => hintId = value;
        }

        public string Text
        {
            get => text;
            set => text = value;
        }

        public StringEventChannel MessageChannel
        {
            get => messageChannel;
            set => messageChannel = value;
        }

        public string Flag => FlagPrefix + hintId;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (showing || !other.CompareTag("Player"))
                return;

            ServiceLocator.TryGet(out StoryState story);
            if (story != null)
            {
                if (story.HasFlag(Flag))
                    return;
                story.SetFlag(Flag);
            }

            showing = true;
            if (messageChannel != null)
                messageChannel.Raise(text);
        }

        void OnTriggerExit(Collider other)
        {
            if (!showing || !other.CompareTag("Player"))
                return;
            showing = false;
            if (messageChannel != null)
                messageChannel.Raise(null);
        }
    }
}
