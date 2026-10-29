using System;
using System.Collections.Generic;
using CronicasDeMexico.Core.Save;

namespace CronicasDeMexico.Core.Narrative
{
    /// <summary>
    /// Estado de la historia: banderas persistentes ("anciana.relato_recordado") y avisos de lo que ocurre en el
    /// mundo ("hablar:artesano", "examinar:altar_trono", "vencer:espiritu_jaguar"). Los diálogos leen las banderas
    /// y las misiones escuchan los avisos, sin conocerse entre sí. Lo registra GameBootstrapper.
    /// </summary>
    public sealed class StoryState : ISaveParticipant
    {
        readonly HashSet<string> flags = new();

        /// <summary>Se publica con la bandera que se activó o se quitó.</summary>
        public event Action<string> FlagChanged;

        /// <summary>Se publica con cada aviso de <see cref="Report"/>.</summary>
        public event Action<string> EventReported;

        public IReadOnlyCollection<string> Flags => flags;

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && flags.Add(flag))
                FlagChanged?.Invoke(flag);
        }

        public void ClearFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && flags.Remove(flag))
                FlagChanged?.Invoke(flag);
        }

        /// <summary>Avisa de algo que acaba de pasar. No se guarda: solo lo oyen quienes escuchan ahora.</summary>
        public void Report(string happening)
        {
            if (!string.IsNullOrEmpty(happening))
                EventReported?.Invoke(happening);
        }

        public void Capture(SaveData data)
        {
            data.flags.Clear();
            data.flags.AddRange(flags);
            data.flags.Sort(StringComparer.Ordinal); // archivo estable entre guardados
        }

        public void Restore(SaveData data)
        {
            flags.Clear();
            foreach (var flag in data.flags)
                if (!string.IsNullOrEmpty(flag))
                    flags.Add(flag);
        }
    }
}
