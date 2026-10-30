using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Fragmento de memoria que se recoge una sola vez: al tocarlo muestra lo que recuerda y avisa "examinar:id"; al
    /// cerrarse el mensaje activa su bandera, y ActiveWhenFlags lo retira. La bandera no se pone al tocarlo porque, si el
    /// fragmento desapareciera con el mensaje abierto, PlayerInteractor lo perdería de vista y cerraría el mensaje.
    /// </summary>
    public sealed class MemoryFragmentInteractable : MonoBehaviour, IConversation
    {
        [SerializeField] string prompt = "Recoger";

        [SerializeField, TextArea(3, 8)] string text;

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Id para las misiones (aviso \"examinar:id\"), compartido por todos los fragmentos.")]
        string examineId = "fragmento_menor";

        [SerializeField, Tooltip("Bandera propia de este fragmento: recogido.")]
        string collectedFlag;

        bool open;

        public string Prompt => prompt;

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
                story.SetFlag(collectedFlag);
            // También va a la bolsa y al Museo de la Memoria (M5), una sola vez por fragmento.
            if (ServiceLocator.TryGet(out RewardService rewards))
                rewards.Grant(new RewardPackage { Experience = 20 }.Add(FragmentItemId, 1), "fragmento." + collectedFlag);
        }

        /// <summary>Objeto «Fragmento de memoria menor» (Docs/Contenido/Catalogo_Objetos_Olmeca.md).</summary>
        public const string FragmentItemId = "fragmento_menor";
    }
}
