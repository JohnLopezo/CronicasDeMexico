using CronicasDeMexico.Core.Events;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Enemies
{
    /// <summary>
    /// Arena de pruebas de los jefes: guarda los seis TrialBoss (inactivos) y saca uno al centro de la arena cuando el
    /// jugador pisa su placa (BossTrialPad). Solo uno está activo a la vez; elegir otro retira al anterior.
    /// </summary>
    public sealed class BossTrial : MonoBehaviour
    {
        [SerializeField] TrialBoss[] bosses;
        [SerializeField, Tooltip("Centro de la arena: ahí aparece el jefe elegido.")] Transform spawn;
        [SerializeField] StringEventChannel bossNameChannel;

        public TrialBoss Active { get; private set; }
        public int Count => bosses?.Length ?? 0;

        public TrialBoss Boss(int index) => bosses[index];

        public void Summon(int index)
        {
            if (bosses == null || index < 0 || index >= bosses.Length || bosses[index] == Active)
                return;
            foreach (var boss in bosses)
                if (boss != null)
                    boss.gameObject.SetActive(false);
            if (bossNameChannel != null)
                bossNameChannel.Raise(null); // la barra vuelve al despertar al nuevo
            Active = bosses[index];
            Active.gameObject.SetActive(true);
            Active.ResetBoss(spawn != null ? spawn.position : transform.position);
        }
    }
}
