using CronicasDeMexico.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Muestra la vida del jugador, el aviso de interacción, los mensajes y, opcionalmente, un contador de FPS.
    /// No conoce al jugador: recibe los datos por canales de eventos.
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

        [SerializeField, Tooltip("Muestra FPS y milisegundos por frame en la esquina superior derecha.")]
        bool showFps = true;

        Label prompt;
        Label message;
        Label fps;
        VisualElement health;
        VisualElement healthFill;
        Label healthText;
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

            ShowPrompt(null);
            ShowMessage(null);
            fps.EnableInClassList(HiddenClass, !showFps);

            if (promptChannel != null)
                promptChannel.Subscribe(ShowPrompt);
            if (messageChannel != null)
                messageChannel.Subscribe(ShowMessage);
            if (healthChannel != null)
                healthChannel.Subscribe(ShowHealth);
        }

        void OnDisable()
        {
            if (promptChannel != null)
                promptChannel.Unsubscribe(ShowPrompt);
            if (messageChannel != null)
                messageChannel.Unsubscribe(ShowMessage);
            if (healthChannel != null)
                healthChannel.Unsubscribe(ShowHealth);
        }

        void Update()
        {
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

        void ShowPrompt(string text) => SetText(prompt, text);

        void ShowMessage(string text) => SetText(message, text);

        // La barra se muestra con el primer dato: escenas sin jugador con vida no la enseñan.
        void ShowHealth(HealthChange change)
        {
            health.RemoveFromClassList(HiddenClass);
            healthFill.style.width = Length.Percent(change.Max > 0 ? 100f * change.Current / change.Max : 0f);
            healthText.text = $"{change.Current}/{change.Max}";
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
