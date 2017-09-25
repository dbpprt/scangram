using System;
using Telegram.Bot.Types;

namespace Scangram.Services.DataTransfer
{
    public class KeybordButtonConversationType
    {
        private readonly string _serializedString;

        public ConversationType Type { get; }
        public ChatId ChatId { get; }

        public KeybordButtonConversationType(string serializedString)
        {
            _serializedString = serializedString;

            var parts = _serializedString.Split(':');
            
            Type = (ConversationType) int.Parse(parts[1]);
            ChatId = new ChatId(long.Parse(parts[0]));
        }

        public KeybordButtonConversationType(ConversationType type, ChatId chatId)
        {
            Type = type;
            ChatId = chatId;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", ChatId, (int)Type);
        }
    }
}
