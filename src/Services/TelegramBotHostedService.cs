using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scangram.Common;
using Scangram.Services.Contracts;
using Scangram.Services.DataTransfer;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scangram.Services
{
    [UsedImplicitly]
    public class TelegramBotHostedService : HostedService
    {
        private readonly ILogger<TelegramBotHostedService> _logger;
        private readonly ILogger<ConversationStateMachine> _conversationLogger;
        private readonly IWorkerService _workerService;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly ConcurrentDictionary<Int64, (ReaderWriterLockSlim, ConversationStateMachine)> _conversations;

        public TelegramBotHostedService(IServiceProvider serviceProvider, ITelegramBotClient telegramBotClient, ILogger<TelegramBotHostedService> logger, ILogger<ConversationStateMachine> conversationLogger, IWorkerService workerService) : base(serviceProvider)
        {
            _conversations = new ConcurrentDictionary<long, (ReaderWriterLockSlim, ConversationStateMachine)>();

            _logger = logger;
            _conversationLogger = conversationLogger;
            _workerService = workerService;

            _telegramBotClient = telegramBotClient;
            _telegramBotClient.OnMessage += OnMessage;
            _telegramBotClient.OnCallbackQuery += OnCallbackQuery;
            _telegramBotClient.OnReceiveError += OnReceiveError;
        }

        private void OnReceiveError(object sender, ReceiveErrorEventArgs args)
        {
            _logger.LogCritical(args.ApiRequestException, "Critical error occurred!");
        }

        private async void OnCallbackQuery(object sender, CallbackQueryEventArgs args)
        {
            var chatId = args.CallbackQuery.Message.Chat.Id;
            
            if (_conversations.TryGetValue(chatId, out (ReaderWriterLockSlim readerWriterLock, ConversationStateMachine conversation) state))
            {
                await state.conversation.HandleCallbackQueryAsync(args.CallbackQuery);
            }
            else
            {
                await _telegramBotClient.AnswerCallbackQueryAsync(args.CallbackQuery.Id);
                await _telegramBotClient.SendTextMessageAsync(args.CallbackQuery.Message.Chat.Id, "Sorry, this session is expired :(");
            }

            //var cacheEntry = _cache.Get<ChatState>(args.CallbackQuery.Message.Chat.Id);

            //if (cacheEntry == null)
            //{
            //    await _telegramBotClient.AnswerCallbackQueryAsync(args.CallbackQuery.Id);
            //    await _telegramBotClient.SendTextMessageAsync(args.CallbackQuery.Message.Chat.Id, "Sorry, this request is expired :(");
            //    return;
            //}

            //_cache.Remove(args.CallbackQuery.Message.Chat.Id);

            //var keyboardButtonConversationType = new KeybordButtonConversationType(args.CallbackQuery.Data);

            //if (keyboardButtonConversationType.Type != ConversationType.Image && keyboardButtonConversationType.Type != ConversationType.Pdf)
            //{
            //    await _telegramBotClient.SendTextMessageAsync(args.CallbackQuery.Message.Chat.Id, "Oh snap, this is not implemented, yet :(");
            //    return;
            //}

            //_workerService.QueueItem(new WorkItem
            //{
            //    ChatId = args.CallbackQuery.Message.Chat.Id,
            //    Files = cacheEntry.Files,
            //    ConversationType = keyboardButtonConversationType.Type
            //});

            //await _telegramBotClient.AnswerCallbackQueryAsync(args.CallbackQuery.Id);
            //await _telegramBotClient.SendTextMessageAsync(args.CallbackQuery.Message.Chat.Id, "I'll start to process your file as soon as possible :)");
        }

        private async void OnMessage(object sender, MessageEventArgs args)
        {
            if (args?.Message == null)
            {
                _logger.LogWarning("Received empty message oO? Skipping...");
                return;
            }

            _logger.LogInformation("Received message from FirstName: {0}, LastName: {1} with Id: {2}", args.Message.From.FirstName, args.Message.From.LastName, args.Message.From.Id);

            var chatId = args.Message.Chat.Id;

            if (_conversations.TryGetValue(chatId, out (ReaderWriterLockSlim readerWriterLock, ConversationStateMachine conversation) state))
            {
                await state.conversation.HandeMessageAsync(args.Message);
            }
            else
            {
                var conversation = new ConversationStateMachine(_telegramBotClient, _workerService, _conversationLogger, chatId);
                _conversations.TryAdd(chatId, (null, conversation)); // TODO: returns false?
                await conversation.HandeMessageAsync(args.Message);
            }
            

            //if (args.Message.Type == MessageType.TextMessage)
            //{
            //    _logger.LogInformation("The message is a text message: {0}", args.Message.Text);

            //    if (args.Message.Text?.StartsWith("/start", StringComparison.InvariantCultureIgnoreCase) == true)
            //    {
            //        await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id, "Hi, my name is ... Bla todo");
            //    }
            //    else if (args.Message.Text?.StartsWith("/queue", StringComparison.InvariantCultureIgnoreCase) == true)
            //    {
            //        var queueLength = _workerService.GetQueueLength();

            //        switch (queueLength)
            //        {
            //            case 0:
            //                await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id, "The queue is empty right now :)");
            //                break;

            //            case var i when (i <= 5):
            //                await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id, "Just a bit work right now, I'm hurrying :P");
            //                break;

            //            case var i when (i > 5):
            //                await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id, "Oh oh, a lot of work right now, please be patient -.-");
            //                break;
            //        }
            //    }
            //}
            //else if (args.Message.Type == MessageType.DocumentMessage || args.Message.Type == MessageType.PhotoMessage)
            //{
            //    if (args.Message.Document != null || args.Message.Photo != null)
            //    {
            //        if (args.Message.Type == MessageType.PhotoMessage)
            //        {
            //            _logger.LogInformation("Received photo from {0}", args.Message.From.Id);
            //            await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id,
            //                "You sent me a photo, Telegram uses heavy compression, please send me a file for better results.");
            //            await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id,
            //                "Please note that im currently only able to handle one photo :(");
            //        }

            //        var file = args.Message.Type == MessageType.DocumentMessage ? args.Message.Document : (File)args.Message.Photo.Last();

            //        if (file == null)
            //        {
            //            await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id,
            //                "Oh snap, something went wrong :(");
            //            return;
            //        }

            //        _logger.LogInformation("Received file from {0} with id {1}", args.Message.From.Id, file.FileId);

            //        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            //        {
            //            new[]
            //            {
            //                new InlineKeyboardCallbackButton("Image", new KeybordButtonConversationType(ConversationType.Image, args.Message.Chat.Id).ToString()), 
            //                new InlineKeyboardCallbackButton("PDF", new KeybordButtonConversationType(ConversationType.Pdf, args.Message.Chat.Id).ToString()),
            //                new InlineKeyboardCallbackButton("PDF/A", new KeybordButtonConversationType(ConversationType.Pdfa, args.Message.Chat.Id).ToString()),
            //            },
            //        });

            //        await _telegramBotClient.SendTextMessageAsync(args.Message.Chat.Id, "How should I convert your file?",
            //            replyMarkup: keyboard, replyToMessageId: args.Message.MessageId);

            //        var cacheEntry = await _cache.GetOrCreateAsync(args.Message.Chat.Id, entry =>
            //        {
            //            entry.SlidingExpiration = new TimeSpan(0, 15, 0);

            //            return Task.FromResult(new ChatState
            //            {
            //                MessageId = args.Message.MessageId,
            //                Files = new List<string>
            //                {
            //                    file.FileId
            //                }
            //            });
            //        });

            //        // TODO: In case the cacheEntry references the same object this access needs to be synchronized!
            //        cacheEntry.Files.Add(file.FileId);
            //        _cache.Set(args.Message.Chat.Id, cacheEntry, new TimeSpan(0, 15, 0));
            //    }
            //}
        }

        protected override Task ExecuteAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            _telegramBotClient.StartReceiving(null, cancellationToken);
            return Task.FromResult(0);
        }
    }
}
