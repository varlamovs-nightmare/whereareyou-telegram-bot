using System;
using System.Collections.Generic;

namespace WhereAreYou.TelegramBot
{
    public class GameChat
    {
        public long ChatId { get; }
        public Guid? GameId { get; }
        public GameState State { get; }
        
        public HashSet<string> AlreadySentTips { get; } = new HashSet<string>();

        public GameChat(long chatId, Guid? gameId = null, GameState state = GameState.NotStarted)
        {
            ChatId = chatId;
            GameId = gameId;
            State = state;
        }
    }
}