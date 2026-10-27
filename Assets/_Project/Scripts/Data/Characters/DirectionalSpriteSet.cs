using System;
using UnityEngine;

namespace CronicasDeMexico.Data.Characters
{
    /// <summary>
    /// Sprites de un personaje en cinco direcciones de pantalla: Norte, Noreste, Este, Sureste y Sur, en ese orden.
    /// Las tres direcciones del oeste se obtienen volteando las del este.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Personajes/Sprites direccionales", fileName = "DSS_Nuevo")]
    public sealed class DirectionalSpriteSet : ScriptableObject
    {
        public const int DirectionCount = 5;

        [Serializable]
        public struct DirectionClip
        {
            [SerializeField] Sprite idle;
            [SerializeField] Sprite[] walk;

            public DirectionClip(Sprite idle, Sprite[] walk)
            {
                this.idle = idle;
                this.walk = walk;
            }

            public Sprite Idle => idle;
            public Sprite[] Walk => walk;
        }

        [SerializeField, Tooltip("Orden: Norte, Noreste, Este, Sureste, Sur.")]
        DirectionClip[] clips = new DirectionClip[DirectionCount];

        [SerializeField, Min(1f), Tooltip("Cuadros por segundo de la caminata.")]
        float walkFramesPerSecond = 8f;

        public Sprite GetIdle(int direction) => clips[direction].Idle;

        /// <summary>Cuadro de caminata para el tiempo dado; sin cuadros de caminata devuelve la pose quieta.</summary>
        public Sprite GetWalkFrame(int direction, float walkTime)
        {
            var walk = clips[direction].Walk;
            if (walk == null || walk.Length == 0)
                return clips[direction].Idle;

            return walk[(int)(walkTime * walkFramesPerSecond) % walk.Length];
        }

        public void SetClips(DirectionClip[] value)
        {
            if (value == null || value.Length != DirectionCount)
                throw new ArgumentException($"Se esperaban {DirectionCount} direcciones.", nameof(value));
            clips = value;
        }

        void OnValidate()
        {
            if (clips == null || clips.Length != DirectionCount)
                Array.Resize(ref clips, DirectionCount);
        }
    }
}
