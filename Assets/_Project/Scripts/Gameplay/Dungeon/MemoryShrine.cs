using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Interaction;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Santuario de Memoria (Diseno.md §9): al descansar fija el punto de control en su punto de aparición, con la vida
    /// que tiene el jugador, recupera toda la energía (que no se regenera sola) y guarda la partida. No cura (eso es el
    /// santuario de Vida). La primera vez despierta su bandera, que enciende el jade (ActiveWhenFlags cambia el sprite).
    /// Al cerrar el mensaje abre el Museo de la Memoria.
    /// </summary>
    public sealed class MemoryShrine : MonoBehaviour, IConversation
    {
        /// <summary>Aviso "examinar:santuario_memoria" al descansar, común a todos (MP_OLM_04).</summary>
        public const string ExamineId = "santuario_memoria";

        [SerializeField] string prompt = "Descansar";

        [SerializeField, Tooltip("Id del santuario, único en el juego.")]
        string shrineId;

        [SerializeField, Tooltip("Id del SpawnPoint de esta escena donde se reaparece (delante del santuario).")]
        string spawnId;

        [SerializeField, TextArea(2, 6)]
        string restText = "Tocas el hacha de jade y la piedra se aquieta. Recuperas la energía. Este lugar te recordará.";

        [SerializeField, TextArea(2, 6)]
        string saveFailedText = "Tocas el hacha de jade, pero la piedra no responde. (No se pudo guardar la partida.)";

        [SerializeField, Tooltip("Canal por el que el HUD recibe el mensaje.")]
        StringEventChannel messageChannel;

        [SerializeField, Tooltip("Canal para abrir el Museo de la Memoria al terminar (MenuTabs).")]
        StringEventChannel menuChannel;

        bool open;

        public string Prompt => prompt;

        public string ShrineId
        {
            get => shrineId;
            set => shrineId = value;
        }

        public string SpawnId
        {
            get => spawnId;
            set => spawnId = value;
        }

        public void Interact(Transform interactor)
        {
            open = true;
            var health = interactor != null ? interactor.GetComponentInParent<Health>() : null;
            if (ServiceLocator.TryGet(out PlayerCheckpoint checkpoint))
                checkpoint.Set(gameObject.scene.name, spawnId, health != null ? health.Current : 0);
            if (ServiceLocator.TryGet(out Progression progression))
                progression.FillEnergy();
            if (ServiceLocator.TryGet(out StoryState story))
            {
                story.SetFlag(DungeonFlags.MemoryShrineAwake(shrineId));
                story.Report(StoryEvents.Examine(ExamineId)); // antes de guardar: el paso de la misión queda guardado
            }

            var saved = ServiceLocator.TryGet(out SaveService save) && save.Save();
            if (messageChannel != null)
                messageChannel.Raise(saved ? restText : saveFailedText);
        }

        public bool Advance() => false;

        public void End()
        {
            if (!open)
                return;
            open = false;
            if (menuChannel != null)
                menuChannel.Raise(MenuTabs.Museum);
        }
    }
}
