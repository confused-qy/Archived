using System.Collections.Generic;

namespace EmployeeHandbook.Feishu
{
    public class FeishuConversationRuntimeState
    {
        public readonly List<FeishuTranscriptEntry> Transcript = new List<FeishuTranscriptEntry>();
        public string PendingNodeId;
        public bool WaitingForChoice;
        public bool Completed;
        public int LastReadDay;
        public bool HasBeenOpened;
    }

    public class FeishuTranscriptEntry
    {
        public string Speaker;
        public string Text;
        public int BubbleSize;

        public bool IsPlayer
        {
            get { return string.Equals(Speaker, "player", System.StringComparison.OrdinalIgnoreCase); }
        }
    }
}
