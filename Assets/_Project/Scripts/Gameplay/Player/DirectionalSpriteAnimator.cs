using CronicasDeMexico.Data.Characters;
using CronicasDeMexico.Gameplay.Movement;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Player
{
    /// <summary>
    /// Elige el sprite según la dirección en pantalla (ocho direcciones) y si el personaje camina.
    /// Las direcciones del oeste usan las del este volteadas con la escala X del objeto y no con
    /// SpriteRenderer.flipX: URP no entrega flipX a las pasadas de sombra y profundidad, así que la sombra no se
    /// voltearía. Al moverse en vertical (Norte o Sur) conserva el lado anterior.
    /// </summary>
    public sealed class DirectionalSpriteAnimator : MonoBehaviour
    {
        // Por debajo de esta velocidad (m/s) se muestra la pose quieta.
        const float WalkSpeedThreshold = 0.1f;

        // Ritmo de la caminata frente a la marcha normal: más lento que la mitad o más rápido que 1,5 ya no parece andar.
        const float MinWalkPace = 0.5f;
        const float MaxWalkPace = 1.5f;

        /// <summary>Los pasos siguen a la velocidad (agua que frena, mando a medias): así los pies no patinan.</summary>
        public static float WalkPace(float speed, float cruiseSpeed) =>
            cruiseSpeed > 0f ? Mathf.Clamp(speed / cruiseSpeed, MinWalkPace, MaxWalkPace) : 1f;

        [SerializeField, Tooltip("Componente que implementa IPlanarMotion (PlayerMovement, NpcPatrol, EnemyChaser...).")]
        MonoBehaviour motionSource;

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] DirectionalSpriteSet spriteSet;

        [SerializeField, Tooltip("Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        [SerializeField, Tooltip("Opcional: reposo animado (respiración) en bucle. Si está vacío, la pose quieta del juego.")]
        DirectionalSpriteSet idleSet;

        IPlanarMotion movement;
        Transform spriteTransform;
        float walkTime;
        float idleTime;
        DirectionalSpriteSet actionSet;
        float actionTime;
        bool actionLoops;
        bool actionHolds;

        /// <summary>Juego de sprites en uso. Un personaje con varias poses (el Jaguar de Piedra) lo cambia según lo que hace.</summary>
        public DirectionalSpriteSet SpriteSet
        {
            get => spriteSet;
            set => spriteSet = value;
        }

        public DirectionalSpriteSet IdleSet
        {
            get => idleSet;
            set => idleSet = value;
        }

        /// <summary>Animación de acción que se muestra ahora por encima de caminar y reposo (null si ninguna).</summary>
        public DirectionalSpriteSet CurrentAction => actionSet;

        /// <summary>
        /// Muestra una animación de acción (zarpazo, golpe, galope, muerte). En bucle dura hasta Stop; si no, termina
        /// sola, salvo con hold, que se queda en el último cuadro (muerte).
        /// </summary>
        public void Play(DirectionalSpriteSet set, bool loop = false, bool hold = false)
        {
            if (set == null || (set == actionSet && (loop || hold)))
                return;
            actionSet = set;
            actionTime = 0f;
            actionLoops = loop;
            actionHolds = hold;
        }

        /// <summary>Quita la acción (o solo esa, si se indica) y vuelve a caminar o reposar.</summary>
        public void Stop(DirectionalSpriteSet set = null)
        {
            if (set == null || set == actionSet)
                actionSet = null;
        }

        void Awake()
        {
            movement = motionSource as IPlanarMotion;
            if (motionSource != null && movement == null)
                Debug.LogError($"[{nameof(DirectionalSpriteAnimator)}] {motionSource.GetType().Name} no implementa IPlanarMotion.", this);

            if (spriteRenderer != null)
                spriteTransform = spriteRenderer.transform;
        }

        // Al arrastrar un GameObject al campo, Unity asigna su primer MonoBehaviour: se busca el que sirve.
        void OnValidate()
        {
            if (motionSource != null && motionSource is not IPlanarMotion)
                motionSource = motionSource.GetComponent<IPlanarMotion>() as MonoBehaviour;
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (movement == null || spriteTransform == null || spriteSet == null)
                return;

            var cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            var direction = DirectionQuantizer.FromWorld(movement.FacingDirection, cameraYaw);
            var westward = DirectionQuantizer.IsWestward(direction);
            var clipIndex = (int)(westward ? DirectionQuantizer.Mirror(direction) : direction);

            Sprite sprite;
            if (actionSet != null)
            {
                actionTime += Time.deltaTime;
                if (actionLoops)
                    sprite = actionSet.GetWalkFrame(clipIndex, actionTime);
                else
                {
                    sprite = actionSet.GetOnceFrame(clipIndex, actionTime);
                    if (!actionHolds && actionTime >= actionSet.Duration)
                        actionSet = null;
                }
            }
            else if (movement.PlanarVelocity.sqrMagnitude > WalkSpeedThreshold * WalkSpeedThreshold)
            {
                walkTime += Time.deltaTime * WalkPace(movement.PlanarVelocity.magnitude, movement.CruiseSpeed);
                idleTime = 0f;
                sprite = spriteSet.GetWalkFrame(clipIndex, walkTime);
            }
            else
            {
                walkTime = 0f;
                idleTime += Time.deltaTime;
                sprite = idleSet != null ? idleSet.GetWalkFrame(clipIndex, idleTime) : spriteSet.GetIdle(clipIndex);
            }

            if (spriteRenderer.sprite != sprite)
                spriteRenderer.sprite = sprite;

            if (direction is ScreenDirection.North or ScreenDirection.South)
                return;

            // Solo el signo: un sprite agrandado (el Espíritu del Jaguar, ×2) conserva su tamaño al voltearse.
            var scale = spriteTransform.localScale;
            var scaleX = (westward ? -1f : 1f) * Mathf.Abs(scale.x);
            if (!Mathf.Approximately(scale.x, scaleX))
                spriteTransform.localScale = new Vector3(scaleX, scale.y, scale.z);
        }
    }
}
