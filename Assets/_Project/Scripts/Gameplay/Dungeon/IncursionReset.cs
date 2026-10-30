using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Fuera de la mazmorra (en la selva): al cargarse la escena da por terminada la incursión y borra sus banderas, así
    /// que los santuarios de Vida vuelven a encenderse para la siguiente. Al pulsar Play aquí Boot aún no está y no
    /// hay nada que borrar.
    /// </summary>
    public sealed class IncursionReset : MonoBehaviour
    {
        void Start()
        {
            if (ServiceLocator.TryGet(out StoryState story))
                story.ClearFlagsWithPrefix(DungeonFlags.IncursionPrefix);
        }
    }
}
