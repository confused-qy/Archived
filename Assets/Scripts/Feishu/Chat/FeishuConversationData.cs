using System;

namespace EmployeeHandbook.Feishu
{
    [Serializable]
    public class FeishuConversationCollection
    {
        public FeishuConversationData[] conversations;
    }

    [Serializable]
    public class FeishuConversationData
    {
        public string conversationId;
        public int unlockDay = 1;
        public string contactName;
        public string firstNodeId = "start";
        public FeishuChatNode[] nodes;
    }

    [Serializable]
    public class FeishuChatNode
    {
        public string id;
        public string speaker = "other";
        public string text;
        public int bubbleSize = 3;
        public float delay = 1f;
        public string next;
        public int unlockClueId;
        public FeishuChoiceData[] choices;

        public bool IsChoice
        {
            get { return string.Equals(speaker, "choice", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsPlayer
        {
            get { return string.Equals(speaker, "player", StringComparison.OrdinalIgnoreCase); }
        }
    }

    [Serializable]
    public class FeishuChoiceData
    {
        public string text;
        public int bubbleSize = 3;
        public string next;
        public int unlockClueId;
    }
}
