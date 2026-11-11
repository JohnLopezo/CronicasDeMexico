using System;
using CronicasDeMexico.Core.Feedback;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Opciones de comodidad (ComfortSettings) como filas de botones: sacudida de cámara, números de daño y velocidad
    /// del texto. La usan el panel de opciones del juego y el del menú de inicio, cada uno con sus clases.
    /// </summary>
    public static class ComfortOptionsView
    {
        public static void Build(VisualElement container, string titleClass, string rowClass, string buttonClass, string selectedClass)
        {
            container.Clear();
            Row(container, titleClass, rowClass, buttonClass, selectedClass, "Sacudida de cámara",
                new[] { "Sí", "No" }, () => ComfortSettings.ScreenShake ? 0 : 1, i => ComfortSettings.ScreenShake = i == 0);
            Row(container, titleClass, rowClass, buttonClass, selectedClass, "Números de daño",
                new[] { "Sí", "No" }, () => ComfortSettings.DamageNumbers ? 0 : 1, i => ComfortSettings.DamageNumbers = i == 0);
            Row(container, titleClass, rowClass, buttonClass, selectedClass, "Velocidad del texto",
                new[] { "Lento", "Normal", "Rápido", "Al instante" }, () => (int)ComfortSettings.TextSpeed,
                i => ComfortSettings.TextSpeed = (TextSpeed)i);
        }

        static void Row(VisualElement container, string titleClass, string rowClass, string buttonClass, string selectedClass,
            string title, string[] choices, Func<int> current, Action<int> select)
        {
            var label = new Label(title);
            label.AddToClassList(titleClass);
            container.Add(label);

            var row = new VisualElement();
            row.AddToClassList(rowClass);
            container.Add(row);

            var buttons = new Button[choices.Length];
            void Refresh()
            {
                var selected = current();
                for (var i = 0; i < buttons.Length; i++)
                    buttons[i].EnableInClassList(selectedClass, i == selected);
            }

            for (var i = 0; i < choices.Length; i++)
            {
                var index = i;
                var button = new Button(() =>
                {
                    select(index);
                    Refresh();
                }) { text = choices[i] };
                button.AddToClassList(buttonClass);
                row.Add(button);
                buttons[i] = button;
            }
            Refresh();
        }
    }
}
