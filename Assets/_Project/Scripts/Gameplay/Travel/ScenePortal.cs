using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Travel
{
    /// <summary>
    /// Zona (collider trigger) que lleva al jugador a otra escena, al punto de aparición indicado, con la vida que
    /// tiene (SceneTravel). El punto de llegada del viaje de vuelta debe quedar fuera de la zona.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ScenePortal : MonoBehaviour
    {
        [SerializeField, Tooltip("Escena de destino. Debe estar en Build Settings.")]
        string targetScene;

        [SerializeField, Tooltip("Id del SpawnPoint de la escena de destino.")]
        string targetSpawnId;

        public string TargetScene
        {
            get => targetScene;
            set => targetScene = value;
        }

        public string TargetSpawnId
        {
            get => targetSpawnId;
            set => targetSpawnId = value;
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || !other.TryGetComponent(out Health health) || health.IsDead)
                return;
            if (!ServiceLocator.TryGet(out SceneTravel travel))
                return;

            if (travel.Travel(gameObject.scene, targetScene, targetSpawnId, health.Current) &&
                other.TryGetComponent(out PlayerMovement movement))
                movement.enabled = false; // quieto durante el fundido
        }
    }
}
