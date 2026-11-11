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
        const float BossTrailHold = 0.4f;
        const float BossTrailSpeed = 60f;

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
        MessageBox messageBox;
        DamageNumberLayer damageNumbers;
        VisualElement bossTrail;
        VisualElement holdContainer;
        HoldRing holdRing;
        VisualElement lockReticle;
        float bossPercent = 100f;
        float bossTrailPercent = 100f;
        float bossTrailHoldUntil;
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

        /// <summary>Cuadro de mensajes y diálogos (para las pruebas).</summary>
        public MessageBox Messages => messageBox;

        /// <summary>Números de daño a la vista (para las pruebas).</summary>
        public int DamageNumbersShown => damageNumbers?.ActiveCount ?? 0;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            prompt = root.Q<Label>("prompt");
            messageBox = new MessageBox(root);
            damageNumbers = new DamageNumberLayer(root);
            bossTrail = root.Q("boss-trail");
            holdContainer = root.Q("hold-ring");
            if (holdContainer != null)
            {
                holdRing = new HoldRing();
                holdContainer.Clear();
                holdContainer.Add(holdRing);
            }
            lockReticle = new VisualElement { pickingMode = PickingMode.Ignore };
            lockReticle.AddToClassList("hud-lock");
            lockReticle.style.display = DisplayStyle.None;
            root.Q("damage-numbers")?.Add(lockReticle);
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
            messageBox?.Dispose();
            damageNumbers?.Dispose();
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

            var unscaled = Time.unscaledDeltaTime;
            messageBox.Tick(unscaled);
            damageNumbers.Tick(unscaled);
            UpdateBossTrail(unscaled);
            UpdateTargeting();

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

        void ShowMessage(string text) => messageBox.Show(text);

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

        void ShowBossHealth(HealthChange change)
        {
            bossPercent = change.Max > 0 ? 100f * change.Current / change.Max : 0f;
            bossFill.style.width = Length.Percent(bossPercent);
            // Curación o jefe nuevo: la estela salta; daño: espera un momento y baja despacio.
            if (bossPercent >= bossTrailPercent)
                bossTrailPercent = bossPercent;
            else
                bossTrailHoldUntil = Time.unscaledTime + BossTrailHold;
            ApplyBossTrail();
        }

        // Anillo de la pulsación larga y mira del objetivo fijado (Core/Feedback/TargetingFeedback).
        void UpdateTargeting()
        {
            var hold = HoldProgress.Value;
            if (holdContainer != null)
            {
                holdContainer.EnableInClassList(HiddenClass, hold < 0f);
                if (hold >= 0f)
                    holdRing.Progress = hold;
            }

            var target = LockOnMarker.Target;
            var position = Vector2.zero;
            var visible = target != null && DamageNumberLayer.TryProject(lockReticle, Camera.main, target.position + Vector3.up * 0.8f, out position);
            lockReticle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            lockReticle.style.left = position.x;
            lockReticle.style.top = position.y;
        }

        /// <summary>La mira del objetivo fijado está a la vista (para las pruebas).</summary>
        public bool LockReticleShown => lockReticle != null && lockReticle.style.display.value == DisplayStyle.Flex;

        /// <summary>El anillo de la pulsación larga está a la vista (para las pruebas).</summary>
        public bool HoldRingShown => holdContainer != null && !holdContainer.ClassListContains(HiddenClass);

        void UpdateBossTrail(float unscaledDeltaTime)
        {
            if (bossTrailPercent <= bossPercent || Time.unscaledTime < bossTrailHoldUntil)
                return;
            bossTrailPercent = Mathf.MoveTowards(bossTrailPercent, bossPercent, BossTrailSpeed * unscaledDeltaTime);
            ApplyBossTrail();
        }

        void ApplyBossTrail()
        {
            if (bossTrail != null)
                bossTrail.style.width = Length.Percent(bossTrailPercent);
        }

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
