using System;
using Scangram.Services.DataTransfer;

namespace Scangram.Services.Contracts
{
    public interface IWorkerService
    {
        Guid QueueItem(WorkItem workItem);
        int GetQueueLength();
    }
}