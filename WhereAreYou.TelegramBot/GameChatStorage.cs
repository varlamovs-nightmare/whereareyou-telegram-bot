using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WhereAreYou.TelegramBot
{
    public class GameChatStorage
    {
        private readonly IDictionary<long, GameChat> Chats = new ConcurrentDictionary<long, GameChat>();
        
        public GameChat CreateNew(long chatId)
        {
            return Upsert(chatId, null, GameState.NotStarted);
        }
        
        public GameChat Upsert(long chatId, Guid? gameId, GameState state)
        {
            var chat = new GameChat(chatId, gameId, state);
            if (!Chats.ContainsKey(chatId))
            {
                Chats.Add(chatId, chat);
            }
            else
            {
                Chats[chatId] = chat;
            }

            return chat;
        }
        
        public GameChat FindGameChat(long chatId)
        {
            return Chats.ContainsKey(chatId) ? Chats[chatId] : null;
        }
        
        public void Delete(long chatId)
        {
            if (Chats.ContainsKey(chatId))
            {
                Chats.Remove(chatId);
            }
        }
    }
}