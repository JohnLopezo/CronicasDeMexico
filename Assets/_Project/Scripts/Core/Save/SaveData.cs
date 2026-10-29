using System;
using System.Collections.Generic;

namespace CronicasDeMexico.Core.Save
{
    /// <summary>
    /// Contenido de una partida (JsonUtility). Al cambiar su forma, subir <see cref="SaveService.CurrentVersion"/>
    /// y migrar las versiones anteriores en SaveService.Migrate.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version;
        /// <summary>Fecha UTC en ISO 8601.</summary>
        public string savedAt;

        // Jugador. Los rellenarán sus participantes cuando existan (Guardián, santuarios); por ahora quedan vacíos.
        public string guardianId;
        public string scene;
        public string checkpointId;
        public int health;

        // Historia.
        public List<string> flags = new();
        public List<QuestRecord> quests = new();
    }

    [Serializable]
    public sealed class QuestRecord
    {
        public string id;
        public bool completed;
        public int step;
        public int count;
    }

    /// <summary>Servicio o componente que escribe y lee su parte de la partida.</summary>
    public interface ISaveParticipant
    {
        void Capture(SaveData data);
        void Restore(SaveData data);
    }
}
