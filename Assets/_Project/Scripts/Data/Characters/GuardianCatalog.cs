using System;
using CronicasDeMexico.Core.Progress;
using UnityEngine;

namespace CronicasDeMexico.Data.Characters
{
    /// <summary>
    /// Guardianes que se pueden elegir, en el orden de la pantalla de selección. GameBootstrapper lo carga de Resources
    /// (<see cref="ResourcePath"/>): Core no puede referenciar a Data, así que lo trata como IGuardianCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Personajes/Catálogo de Guardianes", fileName = "CatalogoGuardianes")]
    public sealed class GuardianCatalog : ScriptableObject, IGuardianCatalog
    {
        public const string ResourcePath = "Personajes/CatalogoGuardianes";

        [SerializeField] GuardianData[] guardians = Array.Empty<GuardianData>();

        public GuardianData[] Guardians
        {
            get => guardians;
            set => guardians = value ?? Array.Empty<GuardianData>();
        }

        public IStatProfile Find(string guardianId)
        {
            foreach (var guardian in guardians)
                if (guardian != null && guardian.Id == guardianId)
                    return guardian;
            return null;
        }
    }
}
