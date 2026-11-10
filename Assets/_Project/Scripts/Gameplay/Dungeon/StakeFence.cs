using CronicasDeMexico.Gameplay.Combat;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Cerca de estacas: bloquea el paso (su collider sólido va aparte) y daña a quien la toque. Este collider es el
    /// trigger, algo más grande que el sólido; el golpe empuja desde la cerca y la invulnerabilidad lo dosifica.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class StakeFence : MonoBehaviour
    {
        [SerializeField, Min(1)] int damage = 5;

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            var health = other.GetComponentInParent<Health>();
            if (health != null)
                health.TryDamage(damage, transform.position);
        }
    }
}
