using System;

namespace WhereAreYou.TelegramBot
{
    internal class Program
    {
      

        private static void Main(string[] args)
        {
            var telegramBot = new TelegramBot(new GameChatStorage(), new ServiceClient());
            
            telegramBot.Start();
            
            Console.ReadLine();
            telegramBot.Stop();
        }

        
    }
}