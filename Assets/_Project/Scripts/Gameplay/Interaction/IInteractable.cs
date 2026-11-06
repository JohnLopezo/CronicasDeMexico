using UnityEngine;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Algo con lo que el jugador puede interactuar al acercarse. El componente que lo implementa debe estar en
    /// un objeto con collider en el layer "Interactuable" (o en uno de sus hijos).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Verbo que se muestra en el aviso, p. ej. "Examinar".</summary>
        string Prompt { get; }

        /// <param name="interactor">Quién interactúa (para mirarlo, por ejemplo).</param>
        void Interact(Transform interactor);
    }

    /// <summary>
    /// Interacción de varias líneas: mientras su mensaje está abierto, Interactuar pasa a la siguiente.
    /// </summary>
    public interface IConversation : IInteractable
    {
        /// <summary>Muestra la línea siguiente. Devuelve false si no quedaban; entonces se cierra el mensaje.</summary>
        bool Advance();

        /// <summary>Se llama al cerrarse el mensaje, haya terminado o porque el jugador se alejó.</summary>
        void End();
    }
}
