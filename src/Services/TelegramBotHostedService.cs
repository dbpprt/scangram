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

            var hasConversation = _conversations.TryGetValue(chatId,
                out (ReaderWriterLockSlim readerWriterLock, ConversationStateMachine conversation) state);

            if (hasConversation && state.conversation.InProgress)
            {
                await state.conversation.HandeMessageAsync(args.Message);
                return;
            }

            if (hasConversation && !state.conversation.InProgress)
            {
                _conversations.TryRemove(chatId, out state);
            }
            
            var conversation =
                new ConversationStateMachine(_telegramBotClient, _workerService, _conversationLogger, chatId);
            _conversations.TryAdd(chatId, (null, conversation)); // TODO: returns false?
            await conversation.HandeMessageAsync(args.Message);
        }

        protected override Task ExecuteAsync(IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            _telegramBotClient.StartReceiving(null, cancellationToken);
            return Task.FromResult(0);
        }
    }
}
