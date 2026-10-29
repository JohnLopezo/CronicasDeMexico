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

        [SerializeField, Tooltip("Componente que implementa IPlanarMotion (PlayerMovement, NpcPatrol, EnemyChaser...).")]
        MonoBehaviour motionSource;

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] DirectionalSpriteSet spriteSet;

        [SerializeField, Tooltip("Si está vacía se usa Camera.main al iniciar.")]
        Transform cameraTransform;

        IPlanarMotion movement;
        Transform spriteTransform;
        float walkTime;

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
            if (movement.PlanarVelocity.sqrMagnitude > WalkSpeedThreshold * WalkSpeedThreshold)
            {
                walkTime += Time.deltaTime;
                sprite = spriteSet.GetWalkFrame(clipIndex, walkTime);
            }
            else
            {
                walkTime = 0f;
                sprite = spriteSet.GetIdle(clipIndex);
            }

            if (spriteRenderer.sprite != sprite)
                spriteRenderer.sprite = sprite;

            if (direction is ScreenDirection.North or ScreenDirection.South)
                return;

            var scaleX = westward ? -1f : 1f;
            var scale = spriteTransform.localScale;
            if (!Mathf.Approximately(scale.x, scaleX))
                spriteTransform.localScale = new Vector3(scaleX, scale.y, scale.z);
        }
    }
}
