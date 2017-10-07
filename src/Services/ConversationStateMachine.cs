using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scangram.Services.Contracts;
using Scangram.Services.DataTransfer;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scangram.Services
{
    public partial class ConversationStateMachine
    {
        public enum State
        {
            None = 0,
            ConversationStarted = 0,
            AddingFiles = 1,
            AllFilesCollected = 2
        }

        private readonly ITelegramBotClient _telegramBotClient;
        private readonly IWorkerService _workerService;
        private readonly ILogger<ConversationStateMachine> _logger;
        private readonly ChatId _chatId;

        public bool InProgress = true;

        private State _state = State.None;
        private DocumentAction _documentAction = DocumentAction.NotSet;
        private ConversationType _conversationType = ConversationType.NotSet;

        private readonly Dictionary<Guid, Func<CallbackQuery, Task>> _callbackQueryHandlers;
        private readonly List<string> _files;

        public ConversationStateMachine(ITelegramBotClient telegramBotClient, IWorkerService workerService,
            ILogger<ConversationStateMachine> logger, ChatId chatId)
        {
            _telegramBotClient = telegramBotClient;
            _workerService = workerService;
            _logger = logger;
            _chatId = chatId;

            _callbackQueryHandlers = new Dictionary<Guid, Func<CallbackQuery, Task>>();
            _files = new List<string>();
        }

        public async Task HandeMessageAsync(Message message)
        {
            if (message.Type == MessageType.TextMessage)
            {
                _logger.LogInformation("The message is a text message: {0}", message.Text);

                if (message.Text?.StartsWith("/start", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "Hi, my name is ... Bla todo");
                }
                else if (message.Text?.StartsWith("/queue", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    var queueLength = _workerService.GetQueueLength();

                    switch (queueLength)
                    {
                        case 0:
                            await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "The queue is empty right now :)");
                            break;

                        case var i when (i <= 5):
                            await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "Just a bit work right now, I'm hurrying :P");
                            break;

                        case var i when (i > 5):
                            await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "Oh oh, a lot of work right now, please be patient -.-");
                            break;
                    }
                }
            }

            if (_state == State.None)
            {
                _state = State.ConversationStarted;

                if (message.Type == MessageType.DocumentMessage || message.Type == MessageType.PhotoMessage)
                {
                    await HandleFile(message);

                    if (_documentAction == DocumentAction.NotSet)
                    {
                        await AskForDocumentActionAsync(message);
                    }
                    else
                    {
                        await AskForAnotherFileAsync(message);
                    }
                }
                else
                {
                    // TODO: Send message with usage!
                }
            }
        }

        private string CreateCallbackQueryData(Func<CallbackQuery, Task> handler)
        {
            var id = Guid.NewGuid();
            _callbackQueryHandlers.Add(id, handler);
            return id.ToString();
        }

        private async Task HandleDocumentActionAsync(CallbackQuery query, DocumentAction action)
        {
            _documentAction = action;
            await _telegramBotClient.AnswerCallbackQueryAsync(query.Id);
            await _telegramBotClient.SendTextMessageAsync(_chatId, "Okay no problem.",
                replyToMessageId: query.Message.MessageId);

            await AskForConversationTypeAsync(query.Message);
        }

        private async Task HandleConversationTypeAsync(CallbackQuery query, ConversationType conversationType)
        {
            _conversationType = conversationType;
            await _telegramBotClient.AnswerCallbackQueryAsync(query.Id);
            await _telegramBotClient.SendTextMessageAsync(_chatId, "Okay no problem.",
                replyToMessageId: query.Message.MessageId);

            await AskForAnotherFileAsync(query.Message);
        }

        private async Task HandleAnotherFileAsync(CallbackQuery query, AddFileAction addFileAction)
        {
            await _telegramBotClient.AnswerCallbackQueryAsync(query.Id);

            if (addFileAction == AddFileAction.Yes)
            {
                await _telegramBotClient.SendTextMessageAsync(_chatId, "Just send me the next file!",
                    replyToMessageId: query.Message.MessageId);
            }
            else
            {
                _workerService.QueueItem(new WorkItem
                {
                    ChatId = _chatId.Identifier,
                    ConversationType = _conversationType,
                    DocumentAction = _documentAction,
                    Files = _files
                });

                await _telegramBotClient.SendTextMessageAsync(_chatId, "Please be patient, your conversation is in progress. You can start over again, I'll ping you as soon as I'm done.",
                    replyToMessageId: query.Message.MessageId);
                InProgress = false;
            }
        }

        private async Task AskForDocumentActionAsync(Message message)
        {
            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                new[]
                {
                    new InlineKeyboardCallbackButton("As it is", CreateCallbackQueryData(async query =>
                    {
                        await HandleDocumentActionAsync(query, DocumentAction.None);
                    })),
                    new InlineKeyboardCallbackButton("With document detection", CreateCallbackQueryData(async query =>
                    {
                        await HandleDocumentActionAsync(query, DocumentAction.DocumentScan);
                    }))
                },
            });

            await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "How should I handle your file? 'As it is' will not modify your image and 'With document detection' tries to extract a document from your image.",
                replyMarkup: keyboard, replyToMessageId: message.MessageId);
        }

        private async Task AskForAnotherFileAsync(Message message)
        {
            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                new[]
                {
                    new InlineKeyboardCallbackButton("Yes", CreateCallbackQueryData(async query =>
                    {
                        await HandleAnotherFileAsync(query, AddFileAction.Yes);
                    })),
                    new InlineKeyboardCallbackButton("No, I'm done.", CreateCallbackQueryData(async query =>
                    {
                        await HandleAnotherFileAsync(query, AddFileAction.No);
                    }))
                },
            });

            await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "Do you want to add another file?",
                replyMarkup: keyboard, replyToMessageId: message.MessageId);
        }

        private async Task AskForConversationTypeAsync(Message message)
        {
            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                new[]
                {
                    new InlineKeyboardCallbackButton("Image", CreateCallbackQueryData(async query =>
                    {
                        await HandleConversationTypeAsync(query, ConversationType.Image);
                    })),
                    new InlineKeyboardCallbackButton("PDF", CreateCallbackQueryData(async query =>
                    {
                        await HandleConversationTypeAsync(query, ConversationType.Pdf);
                    })),
                    new InlineKeyboardCallbackButton("PDF/A (OCR)", CreateCallbackQueryData(async query =>
                    {
                        await HandleConversationTypeAsync(query, ConversationType.Pdfa);
                    })),
                },
            });

            await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "How should I convert your file?",
                replyMarkup: keyboard, replyToMessageId: message.MessageId);
        }

        public async Task HandleCallbackQueryAsync(CallbackQuery query)
        {
            var data = query.Data;

            if (Guid.TryParse(data, out var id))
            {
                if (_callbackQueryHandlers.TryGetValue(id, out var handler))
                {
                    if (handler == null)
                    {
                        await _telegramBotClient.SendTextMessageAsync(query.Message.Chat.Id, "You already answered this question :).",
                            replyToMessageId: query.Message.MessageId);
                    }
                    else
                    {
                        await handler(query);
                        _callbackQueryHandlers[id] = null;
                    }
                }
                else
                {
                    await _telegramBotClient.SendTextMessageAsync(query.Message.Chat.Id, "Hmm, I'm not aware about this action :/.",
                        replyToMessageId: query.Message.MessageId);
                }

                await _telegramBotClient.AnswerCallbackQueryAsync(query.Id);
            }
            else
            {
                _logger.LogWarning("Callback query with invalid data received from chat {0} and data {1}", _chatId, query.Data);
            }
        }

        public async Task HandleFile(Message message)
        {
            if (message.Document != null || message.Photo != null)
            {
                if (message.Type == MessageType.PhotoMessage)
                {
                    _logger.LogInformation("Received photo from {0}", message.From.Id);
                    await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        "You sent me a photo, Telegram uses heavy compression, please send me a file for better results.");
                    await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        "Please note that im currently only able to handle one photo :(");
                }

                var file = message.Type == MessageType.DocumentMessage
                    ? message.Document
                    : (File)message.Photo.Last();

                if (file == null)
                {
                    await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        "Oh snap, something went wrong :(");
                }

                _logger.LogInformation("Received file from {0} with id {1}", message.From.Id, file.FileId);
                
                _files.Add(file.FileId);
            }
        }
    }
}
