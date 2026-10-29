using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Narrative
{
    /// <summary>
    /// Avisa "vencer:id" a las misiones cuando muere la Health del objeto (p. ej. "espiritu_jaguar").
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class ReportOnDefeat : MonoBehaviour
    {
        [SerializeField, Tooltip("Id del tipo de enemigo, compartido por todos los de su clase.")]
        string enemyId;

        Health health;

        public string EnemyId
        {
            get => enemyId;
            set => enemyId = value;
        }

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            if (!string.IsNullOrEmpty(enemyId) && ServiceLocator.TryGet(out StoryState story))
                story.Report(StoryEvents.Defeat(enemyId));
        }
    }
}
