using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>Placa de la arena de pruebas: al pisarla, el jugador saca al jefe <c>index</c> (BossTrial).</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BossTrialPad : MonoBehaviour
    {
        [SerializeField] BossTrial trial;
        [SerializeField, Min(0)] int index;

        void OnTriggerEnter(Collider other)
        {
            if (trial != null && other.CompareTag("Player"))
                trial.Summon(index);
        }
    }
}
