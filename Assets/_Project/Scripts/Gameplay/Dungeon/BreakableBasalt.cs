using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Basalto agrietado que bloquea un paso y solo cede al golpe de tierra (PlayerAbility): la función de exploración
    /// de la Bendición de la Tierra. Al romperse activa su bandera ("roto.id"), y ActiveWhenFlags lo retira para siempre.
    /// </summary>
    public sealed class BreakableBasalt : MonoBehaviour
    {
        public const string FlagPrefix = "roto.";

        [SerializeField, Tooltip("Id único del bloque.")] string breakId;

        public string BreakId
        {
            get => breakId;
            set => breakId = value;
        }

        public string Flag => FlagPrefix + breakId;

        public void Break()
        {
            if (ServiceLocator.TryGet(out StoryState story))
                story.SetFlag(Flag);
            else
                gameObject.SetActive(false);
        }
    }
}
