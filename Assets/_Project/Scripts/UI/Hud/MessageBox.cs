using CronicasDeMexico.Core.Feedback;
using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Cuadro de mensajes y diálogos del HUD. Separa el nombre ("<b>Nombre</b>\n" al principio, como lo publica
    /// DialogueInteractable) en una placa, pone el retrato si SpeakerPortrait lo trae y escribe el texto letra a letra
    /// según ComfortSettings. Las letras que faltan van transparentes (alpha 0) para que el texto no se recoloque al
    /// escribirse. La primera pulsación de Hablar lo completa (MessageReveal) y entonces aparece ▼.
    /// </summary>
    public sealed class MessageBox
    {
        const string HiddenClass = "hidden";
        const string SpeakerOpen = "<b>";
        const string SpeakerClose = "</b>\n";
        const string Transparent = "<alpha=#00>";

        readonly VisualElement box;
        readonly VisualElement portrait;
        readonly Label speaker;
        readonly Label text;
        readonly VisualElement next;
        string body = string.Empty;
        int shown;
        float progress;
        float charactersPerSecond;
        bool revealing;

        public MessageBox(VisualElement root)
        {
            box = root.Q("message-box");
            portrait = root.Q("message-portrait");
            speaker = root.Q<Label>("message-speaker");
            text = root.Q<Label>("message");
            next = root.Q("message-next");
            MessageReveal.CompleteRequested += Complete;
        }

        public bool IsVisible => box != null && !box.ClassListContains(HiddenClass);

        /// <summary>Texto visible ahora (sin la parte transparente). Para las pruebas.</summary>
        public string VisibleText => body.Substring(0, shown);

        public string Speaker => speaker == null || speaker.ClassListContains(HiddenClass) ? null : speaker.text;

        public bool HasPortrait => portrait != null && !portrait.ClassListContains(HiddenClass);

        public void Dispose()
        {
            MessageReveal.CompleteRequested -= Complete;
            SetRevealing(false);
        }

        public void Show(string message)
        {
            if (box == null)
                return;
            if (string.IsNullOrEmpty(message))
            {
                box.AddToClassList(HiddenClass);
                SetRevealing(false);
                SpeakerPortrait.Clear();
                return;
            }

            string name = null;
            body = message;
            var close = message.StartsWith(SpeakerOpen) ? message.IndexOf(SpeakerClose, System.StringComparison.Ordinal) : -1;
            if (close > 0)
            {
                name = message.Substring(SpeakerOpen.Length, close - SpeakerOpen.Length);
                body = message.Substring(close + SpeakerClose.Length);
            }

            speaker.EnableInClassList(HiddenClass, string.IsNullOrEmpty(name));
            speaker.text = name ?? string.Empty;

            var sprite = name != null && SpeakerPortrait.Name == name ? SpeakerPortrait.Sprite : null;
            portrait.EnableInClassList(HiddenClass, sprite == null);
            portrait.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);

            box.RemoveFromClassList(HiddenClass);
            charactersPerSecond = ComfortSettings.CharactersPerSecond;
            shown = 0;
            progress = 0f;
            if (charactersPerSecond <= 0f)
            {
                Complete();
                return;
            }
            SetRevealing(true);
            Render();
        }

        /// <summary>Avanza la escritura; con tiempo real, para que el hitstop o la cámara lenta no la frenen.</summary>
        public void Tick(float unscaledDeltaTime)
        {
            if (!revealing)
                return;
            progress += unscaledDeltaTime * charactersPerSecond;
            var count = Mathf.Min(body.Length, (int)progress);
            if (count == shown)
                return;
            shown = count;
            if (shown >= body.Length)
                Complete();
            else
                Render();
        }

        void Complete()
        {
            SetRevealing(false);
            shown = body.Length;
            Render();
        }

        void SetRevealing(bool value)
        {
            revealing = value;
            MessageReveal.IsRevealing = value;
            next?.EnableInClassList(HiddenClass, value || !IsVisible);
        }

        void Render() =>
            text.text = shown >= body.Length ? body : string.Concat(body.Substring(0, shown), Transparent, body.Substring(shown));
    }
}
