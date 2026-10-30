using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Interaction;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Paso sellado que se abre al examinarlo (la losa de la antesala, M4): muestra su texto, avisa "examinar:id" y, al
    /// cerrarse el mensaje, activa su bandera. ActiveWhenFlags retira entonces la losa y su bloqueo. La bandera no se
    /// pone al tocarla porque, si la losa desapareciera con el mensaje abierto, PlayerInteractor lo cerraría al instante
    /// (como MemoryFragmentInteractable).
    /// </summary>
    public sealed class SealedPassage : MonoBehaviour, IConversation
    {
        [SerializeField] string prompt = "Empujar la losa";

        [SerializeField, TextArea(3, 8)] string text;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Id para las misiones (aviso \"examinar:id\").")]
        string examineId;

        [SerializeField, Tooltip("Bandera que lo deja abierto.")]
        string openFlag;

        bool open;

        public string Prompt => prompt;

        public string OpenFlag
        {
            get => openFlag;
            set => openFlag = value;
        }

        public void Interact(Transform interactor)
        {
            open = true;
            if (messageChannel != null)
                messageChannel.Raise(text);
            if (!string.IsNullOrEmpty(examineId) && ServiceLocator.TryGet(out StoryState story))
                story.Report(StoryEvents.Examine(examineId));
        }

        public bool Advance() => false;

        public void End()
        {
            if (!open)
                return;
            open = false;
            if (ServiceLocator.TryGet(out StoryState story))
                story.SetFlag(openFlag);
        }
    }
}
