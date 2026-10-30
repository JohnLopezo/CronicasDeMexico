using CronicasDeMexico.Core.Events;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Servicio de un NPC (el taller del artesano, el puesto del comerciante): al tocarlo abre la pantalla del menú
    /// correspondiente (MenuTabs) por el canal EVT_Menu_Abrir. Va junto al NPC, aparte de su conversación.
    /// </summary>
    public sealed class ServicePoint : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Comerciar";
        [SerializeField, Tooltip("Pantalla del menú (MenuTabs).")] string menuTab;
        [SerializeField] StringEventChannel menuChannel;

        public string Prompt => prompt;

        public void Configure(string verb, string tab, StringEventChannel channel)
        {
            prompt = verb;
            menuTab = tab;
            menuChannel = channel;
        }

        public void Interact(Transform interactor)
        {
            if (menuChannel != null)
                menuChannel.Raise(menuTab);
        }
    }
}
