using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Group")]
public class DialogueGroupSO : ScriptableObject
{
    [Header("Group Info")]
    public string groupId = "Group_001";
    public int startIndex = 0;

    [Header("Nodes")]
    public List<DialogueNode> nodes = new List<DialogueNode>();

    // =========================
    // Data Types
    // =========================
    [Serializable]
    public class DialogueNode
    {
        [TextArea(2, 6)]
        public string content;

        [Header("Flow")]
        public bool isLocked = false;
        public int nextIndex = -1;

        [Header("Explore Tokens")]
        public List<SearchToken> tokens = new List<SearchToken>();

        [Header("Player Selections")]
        public List<PlayerSelection> selection = new List<PlayerSelection>();
    }

    [Serializable]
    public class SearchToken
    {
        [Tooltip("Must match <link=id>...</link>")]
        public string id;

        [Tooltip("If true, all required tokens must be clicked to unlock this node")]
        public bool required = true;

        [Tooltip("Runtime state")]
        public bool clicked = false;
    }

    [Serializable]
    public class PlayerSelection
    {
        public string id;
        public string content;
        public int targetDialogueIndex;
    }
}
