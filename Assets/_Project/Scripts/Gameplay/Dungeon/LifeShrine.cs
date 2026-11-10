using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Interaction;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Santuario de Vida (Diseno.md §9): cura una parte de la vida una sola vez por incursión. El uso queda en una
    /// bandera de la incursión, que apaga el fuego (ActiveWhenFlags) y se borra al salir de la mazmorra. Con la vida
    /// llena no se gasta.
    /// </summary>
    public sealed class LifeShrine : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Calentarse";

        [SerializeField, Tooltip("Id del santuario, único en el juego.")]
        string shrineId;

        [SerializeField, Range(0f, 1f), Tooltip("Parte de la vida máxima que cura (se redondea hacia arriba).")]
        float healFraction = 0.5f;

        [SerializeField, TextArea(2, 6)]
        string healText = "El calor del fogón te entra en los huesos y las heridas se cierran un poco. El fuego se apaga.";

        [SerializeField, TextArea(2, 6)]
        string fullHealthText = "Te sientes entero. Mejor guardar este fuego para cuando haga falta.";

        [SerializeField, TextArea(2, 6)]
        string usedText = "Solo quedan brasas frías. Este fuego no volverá a encenderse hasta que salgas de la estructura.";

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        public string Prompt => prompt;

        public string ShrineId
        {
            get => shrineId;
            set => shrineId = value;
        }

        public void Interact(Transform interactor)
        {
            var health = interactor != null ? interactor.GetComponentInParent<Health>() : null;
            if (health == null || !ServiceLocator.TryGet(out StoryState story))
                return;

            var usedFlag = DungeonFlags.LifeShrineUsed(shrineId);
            string text;
            if (story.HasFlag(usedFlag))
                text = usedText;
            else if (health.Current >= health.Max)
                text = fullHealthText;
            else
            {
                health.Heal(Mathf.CeilToInt(health.Max * healFraction), HealSource.RestShrine);
                story.SetFlag(usedFlag);
                text = healText;
            }

            if (messageChannel != null)
                messageChannel.Raise(text);
        }
    }
}
