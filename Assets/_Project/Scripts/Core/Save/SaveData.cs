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

        // Recompensas y progresión (versión 2, M5).
        public List<Progress.ItemStack> items = new();
        public List<string> equippedRelics = new();
        public int level;
        public int experience;
        /// <summary>Energía actual; -1 = llena (partidas anteriores a la versión 2).</summary>
        public int energy = -1;
        public List<string> blessings = new();
        public string primaryBlessing;
        public string secondaryBlessing;
        /// <summary>Veces que se venció a cada enemigo concreto (protección contra el farmeo).</summary>
        public List<KeyCount> defeats = new();
        /// <summary>Recompensas únicas ya entregadas (cofres, jefes, misiones): nunca se entregan dos veces.</summary>
        public List<string> grantedRewards = new();
    }

    [Serializable]
    public sealed class KeyCount
    {
        public string key;
        public int count;
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
