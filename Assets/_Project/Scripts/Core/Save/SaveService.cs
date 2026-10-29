using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace CronicasDeMexico.Core.Save
{
    /// <summary>
    /// Guarda y carga la partida en JSON (GDD §9: no PlayerPrefs). Escribe primero un temporal y conserva la versión
    /// anterior como copia de respaldo; si la principal no se puede leer, carga la copia. Los participantes
    /// (StoryState, QuestService...) se restauran en el orden en que se registraron.
    /// </summary>
    public sealed class SaveService
    {
        public const int CurrentVersion = 1;
        const string FileName = "partida.json";

        readonly List<ISaveParticipant> participants = new();

        public SaveService(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("Falta la carpeta de guardado.", nameof(folder));
            MainPath = Path.Combine(folder, FileName);
        }

        public string MainPath { get; }
        public string BackupPath => MainPath + ".bak";
        string TempPath => MainPath + ".tmp";

        public bool HasSave => File.Exists(MainPath) || File.Exists(BackupPath);

        public void Register(ISaveParticipant participant)
        {
            if (participant != null && !participants.Contains(participant))
                participants.Add(participant);
        }

        public void Unregister(ISaveParticipant participant) => participants.Remove(participant);

        /// <summary>Guarda el estado de los participantes. Devuelve false si no se pudo escribir.</summary>
        public bool Save()
        {
            var data = new SaveData
            {
                version = CurrentVersion,
                savedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            };
            foreach (var participant in participants)
                participant.Capture(data);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MainPath)!);
                File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));

                // Sin File.Replace (no es fiable en todas las plataformas): si se corta entre estos pasos, queda
                // la copia de respaldo con la partida anterior y Load la usa.
                if (File.Exists(MainPath))
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                File.Move(TempPath, MainPath);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogError($"[Guardado] No se pudo guardar en {MainPath}: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga la partida y restaura a los participantes. Devuelve false, sin tocar nada, si no hay partida o
        /// ni la principal ni la copia se pueden leer.
        /// </summary>
        public bool Load()
        {
            var data = Read(MainPath) ?? Read(BackupPath);
            if (data == null)
                return false;

            foreach (var participant in participants)
                participant.Restore(data);
            return true;
        }

        public void Delete()
        {
            foreach (var path in new[] { MainPath, BackupPath, TempPath })
                if (File.Exists(path))
                    File.Delete(path);
        }

        static SaveData Read(string path)
        {
            if (!File.Exists(path))
                return null;

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogWarning($"[Guardado] No se pudo leer {path}: {exception.Message}");
                return null;
            }

            if (data == null || data.version <= 0)
            {
                Debug.LogWarning($"[Guardado] {path} no es una partida válida.");
                return null;
            }
            if (data.version > CurrentVersion)
            {
                Debug.LogWarning($"[Guardado] {path} es de una versión más nueva del juego ({data.version}); no se carga.");
                return null;
            }

            return Migrate(data);
        }

        // Punto único para convertir partidas de versiones anteriores. Por ahora solo existe la versión 1.
        static SaveData Migrate(SaveData data)
        {
            data.flags ??= new List<string>();
            data.quests ??= new List<QuestRecord>();
            data.version = CurrentVersion;
            return data;
        }
    }
}
