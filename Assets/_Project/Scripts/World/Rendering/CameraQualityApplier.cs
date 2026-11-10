using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CronicasDeMexico.World.Rendering
{
    /// <summary>
    /// Apaga el post-procesado de la cámara en los niveles de calidad bajos y lo reactiva al subir de nivel.
    /// En Bajo el URP Asset no usa HDR: el bloom casi no se nota, pero su coste en la GPU se seguiría pagando.
    /// Escucha a QualitySettings y no a QualityService porque, al pulsar Play desde una escena de juego,
    /// la escena Boot (que registra el servicio) se carga después.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraQualityApplier : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("Nivel de calidad mínimo con post-procesado (0 = Bajo, 1 = Medio...).")]
        int postProcessingMinLevel = 1;

        UniversalAdditionalCameraData cameraData;

        void Awake() => cameraData = GetComponent<Camera>().GetUniversalAdditionalCameraData();

        void OnEnable()
        {
            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;
            Apply(QualitySettings.GetQualityLevel());
        }

        void OnDisable() => QualitySettings.activeQualityLevelChanged -= OnQualityLevelChanged;

        void OnQualityLevelChanged(int previousLevel, int currentLevel) => Apply(currentLevel);

        void Apply(int level) => cameraData.renderPostProcessing = level >= postProcessingMinLevel;
    }
}
