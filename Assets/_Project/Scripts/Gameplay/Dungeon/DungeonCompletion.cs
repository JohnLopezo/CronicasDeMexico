using System;
using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Data.Narrative;
using CronicasDeMexico.Data.Rewards;
using UnityEngine;

namespace CronicasDeMexico.Gameplay.Dungeon
{
    /// <summary>
    /// Cierre de una mazmorra (Docs/Sistemas/Sistema_Recompensas.md §4.2 y §12), en la arena del jefe final:
    /// - Al liberar al jefe (su bandera) entrega lo garantizado: la Bendición principal (en el arco olmeca, la de la
    ///   Tierra), el objeto narrativo y la experiencia. Empieza la misión de vuelta a la comunidad.
    /// - Al abrir la ofrenda final (RewardChest llama a Finish) calcula la nota (C, B, A, S) según las ofrendas ocultas
    ///   encontradas y si se cayó en la incursión, entrega la recompensa de esa nota y pide a la interfaz la elección
    ///   de Bendición secundaria (si aún no se eligió) y la pantalla de resultados.
    /// Todo se entrega una sola vez (RewardService). La nota nunca bloquea nada necesario para la historia.
    /// </summary>
    public sealed class DungeonCompletion : MonoBehaviour
    {
        public const string FallenFlag = "incursion.caido";

        [SerializeField, Tooltip("Id de la cultura (claves de recompensa).")] string cultureId = "olmeca";
        [SerializeField] string title = "La ciudad bajo la selva";
        [SerializeField, Tooltip("Bandera de jefe liberado.")] string bossFreedFlag;
        [SerializeField, Tooltip("Lo garantizado al liberar al jefe (Bendición principal incluida).")] RewardBundle bossReward;
        [SerializeField, Tooltip("Banderas de las ofrendas ocultas de la mazmorra (secretos).")] string[] secretFlags = Array.Empty<string>();
        [SerializeField, Tooltip("Recompensa por nota: C, B, A, S.")] RewardBundle[] gradeRewards = new RewardBundle[4];
        [SerializeField, Tooltip("Misión que empieza al liberar al jefe (volver a la comunidad).")] QuestData followQuest;
        [SerializeField, Tooltip("Canal para abrir pantallas del menú (MenuTabs).")] StringEventChannel menuChannel;

        StoryState story;

        public string CultureId => cultureId;

        public void Configure(string culture, string dungeonTitle, string freedFlag, RewardBundle boss, string[] secrets,
            RewardBundle[] grades, QuestData quest, StringEventChannel channel)
        {
            cultureId = culture;
            title = dungeonTitle;
            bossFreedFlag = freedFlag;
            bossReward = boss;
            secretFlags = secrets ?? Array.Empty<string>();
            gradeRewards = grades ?? new RewardBundle[4];
            followQuest = quest;
            menuChannel = channel;
        }

        // Al pulsar Play desde una escena de juego, Boot (que registra StoryState) se carga después: se espera a él.
        void Update()
        {
            if (!ServiceLocator.TryGet(out story))
                return;
            enabled = false;
            story.FlagChanged += OnFlagChanged;
            if (story.HasFlag(bossFreedFlag))
                GrantBoss(); // por si se liberó en una sesión anterior sin llegar a entregarse (no duplica)
        }

        void OnDestroy()
        {
            if (story != null)
                story.FlagChanged -= OnFlagChanged;
        }

        void OnFlagChanged(string flag)
        {
            if (flag == bossFreedFlag && story.HasFlag(flag))
                GrantBoss();
        }

        void GrantBoss()
        {
            if (ServiceLocator.TryGet(out RewardService rewards) && bossReward != null)
                rewards.Grant(bossReward.ToPackage(rewards.Random), "jefe." + cultureId);
            if (followQuest != null && ServiceLocator.TryGet(out QuestService quests) &&
                quests.GetStatus(followQuest.Id) == QuestStatus.NotStarted)
                quests.Start(followQuest);
        }

        /// <summary>Nota según secretos encontrados y si se cayó: S todo y sin caer, A falta uno, B algo, C lo demás.</summary>
        public static string Grade(int secretsFound, int secretsTotal, bool fallen)
        {
            var score = secretsFound + (fallen ? 0 : 1);
            var total = secretsTotal + 1;
            return score >= total ? "S" : score >= total - 1 ? "A" : score >= 1 ? "B" : "C";
        }

        /// <summary>Lo llama la ofrenda final al abrirse: nota, su recompensa y las pantallas de cierre.</summary>
        public void Finish()
        {
            if (!ServiceLocator.TryGet(out RewardService rewards))
                return;
            var found = 0;
            foreach (var flag in secretFlags)
                if (rewards.Story != null && rewards.Story.HasFlag(flag))
                    found++;
            var fallen = rewards.Story != null && rewards.Story.HasFlag(FallenFlag);
            var grade = Grade(found, secretFlags.Length, fallen);

            var result = new DungeonResult { Title = title, Grade = grade, SecretsFound = found, SecretsTotal = secretFlags.Length, Fallen = fallen };
            var bundle = gradeRewards.Length == 4 ? gradeRewards["CBAS".IndexOf(grade, StringComparison.Ordinal)] : null;
            if (bundle != null)
            {
                var package = bundle.ToPackage(rewards.Random);
                if (rewards.Grant(package, "nota." + cultureId))
                    result.Rewards = package;
            }
            rewards.LastResult = result;

            if (menuChannel != null)
                menuChannel.Raise(string.IsNullOrEmpty(rewards.Progression.SecondaryBlessing) ? MenuTabs.BlessingChoice : MenuTabs.Results);
        }
    }
}
