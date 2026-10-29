using System;
using UnityEngine;

namespace CronicasDeMexico.Data.Narrative
{
    /// <summary>
    /// Lo que dice un personaje según el estado de la historia. Las ramas se prueban en orden y se usa la primera
    /// cuyas condiciones se cumplen, así que las más específicas van arriba y la de por defecto al final.
    /// </summary>
    [CreateAssetMenu(menuName = "Crónicas/Narrativa/Diálogo", fileName = "DLG_")]
    public sealed class DialogueData : ScriptableObject
    {
        [SerializeField] DialogueBranch[] branches = Array.Empty<DialogueBranch>();

        public DialogueBranch[] Branches => branches;

        /// <summary>Primera rama disponible, o null si ninguna lo está.</summary>
        public DialogueBranch Select(Func<string, bool> hasFlag)
        {
            foreach (var branch in branches)
                if (branch != null && branch.IsAvailable(hasFlag))
                    return branch;
            return null;
        }

        public static DialogueData Create(string name, params DialogueBranch[] branches)
        {
            var dialogue = CreateInstance<DialogueData>();
            dialogue.name = name;
            dialogue.branches = branches;
            return dialogue;
        }
    }

    [Serializable]
    public sealed class DialogueBranch
    {
        [SerializeField, Tooltip("Banderas que deben estar activas.")]
        string[] requiredFlags = Array.Empty<string>();

        [SerializeField, Tooltip("Banderas que no deben estar activas.")]
        string[] forbiddenFlags = Array.Empty<string>();

        [SerializeField] DialogueLine[] lines = Array.Empty<DialogueLine>();

        [SerializeField, Tooltip("Banderas que se activan al terminar la rama (no si el jugador se aleja antes).")]
        string[] flagsOnFinish = Array.Empty<string>();

        [SerializeField, Tooltip("Opcional: misión que empieza al terminar la rama.")]
        QuestData questToStart;

        public DialogueBranch(DialogueLine[] lines, string[] requiredFlags = null, string[] forbiddenFlags = null,
            string[] flagsOnFinish = null, QuestData questToStart = null)
        {
            this.lines = lines ?? Array.Empty<DialogueLine>();
            this.requiredFlags = requiredFlags ?? Array.Empty<string>();
            this.forbiddenFlags = forbiddenFlags ?? Array.Empty<string>();
            this.flagsOnFinish = flagsOnFinish ?? Array.Empty<string>();
            this.questToStart = questToStart;
        }

        public DialogueLine[] Lines => lines;
        public string[] FlagsOnFinish => flagsOnFinish;
        public QuestData QuestToStart => questToStart;

        public bool IsAvailable(Func<string, bool> hasFlag)
        {
            if (lines == null || lines.Length == 0)
                return false;
            foreach (var flag in requiredFlags)
                if (!hasFlag(flag))
                    return false;
            foreach (var flag in forbiddenFlags)
                if (hasFlag(flag))
                    return false;
            return true;
        }
    }

    [Serializable]
    public struct DialogueLine
    {
        [SerializeField, Tooltip("Vacío = el personaje del diálogo.")]
        NpcData speaker;

        [SerializeField, TextArea(2, 5)] string text;

        public DialogueLine(string text, NpcData speaker = null)
        {
            this.text = text;
            this.speaker = speaker;
        }

        public NpcData Speaker => speaker;
        public string Text => text;
    }
}
