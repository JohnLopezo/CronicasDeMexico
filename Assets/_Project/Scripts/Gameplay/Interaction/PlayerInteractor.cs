using CronicasDeMexico.Core.Feedback;
using CronicasDeMexico.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.Gameplay.Interaction
{
    /// <summary>
    /// Busca el objeto interactuable más cercano, publica su aviso ("[E] Examinar") y lo activa al pulsar
    /// Interactuar. Con un mensaje abierto, Interactuar pasa a la siguiente línea de una conversación o lo cierra;
    /// alejarse también lo cierra.
    /// La búsqueda corre unas diez veces por segundo con un búfer fijo: no genera basura ni cuesta física cada frame.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        const float ScanInterval = 0.1f;
        const string KeyboardBindingGroup = "Keyboard&Mouse";

        [SerializeField, Tooltip("Acción de botón Interactuar (Player/Interact).")]
        InputActionReference interactAction;

        [SerializeField, Tooltip("Layers en los que se buscan objetos interactuables.")]
        LayerMask interactableLayers;

        [SerializeField, Min(0.1f), Tooltip("Radio de búsqueda en metros.")]
        float detectionRadius = 1f;

        [SerializeField, Tooltip("Altura del centro de búsqueda sobre los pies.")]
        float detectionHeight = 0.7f;

        [SerializeField, Tooltip("Canal donde se publica el aviso (vacío = ocultarlo).")]
        StringEventChannel promptChannel;

        [SerializeField, Tooltip("Canal de mensajes: se escucha para saber si hay uno abierto y se usa para cerrarlo.")]
        StringEventChannel messageChannel;

        readonly Collider[] hits = new Collider[8];
        IInteractable current;
        IInteractable active; // el que abrió el mensaje actual
        string keyLabel;
        string shownPrompt;
        bool messageOpen;
        float scanTimer = ScanInterval;
        bool holding;
        IPathActionTarget holdTarget;
        float holdStart;

        /// <summary>Segundos que hay que mantener Interactuar para la acción de senda.</summary>
        public const float HoldSeconds = 0.5f;

        const float HoldRingDelay = 0.1f;

        void StopHolding()
        {
            holding = false;
            HoldProgress.Value = -1f;
        }

        /// <summary>Objeto interactuable más cercano en la última búsqueda, o null.</summary>
        public IInteractable Current => current;

        void OnEnable()
        {
            if (interactAction != null)
            {
                var action = interactAction.action;
                action.Enable();
                keyLabel = action.GetBindingDisplayString(InputBinding.MaskByGroup(KeyboardBindingGroup));
                if (string.IsNullOrEmpty(keyLabel))
                    keyLabel = action.GetBindingDisplayString();
            }

            if (messageChannel != null)
                messageChannel.Subscribe(OnMessage);
        }

        void OnDisable()
        {
            if (interactAction != null)
                interactAction.action.Disable();
            if (messageChannel != null)
                messageChannel.Unsubscribe(OnMessage);

            current = null;
            StopHolding();
            PublishPrompt(null);
        }

        void Update()
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= ScanInterval)
            {
                scanTimer = 0f;
                Scan();
            }

            if (interactAction == null)
                return;
            var action = interactAction.action;

            // Pulsación larga (acción de senda): solo se espera a soltar si el habitante tiene una disponible, así el
            // resto de interacciones siguen respondiendo al pulsar.
            if (holding)
            {
                var held = Time.unscaledTime - holdStart;
                if (holdTarget != current || messageOpen)
                {
                    StopHolding();
                }
                else if (held >= HoldSeconds)
                {
                    StopHolding();
                    active = holdTarget;
                    holdTarget.PathInteract(transform);
                }
                else if (!action.IsPressed())
                {
                    StopHolding();
                    active = holdTarget;
                    holdTarget.Interact(transform);
                }
                else
                {
                    // El anillo empieza a verse pasado un instante: un toque corto no lo enseña.
                    HoldProgress.Value = held < HoldRingDelay ? -1f : (held - HoldRingDelay) / (HoldSeconds - HoldRingDelay);
                }
                return;
            }

            if (!action.WasPressedThisFrame())
                return;

            if (!messageOpen && current is IPathActionTarget target && target.PathVerb != null)
            {
                holding = true;
                holdTarget = target;
                holdStart = Time.unscaledTime;
                return;
            }

            if (messageOpen)
            {
                // Primera pulsación: termina de escribir el texto; la siguiente avanza.
                if (MessageReveal.TryComplete())
                    return;
                if (active is IConversation conversation && conversation.Advance())
                    return;
                CloseMessage();
            }
            else if (current != null)
            {
                active = current;
                current.Interact(transform);
            }
        }

        void Scan()
        {
            var center = transform.position + Vector3.up * detectionHeight;
            var count = Physics.OverlapSphereNonAlloc(center, detectionRadius, hits, interactableLayers, QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var interactable = hits[i].GetComponentInParent<IInteractable>();
                if (interactable == null)
                    continue;

                var distance = (hits[i].bounds.ClosestPoint(center) - center).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            if (nearest == current)
                return;

            current = nearest;
            if (messageOpen)
                CloseMessage();
            RefreshPrompt();
        }

        void CloseMessage()
        {
            if (messageChannel != null)
                messageChannel.Raise(null);
            else
                OnMessage(null);
        }

        void OnMessage(string message)
        {
            messageOpen = !string.IsNullOrEmpty(message);
            if (!messageOpen && active != null)
            {
                var ended = active;
                active = null;
                (ended as IConversation)?.End();
            }

            RefreshPrompt();
        }

        // Con pantalla táctil no hay tecla que mostrar: el botón en pantalla ya dice qué hace. Con acción de senda se
        // añade "· mantén: Indagar".
        void RefreshPrompt()
        {
            if (current == null || messageOpen)
            {
                PublishPrompt(null);
                return;
            }
            var touch = Touchscreen.current != null;
            var prompt = touch ? current.Prompt : $"[{keyLabel}] {current.Prompt}";
            if (current is IPathActionTarget target && target.PathVerb is { } verb)
                prompt += touch ? $"  ·  mantén: {verb}" : $"  ·  mantén [{keyLabel}]: {verb}";
            PublishPrompt(prompt);
        }

        void PublishPrompt(string prompt)
        {
            if (prompt == shownPrompt)
                return;

            shownPrompt = prompt;
            if (promptChannel != null)
                promptChannel.Raise(prompt);
        }
    }
}
