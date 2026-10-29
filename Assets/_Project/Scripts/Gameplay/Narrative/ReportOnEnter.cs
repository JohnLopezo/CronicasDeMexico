using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Narrative
{
    /// <summary>
    /// Zona (collider trigger) que avisa "llegar:id" a las misiones cada vez que entra el jugador, o solo la
    /// primera con onlyOnce. Por defecto se repite: así cuenta aunque la misión empiece después de pasar por ella.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ReportOnEnter : MonoBehaviour
    {
        [SerializeField] string zoneId;

        [SerializeField, Tooltip("Avisar solo la primera vez.")]
        bool onlyOnce;

        bool reported;

        public string ZoneId
        {
            get => zoneId;
            set => zoneId = value;
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if ((onlyOnce && reported) || string.IsNullOrEmpty(zoneId) || !other.CompareTag("Player"))
                return;
            if (!ServiceLocator.TryGet(out StoryState story))
                return;

            reported = true;
            story.Report(StoryEvents.Reach(zoneId));
        }
    }
}
