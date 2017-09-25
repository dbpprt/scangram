using System.Collections.Generic;

namespace Scangram.Services.DataTransfer
{
    public class ChatState
    {
        public int MessageId { get; set; }

        public List<string> Files { get; set; }
    }
}
