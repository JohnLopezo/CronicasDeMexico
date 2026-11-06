using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Narrative
{
    /// <summary>
    /// Muestra u oculta un objeto según las banderas de la historia: activo solo si están todas las requeridas y
    /// ninguna de las prohibidas. Va en un objeto distinto del que controla (si se desactivara a sí mismo dejaría de
    /// escuchar). Con liveUpdate reacciona en cuanto cambian las banderas; sin él, solo al cargar la escena (para que
    /// un personaje no desaparezca en mitad de una conversación).
    /// </summary>
    public sealed class ActiveWhenFlags : MonoBehaviour
    {
        [SerializeField] GameObject target;

        [SerializeField, Tooltip("Banderas que deben estar activas.")]
        string[] requiredFlags = System.Array.Empty<string>();

        [SerializeField, Tooltip("Banderas que no deben estar activas.")]
        string[] forbiddenFlags = System.Array.Empty<string>();

        [SerializeField, Tooltip("Reaccionar a los cambios durante la partida, no solo al cargar.")]
        bool liveUpdate = true;

        StoryState story;

        public GameObject Target
        {
            get => target;
            set => target = value;
        }

        // Al pulsar Play desde una escena de juego, Boot (que registra StoryState) se carga después: se espera a él.
        void Update()
        {
            if (!ServiceLocator.TryGet(out story))
                return;

            enabled = false;
            Apply();
            if (liveUpdate)
                story.FlagChanged += OnFlagChanged;
        }

        void OnDestroy()
        {
            if (story != null)
                story.FlagChanged -= OnFlagChanged;
        }

        void OnFlagChanged(string flag) => Apply();

        void Apply()
        {
            if (target != null)
                target.SetActive(IsSatisfied(story));
        }

        public bool IsSatisfied(StoryState state)
        {
            foreach (var flag in requiredFlags)
                if (!state.HasFlag(flag))
                    return false;
            foreach (var flag in forbiddenFlags)
                if (state.HasFlag(flag))
                    return false;
            return true;
        }
    }
}
