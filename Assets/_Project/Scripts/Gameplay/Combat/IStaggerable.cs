namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Personaje que se puede aturdir desde fuera: interrumpe su ataque y se queda quieto unos segundos (bloqueo
    /// perfecto del jugador). Los jefes no lo implementan: sus ataques son corrutinas con fases y no se cortan.
    /// </summary>
    public interface IStaggerable
    {
        void Stagger(float seconds);
    }
}
