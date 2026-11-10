using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CronicasDeMexico.UI.Touch
{
    /// <summary>
    /// Controles táctiles para Android en horizontal (GDD §14): un joystick y botones en pantalla (OnScreenStick y
    /// OnScreenButton del Input System) que hacen de mando virtual, así que el juego los entiende con los bindings de
    /// mando que ya tiene, sin cambiar nada. Se muestran solo si hay pantalla táctil (o con forceVisible, para probarlos
    /// en el PC) y se ocultan con el juego en pausa (menú abierto: Time.timeScale = 0), cuyos menús de UI Toolkit ya
    /// responden al toque. Viven en Boot, que dura toda la partida. Los botones de uGUI necesitan un EventSystem activo:
    /// usa el suyo solo si no hay otro (el menú de inicio trae el suyo).
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        [SerializeField, Tooltip("Lo que se muestra y se oculta (joystick y botones).")] GameObject controls;
        [SerializeField] EventSystem ownEventSystem;
        [SerializeField, Tooltip("Mostrarlos aunque no haya pantalla táctil (para probar en el PC).")] bool forceVisible;

        public bool ForceVisible
        {
            get => forceVisible;
            set => forceVisible = value;
        }

        public bool IsVisible => controls != null && controls.activeSelf;

        static bool HasTouchscreen => Touchscreen.current != null;

        void Update()
        {
            var visible = (forceVisible || HasTouchscreen) && Time.timeScale > 0f;
            if (controls != null && controls.activeSelf != visible)
                controls.SetActive(visible);

            if (ownEventSystem == null)
                return;
            // Solo el nuestro, y solo cuando hace falta y no hay otro (dos EventSystem activos dan aviso y se pisan).
            var other = EventSystem.current != null && EventSystem.current != ownEventSystem && EventSystem.current.isActiveAndEnabled;
            var needed = visible && !other;
            if (ownEventSystem.gameObject.activeSelf != needed)
                ownEventSystem.gameObject.SetActive(needed);
        }
    }
}
