using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Gameplay.Combat;
using CronicasDeMexico.Gameplay.Player;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Derrota en la mazmorra (Diseno.md §9): si hay un santuario de Memoria donde se descansó, el jugador vuelve a él
    /// (aunque esté en otro piso) en lugar de reaparecer en el sitio. Viajar recarga la escena, así que los enemigos
    /// normales vuelven; la historia, las misiones y los jefes vencidos (banderas) se conservan. Sin punto de control,
    /// reaparece como fuera de la mazmorra (PlayerDamageResponse). Va en cada piso, fuera del jugador.
    /// </summary>
    public sealed class DefeatReturn : MonoBehaviour
    {
        [SerializeField] Health player;

        [SerializeField, Min(0f), Tooltip("Segundos tumbado antes de volver al santuario.")]
        float returnDelay = 1.5f;

        [SerializeField, Range(0.01f, 1f), Tooltip("Penalización: parte de la vida máxima con la que se vuelve (1 = completa).")]
        float returnHealthFraction = 1f;

        PlayerDamageResponse damageResponse;
        float timer = -1f;

        public Health Player
        {
            get => player;
            set => player = value;
        }

        void Awake()
        {
            if (player != null)
                damageResponse = player.GetComponent<PlayerDamageResponse>();
        }

        void OnEnable()
        {
            if (player != null)
                player.Died += OnDied;
        }

        void OnDisable()
        {
            if (player != null)
                player.Died -= OnDied;
        }

        void OnDied()
        {
            if (!ServiceLocator.TryGet(out PlayerCheckpoint checkpoint) || !checkpoint.HasCheckpoint ||
                !ServiceLocator.TryGet(out SceneTravel _))
                return; // reaparece en el sitio

            if (damageResponse != null)
                damageResponse.RespawnInPlace = false;
            timer = returnDelay;
        }

        void Update()
        {
            if (timer < 0f)
                return;
            timer -= Time.deltaTime;
            if (timer >= 0f)
                return;

            var checkpoint = ServiceLocator.Get<PlayerCheckpoint>();
            var health = Mathf.CeilToInt(player.Max * returnHealthFraction);
            if (!ServiceLocator.TryGet(out SceneTravel travel) ||
                !travel.Travel(gameObject.scene, checkpoint.Scene, checkpoint.SpawnId, health))
            {
                if (damageResponse != null)
                    damageResponse.RespawnInPlace = true; // no se pudo viajar: reaparece en el sitio
            }
        }
    }
}
