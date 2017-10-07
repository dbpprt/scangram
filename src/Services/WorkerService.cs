using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var files = new List<Stream>();

            try
            {
                foreach (var fileInfo in workItem.Files)
                {
                    var stream = new MemoryStream();

                    try
                    {
                        var file = await _telegramBotClient.GetFileAsync(fileInfo, stream);
                        _logger.LogInformation("Downloaded file with name {0} and id {1} - Size: {2} KB", file.FilePath,
                            file.FileId, file.FileSize / 1024);

                        if (workItem.DocumentAction == DocumentAction.DocumentScan)
                        {
                            var result = _imageDocumentDetectionService.ProcessStream(stream);
                            stream.Dispose();

                            if (result == null)
                            {
                                await _telegramBotClient.SendTextMessageAsync(workItem.ChatId,
                                    "Oh snap, I'm not able to handle this picture :(");
                                return;
                            }

                            files.Add(result);
                        }
                        else
                        {
                            files.Add(stream);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Unable to process a file :(");
                        stream.Dispose();

                        await _telegramBotClient.SendTextMessageAsync(workItem.ChatId,
                            "Oh snap, I'm not able to complete your action :(");
                        return;
                    }
                }

                switch (workItem.ConversationType)
                {
                    case ConversationType.Image:
                        foreach (var processedFile in files)
                        {
                            await _telegramBotClient.SendPhotoAsync(workItem.ChatId,
                                new FileToSend(Guid.NewGuid() + ".jpg", processedFile));
                        }
                        break;

                    case ConversationType.Pdf:
                        using (var collection = new MagickImageCollection())
                        {
                            foreach (var processedFile in files)
                            {
                                collection.Add(new MagickImage(processedFile));
                            }

                            using (var pdfStream = new MemoryStream())
                            {
                                collection.Write(pdfStream, MagickFormat.Pdf);
                                pdfStream.Position = 0;

                                await _telegramBotClient.SendDocumentAsync(workItem.ChatId, new FileToSend(Guid.NewGuid() + ".pdf", pdfStream));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unable to process a file :(");

                await _telegramBotClient.SendTextMessageAsync(workItem.ChatId,
                    "Oh snap, I'm not able to complete your action :(");
                return;
            }
            finally
            {

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
