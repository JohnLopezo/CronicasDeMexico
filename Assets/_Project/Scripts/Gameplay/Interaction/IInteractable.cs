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

        void Interact();
    }
}
