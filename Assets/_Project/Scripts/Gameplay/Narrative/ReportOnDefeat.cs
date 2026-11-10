using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Narrative
{
    /// <summary>
    /// Avisa "vencer:id" a las misiones cuando muere la Health del objeto (p. ej. "espiritu_jaguar"). Con una bandera,
    /// además la activa: así se recuerda a este enemigo en concreto (p. ej. para que no vuelva, con ActiveWhenFlags).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class ReportOnDefeat : MonoBehaviour
    {
        [SerializeField, Tooltip("Id del tipo de enemigo, compartido por todos los de su clase.")]
        string enemyId;

        [SerializeField, Tooltip("Opcional: bandera que se activa al vencer a este enemigo.")]
        string flagOnDefeat;

        Health health;

        public string EnemyId
        {
            get => enemyId;
            set => enemyId = value;
        }

        public string FlagOnDefeat
        {
            get => flagOnDefeat;
            set => flagOnDefeat = value;
        }

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            if (!ServiceLocator.TryGet(out StoryState story))
                return;
            if (!string.IsNullOrEmpty(enemyId))
                story.Report(StoryEvents.Defeat(enemyId));
            story.SetFlag(flagOnDefeat);
        }
    }
}
