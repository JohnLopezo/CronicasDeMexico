using CronicasDeMexico.Core.Boot;
using CronicasDeMexico.Core.Progress;
using CronicasDeMexico.Core.Save;
using CronicasDeMexico.Core.Services;
using CronicasDeMexico.Core.Travel;
using CronicasDeMexico.Data.Characters;
using UnityEngine;
using UnityEngine.UIElements;

namespace CronicasDeMexico.UI.Hud
{
    /// <summary>
    /// Menú de inicio, en la escena Boot: Continuar, Nueva partida (pide confirmar si hay partida guardada y luego elegir
    /// Guardián), Opciones (calidad) y Salir. Solo se muestra cuando el juego arranca desde Boot (GameBootstrapper.StartMenuRequested); al
    /// pulsar Play desde una escena de juego no aparece. La interfaz se construye en código sobre un fondo liso.
    /// La cámara de fondo y el EventSystem (navegación con teclado o mando) son hijos inactivos que solo se activan
    /// mientras el menú está a la vista, para no duplicar los de las escenas de juego.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StartMenuController : MonoBehaviour
    {
        const string Hidden = "hidden";
        const string SelectedClass = "start-menu__button--selected";

        [SerializeField, Tooltip("Estilos del menú (StartMenu.uss).")] StyleSheet styleSheet;
        [SerializeField, Tooltip("Cámara que limpia la pantalla mientras no hay escena de juego.")] Camera backgroundCamera;
        [SerializeField, Tooltip("EventSystem propio del menú (navegación con teclado o mando).")] GameObject eventSystem;

        VisualElement root, main, confirm, options, qualityButtons, guardians, guardianCards;
        Button continueButton, newGameButton, confirmYes, guardianStart, guardianBack;
        Label message, guardianTitle, guardianDescription;
        GuardianData highlighted;
        bool pickOnly;
        SaveService save;
        PlayerCheckpoint checkpoint;
        SceneTravel travel;
        string initialScene;

        public bool IsVisible { get; private set; }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            Build();
            SetVisible(false);
            QualitySettings.activeQualityLevelChanged += OnQualityChanged;
        }

        void OnDisable() => QualitySettings.activeQualityLevelChanged -= OnQualityChanged;

        void Start()
        {
            if (!GameBootstrapper.StartMenuRequested)
            {
                // Play desde una escena de juego: no hay menú, pero sí se elige con quién jugar. En las pruebas, Boot arranca sin jugador.
                if (GameObject.FindWithTag("Player") != null)
                    ShowGuardianPicker();
                return;
            }
            var bootstrapper = FindAnyObjectByType<GameBootstrapper>();
            Show(ServiceLocator.Get<SaveService>(), ServiceLocator.Get<PlayerCheckpoint>(), ServiceLocator.Get<SceneTravel>(),
                bootstrapper != null ? bootstrapper.InitialScene : null);
        }

        /// <summary>Muestra el menú con los servicios dados (las pruebas pasan un guardado en una carpeta propia).</summary>
        public void Show(SaveService saveService, PlayerCheckpoint savedCheckpoint, SceneTravel sceneTravel, string scene)
        {
            save = saveService;
            checkpoint = savedCheckpoint;
            travel = sceneTravel;
            initialScene = scene;
            message.text = "";
            continueButton.SetEnabled(save != null && save.HasSave);
            SetVisible(true);
            // Después: el foco no entra en elementos ocultos. Sin partida que continuar, se empieza eligiendo Guardián.
            ShowPanel(save != null && !save.HasSave && guardianCards.childCount > 0 ? guardians : main);
        }

