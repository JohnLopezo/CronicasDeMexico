using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Travel
{
    /// <summary>
    /// Punto de llegada de un viaje entre escenas (ScenePortal → SceneTravel). Si el viaje en curso viene a este punto,
    /// coloca al jugador aquí, le devuelve la vida que traía y lo hace reaparecer aquí si muere.
    /// Actúa en Awake, después del de Health y PlayerDamageResponse (orden de ejecución) y antes de cualquier Start,
    /// para que la cámara empiece ya mirando al jugador. Al pulsar Play en la escena no hay viaje y no hace nada.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] string spawnId;

        public string SpawnId
        {
            get => spawnId;
            set => spawnId = value;
        }

        void Awake()
        {
            if (!ServiceLocator.TryGet(out SceneTravel travel) ||
                !travel.TryTakeArrival(gameObject.scene.name, spawnId, out var arrival))
                return;

            var player = GameObject.FindWithTag("Player");
            if (player == null || player.scene != gameObject.scene)
                return;

            // El CharacterController sobrescribe la posición si sigue activo al moverlo.
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            player.transform.SetPositionAndRotation(transform.position, player.transform.rotation);
            if (controller != null)
                controller.enabled = true;

            if (player.TryGetComponent(out Health health))
                health.CarryOver(arrival.Health);
            if (player.TryGetComponent(out PlayerDamageResponse damageResponse))
                damageResponse.SetRespawnPoint(transform.position);
        }
    }
}
