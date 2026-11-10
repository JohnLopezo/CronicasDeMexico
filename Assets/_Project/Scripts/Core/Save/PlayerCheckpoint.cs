namespace CronicasDeMexico.Core.Save
{
    /// <summary>
    /// Último santuario de Memoria donde descansó el jugador: la escena, el punto de aparición y la vida que tenía. Lo
    /// fija el santuario (Gameplay) antes de guardar; con él se vuelve tras una derrota en la mazmorra y se continúa la
    /// partida al arrancar desde Boot. Lo registra GameBootstrapper.
    /// </summary>
    public sealed class PlayerCheckpoint : ISaveParticipant
    {
        public string Scene { get; private set; }
        public string SpawnId { get; private set; }
        public int Health { get; private set; }

        public bool HasCheckpoint => !string.IsNullOrEmpty(Scene) && !string.IsNullOrEmpty(SpawnId);

        public void Set(string scene, string spawnId, int health)
        {
            Scene = scene;
            SpawnId = spawnId;
            Health = health;
        }

        public void Clear() => Set(null, null, 0);

        public void Capture(SaveData data)
        {
            data.scene = Scene;
            data.checkpointId = SpawnId;
            data.health = Health;
        }

        public void Restore(SaveData data) => Set(data.scene, data.checkpointId, data.health);
    }
}
