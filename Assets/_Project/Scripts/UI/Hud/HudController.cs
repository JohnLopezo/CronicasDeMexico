using CronicasDeMexico.Core.Events;
using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Core.Narrative;
using CronicasDeMexico.Core.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Muestra la vida del jugador, el objetivo de la misión activa, el aviso de interacción, los mensajes, la barra
    /// del jefe cuando hay uno en combate y, opcionalmente, un contador de FPS. No conoce al jugador: recibe los datos por canales de eventos, y el
    /// objetivo, de QuestService.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        const string HiddenClass = "hidden";
        // El texto de FPS se actualiza dos veces por segundo para no generar una cadena cada frame.
        const float FpsRefreshInterval = 0.5f;

        [SerializeField] StringEventChannel promptChannel;
        [SerializeField] StringEventChannel messageChannel;
        [SerializeField] HealthEventChannel healthChannel;

        [SerializeField, Tooltip("Nombre del jefe: lo muestra con su barra; vacío la oculta.")]
        StringEventChannel bossNameChannel;

        [SerializeField, Tooltip("Vida del jefe.")]
        HealthEventChannel bossHealthChannel;

        [SerializeField, Tooltip("Muestra FPS y milisegundos por frame en la esquina superior derecha.")]
        bool showFps = true;

        Label prompt;
        Label message;
        Label fps;
        VisualElement health;
        VisualElement healthFill;
        Label healthText;
        VisualElement objective;
        Label objectiveTitle;
        Label objectiveText;
        VisualElement boss;
        Label bossName;
        VisualElement bossFill;
        VisualElement target;
        Label targetName;
        VisualElement targetShield;
        VisualElement targetWeak;
        Label targetBroken;
        QuestService quests;
        int frameCount;
        float fpsTimer;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            prompt = root.Q<Label>("prompt");
            message = root.Q<Label>("message");
            fps = root.Q<Label>("fps");
            health = root.Q("health");
            healthFill = root.Q("health-fill");
            healthText = root.Q<Label>("health-text");
            objective = root.Q("objective");
            objectiveTitle = root.Q<Label>("objective-title");
            objectiveText = root.Q<Label>("objective-text");
            boss = root.Q("boss");
            bossName = root.Q<Label>("boss-name");
            bossFill = root.Q("boss-fill");
            target = root.Q("target");
            targetName = root.Q<Label>("target-name");
            targetShield = root.Q("target-shield");
            targetWeak = root.Q("target-weak");
            targetBroken = root.Q<Label>("target-broken");
            ShowBreak(BreakReadout.Current);
            BreakReadout.Changed += ShowBreak;

            ShowPrompt(null);
            ShowBoss(null);
            ShowMessage(null);
            fps.EnableInClassList(HiddenClass, !showFps);
            ConnectQuests();

            if (promptChannel != null)
                promptChannel.Subscribe(ShowPrompt);
            if (messageChannel != null)
                messageChannel.Subscribe(ShowMessage);
            if (healthChannel != null)
                healthChannel.Subscribe(ShowHealth);
            if (bossNameChannel != null)
                bossNameChannel.Subscribe(ShowBoss);
            if (bossHealthChannel != null)
                bossHealthChannel.Subscribe(ShowBossHealth);
        }

        void OnDisable()
        {
            BreakReadout.Changed -= ShowBreak;
            if (promptChannel != null)
                promptChannel.Unsubscribe(ShowPrompt);
            if (messageChannel != null)
                messageChannel.Unsubscribe(ShowMessage);
            if (healthChannel != null)
                healthChannel.Unsubscribe(ShowHealth);
            if (bossNameChannel != null)
                bossNameChannel.Unsubscribe(ShowBoss);
            if (bossHealthChannel != null)
                bossHealthChannel.Unsubscribe(ShowBossHealth);
            if (quests != null)
                quests.Changed -= ShowObjective;
            quests = null;
        }

        void Update()
        {
            // Al pulsar Play desde una escena de juego, Boot (que registra QuestService) se carga después.
            if (quests == null)
                ConnectQuests();

            if (!showFps)
                return;

            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer < FpsRefreshInterval)
                return;

            fps.text = $"{frameCount / fpsTimer:0} FPS  ·  {fpsTimer * 1000f / frameCount:0.0} ms";
            frameCount = 0;
            fpsTimer = 0f;
        }

        void ConnectQuests()
        {
            if (!ServiceLocator.TryGet(out quests))
            {
                objective.AddToClassList(HiddenClass);
                return;
            }

            quests.Changed += ShowObjective;
            ShowObjective();
        }

        void ShowObjective()
        {
            var visible = quests.TryGetCurrentObjective(out var title, out var description);
            if (visible)
            {
                objectiveTitle.text = title;
                objectiveText.text = description;
            }
            objective.EnableInClassList(HiddenClass, !visible);
        }

        void ShowPrompt(string text) => SetText(prompt, text);

        void ShowMessage(string text) => SetText(message, text);

        // La barra se muestra con el primer dato: escenas sin jugador con vida no la enseñan.
        void ShowHealth(HealthChange change)
        {
            health.RemoveFromClassList(HiddenClass);
            healthFill.style.width = Length.Percent(change.Max > 0 ? 100f * change.Current / change.Max : 0f);
            healthText.text = $"{change.Current}/{change.Max}";
        }

        void ShowBoss(string displayName)
        {
            var visible = !string.IsNullOrEmpty(displayName);
            if (visible)
                bossName.text = displayName;
            boss.EnableInClassList(HiddenClass, !visible);
        }

        void ShowBossHealth(HealthChange change) =>
            bossFill.style.width = Length.Percent(change.Max > 0 ? 100f * change.Current / change.Max : 0f);

        // Solo cambia al golpear o al romperse el escudo: crear los elementos aquí no pesa.
        void ShowBreak(BreakInfo? info)
        {
            // HUD de una escena sin el panel (uxml antiguo): no hay nada que mostrar.
            if (target == null)
                return;
            target.EnableInClassList(HiddenClass, info == null);
            if (info is not { } data)
                return;

            targetName.text = data.Name;
            targetShield.Clear();
            for (var i = 0; i < data.MaxShield; i++)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList("hud-target__pip");
                pip.EnableInClassList("hud-target__pip--empty", data.Broken || i >= data.Shield);
                targetShield.Add(pip);
            }

            targetWeak.Clear();
            foreach (var weakness in data.Weaknesses)
            {
                var chip = new Label(weakness) { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("hud-target__chip");
                chip.AddToClassList($"hud-target__chip--{weakness.ToLowerInvariant()}");
                targetWeak.Add(chip);
            }
            targetBroken.EnableInClassList(HiddenClass, !data.Broken);
        }

        static void SetText(Label label, string text)
        {
            var visible = !string.IsNullOrEmpty(text);
            if (visible)
                label.text = text;
            label.EnableInClassList(HiddenClass, !visible);
        }
    }
}
