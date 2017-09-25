using System;
using System.Collections.Generic;

namespace Scangram.Services.DataTransfer
{
    public class WorkItem
    {
        public Guid Id { get; set; }

        public List<string> Files { get; set; }

        public long ChatId { get; set; }

        public ConversationType ConversationType { get; set; }
    }
}
