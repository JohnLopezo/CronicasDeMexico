using UnityEngine;

namespace CronicasDeMexico.Gameplay.Movement
{
    /// <summary>
    /// Terreno que frena a quien lo pisa (agua baja y lodo del Pantano, Docs/Mazmorras/Olmeca.md): un trigger que, en
    /// cada paso de física, frena a cualquier ISlowable que tenga dentro (el jugador y los enemigos que caminan).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SlowZone : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 1f), Tooltip("Multiplicador de la velocidad dentro de la zona.")]
        float speedFactor = 0.5f;

        public float SpeedFactor
        {
            get => speedFactor;
            set => speedFactor = value;
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent(out ISlowable slowable))
                slowable.ApplySlow(speedFactor);
        }
    }
}
