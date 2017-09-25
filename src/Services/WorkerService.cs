using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Scangram.Common;
using Scangram.Common.DocumentDetection;
using Scangram.Services.Contracts;
using Scangram.Services.DataTransfer;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scangram.Services
{
    public class WorkerService : HostedService, IWorkerService
    {
        private readonly ILogger<WorkerService> _logger;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly ImageDocumentDetectionService _imageDocumentDetectionService;
        private readonly ConcurrentQueue<WorkItem> _queue;

        public WorkerService(IServiceProvider serviceProvider, ILogger<WorkerService> logger, ITelegramBotClient telegramBotClient, ImageDocumentDetectionService imageDocumentDetectionService) : base(serviceProvider)
        {
            _logger = logger;
            _telegramBotClient = telegramBotClient;
            _imageDocumentDetectionService = imageDocumentDetectionService;
            _queue = new ConcurrentQueue<WorkItem>();
        }

        protected override async Task ExecuteAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out WorkItem queueItem))
                {
                    _logger.LogInformation("Processing work item with id {0}", queueItem.Id);

                    try
                    {
                        await ProcessItem(queueItem);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "Critical error while processing work item {0}", queueItem.Id);
                    }

                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        private async Task ProcessItem(WorkItem workItem)
        {
            using (var memoryStream = new MemoryStream())
            {
                var file = await _telegramBotClient.GetFileAsync(workItem.Files.First(), memoryStream);
                _logger.LogInformation("Downloaded file with name {0} and id {1} - Size: {2} KB", file.FilePath, file.FileId, file.FileSize / 1024);

                var result = _imageDocumentDetectionService.ProcessStream(memoryStream);

                if (result == null)
                {
                    await _telegramBotClient.SendTextMessageAsync(workItem.ChatId, "Oh snap, I'm not able to handle this picture :(");
                    return;
                }

                using (result)
                {
                    // TODO: Reply to initial message!
                    // TODO: File name

                    switch (workItem.ConversationType)
                    {
                        case ConversationType.Image:
                            await _telegramBotClient.SendPhotoAsync(workItem.ChatId, new FileToSend(Guid.NewGuid() + ".jpg", result));
                            break;

                        case ConversationType.Pdf:
                            using (var image = new MagickImage(result, new MagickReadSettings(new JpegReadDefines())))
                            {
                                using (var pdfStream = new MemoryStream())
                                {
                                    image.Write(pdfStream, MagickFormat.Pdf);
                                    pdfStream.Position = 0;

                                    await _telegramBotClient.SendDocumentAsync(workItem.ChatId, new FileToSend(Guid.NewGuid() + ".pdf", pdfStream));
                                }
                            }
                            break;
                    }
                }
            }
        }

        public int GetQueueLength()
        {
            return _queue.Count;
        }

        public Guid QueueItem(WorkItem workItem)
        {
            workItem.Id = Guid.NewGuid();
            _queue.Enqueue(workItem);
            return workItem.Id;
        }
    }
}
