using CronicasDeMexico.Core.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Panel de opciones con los niveles de calidad (Bajo / Medio / Alto). Se abre y cierra con la acción
    /// Opciones y pausa el juego mientras está abierto.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class QualityOptionsPanel : MonoBehaviour
    {
        const string HiddenClass = "hidden";
        const string ButtonClass = "hud-options__button";
        const string SelectedButtonClass = "hud-options__button--selected";

        [SerializeField, Tooltip("Acción de botón que abre y cierra el panel (Player/Options).")]
        InputActionReference optionsAction;

        VisualElement panel;
        Button[] levelButtons;

        public bool IsOpen { get; private set; }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q("options");

            var container = root.Q("quality-buttons");
            container.Clear();
            var names = QualitySettings.names;
            levelButtons = new Button[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var level = i;
                var button = new Button(() => SelectLevel(level)) { text = names[i] };
                button.AddToClassList(ButtonClass);
                container.Add(button);
                levelButtons[i] = button;
            }

            // Cerrado al empezar, sin tocar Time.timeScale.
            IsOpen = false;
            panel.AddToClassList(HiddenClass);
            RefreshSelection();
            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;

            if (optionsAction != null)
                optionsAction.action.Enable();
        }

        void OnDisable()
        {
            QualitySettings.activeQualityLevelChanged -= OnQualityLevelChanged;
            if (optionsAction != null)
                optionsAction.action.Disable();

            if (IsOpen)
                SetOpen(false);
        }

        void Update()
        {
            if (optionsAction != null && optionsAction.action.WasPressedThisFrame())
                SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            panel.EnableInClassList(HiddenClass, !open);
            Time.timeScale = open ? 0f : 1f;

            if (open)
                levelButtons[QualitySettings.GetQualityLevel()].Focus();
        }

        static void SelectLevel(int level)
        {
            // Sin la escena Boot (p. ej. en pruebas) no hay servicio: se aplica sin guardar la preferencia.
            if (ServiceLocator.TryGet<QualityService>(out var quality))
                quality.SetLevel(level);
            else
                QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
        }

        void OnQualityLevelChanged(int previousLevel, int currentLevel) => RefreshSelection();

        void RefreshSelection()
        {
            var current = QualitySettings.GetQualityLevel();
            for (var i = 0; i < levelButtons.Length; i++)
                levelButtons[i].EnableInClassList(SelectedButtonClass, i == current);
        }
    }
}
