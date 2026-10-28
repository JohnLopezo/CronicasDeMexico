using UnityEngine;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Anima en bucle un grupo de SpriteRenderers con los mismos cuadros (p. ej. todos los tiles de agua). Un solo
    /// componente para todo el grupo: solo cambia los sprites cuando cambia el cuadro.
    /// </summary>
    public sealed class SpriteFrameCycler : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField] SpriteRenderer[] renderers;

        [SerializeField, Min(0.1f)] float framesPerSecond = 4f;

        int currentFrame = -1;

        public int FrameCount => frames != null ? frames.Length : 0;

        public void Configure(Sprite[] newFrames, SpriteRenderer[] newRenderers)
        {
            frames = newFrames;
            renderers = newRenderers;
            currentFrame = -1;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || renderers == null)
                return;

            var frame = (int)(Time.time * framesPerSecond) % frames.Length;
            if (frame == currentFrame)
                return;

            currentFrame = frame;
            var sprite = frames[frame];
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].sprite = sprite;
        }
    }
}