        /// <summary>
        /// Elegir Guardián sobre una escena ya cargada (Play en el Editor desde una escena de juego), con el juego en pausa.
        /// shortcut: abrir el menú de juego (I) encima quita la pausa al cerrarlo; solo pasa en el Editor, sin arreglo por ahora.
        /// </summary>
        public void ShowGuardianPicker()
        {
            if (guardianCards.childCount == 0)
                return;
            pickOnly = true;
            Time.timeScale = 0f;
            IsVisible = true;
            root.Q("start-menu").RemoveFromClassList(Hidden);
            // La cámara de fondo no hace falta; el EventSystem, solo si la escena no trae el suyo.
            if (eventSystem != null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                eventSystem.SetActive(true);
            guardianBack.AddToClassList(Hidden);
            ShowPanel(guardians);
        }

        public void Hide()
        {
            if (pickOnly)
            {
                pickOnly = false;
                Time.timeScale = 1f;
                guardianBack.RemoveFromClassList(Hidden);
            }
            SetVisible(false);
        }

        /// <summary>Continúa la partida en su santuario de Memoria. Si no se puede leer, lo avisa y no viaja.</summary>
        public void Continue()
        {
            if (GameBootstrapper.ContinueSavedGame(save, checkpoint, travel))
            {
                SetVisible(false);
                return;
            }
            message.text = "No se pudo cargar la partida guardada.";
            continueButton.SetEnabled(false);
            newGameButton.Focus();
        }

        /// <summary>Nueva partida: si hay una guardada, primero pide confirmar que se borrará.</summary>
        public void NewGame()
        {
            if (save != null && save.HasSave)
                ShowPanel(confirm);
            else
                ConfirmNewGame();
        }

        /// <summary>Pasa a la elección de Guardián. La partida guardada no se borra hasta empezar con uno.</summary>
        public void ConfirmNewGame()
        {
            if (guardianCards.childCount > 0)
                ShowPanel(guardians);
            else
                StartWithGuardian(null); // sin catálogo: el Guardián de la configuración
        }

        /// <summary>Marca un Guardián en la pantalla de elección y muestra su descripción.</summary>
        public void HighlightGuardian(GuardianData guardian)
        {
            highlighted = guardian;
            foreach (var card in guardianCards.Children())
                card.EnableInClassList(SelectedClass, card.name == CardName(guardian));
            guardianTitle.text = $"{guardian.DisplayName}, {guardian.Title}";
            var stats = guardian.At(1);
            guardianDescription.text = $"{guardian.Description}\nVida {stats.maxHealth} · Ataque {stats.attack} · Defensa {stats.defense} · Energía {stats.maxEnergy}";
            guardianStart.text = $"Empezar con {guardian.DisplayName}";
        }

        public void StartWithGuardian(string guardianId)
        {
            if (ServiceLocator.TryGet(out Progression progression))
                progression.SelectGuardian(guardianId);
            if (pickOnly)
            {
                Hide(); // la escena ya está cargada
                return;
            }
            if (GameBootstrapper.StartNewGame(save, initialScene))
            {
                SetVisible(false);
                return;
            }
            message.text = "No se pudo empezar la partida.";
            ShowPanel(main);
        }

        static string CardName(GuardianData guardian) => $"start-guardian-{guardian.Id}";

        void BuildGuardians(VisualElement screen)
        {
            guardians = Panel(screen, "start-guardians");
            var heading = new Label("Elige tu Guardián");
            heading.AddToClassList("start-menu__text");
            guardians.Add(heading);
            guardianCards = new VisualElement();
            guardianCards.AddToClassList("start-menu__row");
            guardians.Add(guardianCards);

            guardianTitle = new Label { name = "start-guardian-title" };
            guardianTitle.AddToClassList("start-menu__guardian-title");
            guardians.Add(guardianTitle);
            guardianDescription = new Label { name = "start-guardian-description" };
            guardianDescription.AddToClassList("start-menu__text");
            guardians.Add(guardianDescription);
            guardianStart = AddButton(guardians, "start-guardian-start", "Empezar", () => StartWithGuardian(highlighted.Id));
            guardianBack = AddButton(guardians, "start-guardian-back", "Volver", () => ShowPanel(main));

            var catalog = Resources.Load<GuardianCatalog>(GuardianCatalog.ResourcePath);
            if (catalog == null)
                return;
            foreach (var guardian in catalog.Guardians)
            {
                var card = new Button(() => HighlightGuardian(guardian)) { name = CardName(guardian) };
                card.AddToClassList("start-menu__button");
                card.AddToClassList("start-menu__guardian");
                // Con teclado o mando basta moverse por las tarjetas para leerlas.
                card.RegisterCallback<FocusInEvent>(_ => HighlightGuardian(guardian));
                var portrait = new VisualElement();
                portrait.AddToClassList("start-menu__guardian-portrait");
                portrait.style.backgroundImage = new StyleBackground(guardian.SpriteSet.GetIdle(DirectionalSpriteSet.DirectionCount - 1));
                card.Add(portrait);
                var label = new Label(guardian.DisplayName);
                label.AddToClassList("start-menu__guardian-name");
                card.Add(label);
                guardianCards.Add(card);
            }
        }

        void Build()
        {
            var screen = new VisualElement { name = "start-menu" };
            screen.AddToClassList("start-menu");
            root.Add(screen);

            var title = new Label("Crónicas de México");
            title.AddToClassList("start-menu__title");
            screen.Add(title);

            main = Panel(screen, "start-main");
            continueButton = AddButton(main, "start-continue", "Continuar", Continue);
            newGameButton = AddButton(main, "start-new", "Nueva partida", NewGame);
            AddButton(main, "start-options", "Opciones", () => ShowPanel(options));
#if !UNITY_EDITOR
            AddButton(main, "start-quit", "Salir", Application.Quit);
#endif
            message = new Label { name = "start-message" };
            message.AddToClassList("start-menu__message");
            screen.Add(message);

            confirm = Panel(screen, "start-confirm");
            var warning = new Label("Hay una partida guardada. Si empiezas una nueva, se borrará.");
            warning.AddToClassList("start-menu__text");
            confirm.Add(warning);
            confirmYes = AddButton(confirm, "start-confirm-yes", "Borrar y empezar", ConfirmNewGame);
            AddButton(confirm, "start-confirm-no", "Cancelar", () => ShowPanel(main));

            BuildGuardians(screen);

            options = Panel(screen, "start-options-panel");
            var quality = new Label("Calidad gráfica");
            quality.AddToClassList("start-menu__text");
            options.Add(quality);
            qualityButtons = new VisualElement();
            qualityButtons.AddToClassList("start-menu__row");
            options.Add(qualityButtons);
            var names = QualitySettings.names;
            for (var i = 0; i < names.Length; i++)
            {
                var level = i;
                AddButton(qualityButtons, $"start-quality-{i}", names[i], () => QualityOptionsPanel.SelectLevel(level));
            }
            AddButton(options, "start-options-back", "Volver", () => ShowPanel(main));
            OnQualityChanged(0, QualitySettings.GetQualityLevel());

            // Esc o el botón de atrás del mando: de un subpanel vuelve al principal.
            root.RegisterCallback<NavigationCancelEvent>(_ =>
            {
                if (IsVisible && !pickOnly && main.ClassListContains(Hidden))
                    ShowPanel(main);
            });
        }

        static VisualElement Panel(VisualElement parent, string name)
        {
            var panel = new VisualElement { name = name };
            panel.AddToClassList("start-menu__panel");
            parent.Add(panel);
            return panel;
        }

        static Button AddButton(VisualElement parent, string name, string text, System.Action onClick)
        {
            var button = new Button(onClick) { name = name, text = text };
            button.AddToClassList("start-menu__button");
            parent.Add(button);
            return button;
        }

        void ShowPanel(VisualElement panel)
        {
            foreach (var p in new[] { main, confirm, options, guardians })
                p.EnableInClassList(Hidden, p != panel);
            if (panel == main)
                (continueButton.enabledSelf ? continueButton : newGameButton).Focus();
            else if (panel == confirm)
                confirmYes.Focus();
            else if (panel == guardians)
                guardianCards[0].Focus();
            else
                qualityButtons[QualitySettings.GetQualityLevel()].Focus();
        }

        void SetVisible(bool visible)
        {
            IsVisible = visible;
            root.Q("start-menu").EnableInClassList(Hidden, !visible);
            if (backgroundCamera != null)
                backgroundCamera.gameObject.SetActive(visible);
            if (eventSystem != null)
                eventSystem.SetActive(visible);
        }

        void OnQualityChanged(int previous, int current)
        {
            for (var i = 0; i < qualityButtons.childCount; i++)
                qualityButtons[i].EnableInClassList(SelectedClass, i == current);
        }
    }
}
