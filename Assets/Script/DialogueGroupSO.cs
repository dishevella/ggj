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

    [Header("Speakers")]
    public List<Speaker> speakers = new List<Speaker>();


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

        [Header("Presentation")]
        public DialogueChannel channel = DialogueChannel.Narration;

        // 👉 指向 DialogueGroupSO.speakers 里的某一个（Inspector 下拉）
        public int speakerIndex = -1; // -1 = 旁白 / 无角色

        [Tooltip("可空：覆盖 speaker 的默认头像")]
        public Sprite portraitOverride;

        [Tooltip("可空：覆盖 speaker 的名字")]
        public string nameOverride;
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

        [Tooltip("Object/Clue gained")]
        public GameObject getObject;
        public string getClue;
    }

    [Serializable]
    public class PlayerSelection
    {
        public string id;
        public string content;
        public int targetDialogueIndex;
    }

    [Serializable]
    public class Speaker
    {
        public string id;                  // 内部引用用（可选）
        public string displayName;          // 显示名（可空 = 不显示）
        public Sprite defaultPortrait;      // 默认头像（可空）
    }

    public enum DialogueChannel
    {
        Narration,
        Left,
        Right
    }

}
