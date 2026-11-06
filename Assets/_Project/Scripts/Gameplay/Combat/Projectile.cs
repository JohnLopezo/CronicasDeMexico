using UnityEngine;

namespace CronicasDeMexico.Gameplay.Combat
{
    /// <summary>
    /// Proyectil en línea recta (los fragmentos de símbolos del eco borrado). Lo reutiliza su lanzador (pooling): se
    /// activa con Launch y se apaga solo al tocar al objetivo, chocar con el escenario o agotar su tiempo. El impacto con
    /// el objetivo se comprueba por distancia, sin física; con el escenario, con un rayo por paso.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        const float HitRadius = 0.45f;

        [SerializeField, Tooltip("Lo que lo detiene (muros, columnas, vacío).")]
        LayerMask obstacleLayers = 1; // Default

        Health target;
        Vector3 velocity;
        int damage;
        float lifetime;

        public bool IsFlying => gameObject.activeSelf;

        /// <summary>Sale desde <paramref name="origin"/> con la velocidad dada hacia el objetivo.</summary>
        public void Launch(Vector3 origin, Vector3 flyVelocity, int hitDamage, Health aim, float seconds)
        {
            transform.position = origin;
            velocity = flyVelocity;
            damage = hitDamage;
            target = aim;
            lifetime = seconds;
            gameObject.SetActive(true);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            lifetime -= deltaTime;
            var step = velocity * deltaTime;
            if (lifetime <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            // El jugador también está en Default: si el rayo lo toca a él, cuenta como impacto.
            if (Physics.Raycast(transform.position, step.normalized, out var hit, step.magnitude, obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (target != null && hit.collider.GetComponentInParent<Health>() == target)
                    target.TryDamage(damage, transform.position);
                gameObject.SetActive(false);
                return;
            }

            transform.position += step;
            if (target == null || target.IsDead)
                return;

            // El objetivo se mide a la altura del pecho, en el plano.
            var offset = transform.position - target.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > HitRadius * HitRadius)
                return;

            target.TryDamage(damage, transform.position);
            gameObject.SetActive(false);
        }
    }
}
