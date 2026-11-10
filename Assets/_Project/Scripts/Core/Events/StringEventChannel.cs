using UnityEngine;

namespace CronicasDeMexico.Core.Events
{
    /// <summary>
    /// Canal de eventos con un texto (p. ej. avisos de interacción o mensajes). Un texto vacío o nulo significa
    /// "ocultar".
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Eventos/Evento de texto", fileName = "EVT_Nuevo")]
    public sealed class StringEventChannel : EventChannel<string>
    {
    }
}
