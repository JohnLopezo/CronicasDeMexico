using UnityEngine;

namespace CronicasDeMexico.Gameplay.Movement
{
    /// <summary>
    /// Personaje que el terreno puede frenar (agua baja y lodo del Pantano). SlowZone se lo recuerda en cada paso de
    /// física mientras está dentro; en cuanto deja de hacerlo, recupera la velocidad. Así no depende de OnTriggerExit, que
    /// Unity no envía si el personaje sale de golpe (reaparecer, teletransportarse).
    /// </summary>
    public interface ISlowable
    {
        void ApplySlow(float speedFactor);
    }

    /// <summary>Recuerda la zona lenta más reciente que cubre a un personaje (ISlowable).</summary>
    public struct SlowTracker
    {
        // Algo más de un paso de física: si en ese tiempo nadie lo frena, ya salió.
        const float Grace = 0.05f;

        float factor;
        float until;

        /// <summary>Multiplicador de velocidad actual (1 fuera de toda zona).</summary>
        public float Multiplier => Time.time <= until ? factor : 1f;

        public void Apply(float speedFactor)
        {
            // Si se solapan zonas en el mismo paso, manda la más lenta.
            factor = Time.time <= until ? Mathf.Min(factor, speedFactor) : speedFactor;
            until = Time.time + Time.fixedDeltaTime + Grace;
        }
    }
}
