using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace WhereAreYou.TelegramBot
{
    public class TelegramBot
    {
        private readonly GameChatStorage gameChatStorage;
        private readonly ServiceClient serviceClient;
        private readonly TelegramBotClient Bot;
        
        public TelegramBot(GameChatStorage gameChatStorage, ServiceClient serviceClient)
        {
            Bot = new TelegramBotClient("your-api-key");
            this.gameChatStorage = gameChatStorage;
            this.serviceClient = serviceClient;
        }

        public void Start()
        {
            var me = Bot.GetMeAsync().Result;
            Console.Title = me.Username;
            
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");   
        }
        
        public void Stop()
        {
            Bot.StopReceiving();
        }
        
        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            var chatId = message.Chat.Id;

            await GlobalHandle(chatId, message);


        }

        private async Task GlobalHandle(long chatId, Message message)
        {
            var gameChat = gameChatStorage.FindGameChat(chatId);
            if (gameChat == null)
            {
                await HandleGameNotExists(message, chatId);
                return;
            }

            if (gameChat.State == GameState.NeedCityChoose)
            {
                await HandleCityChoose(message.Text, chatId);
                return;
            }

            if (gameChat.State == GameState.Started)
            {
                await HandleGameStarted(message, chatId);
                return;
            }
        }

        private async Task HandleGameNotExists(Message message, long chatId)
        {
            var userName = message.Chat.FirstName ?? message.Chat.Username;

            gameChatStorage.CreateNew(chatId);

            await Bot.SendTextMessageAsync(
                message.Chat.Id,
                $"Привет, {userName}! Давай сыграем в игру. Я загадаю какое-то место в городе, а ты попробуемшь его отгадать. " +
                $"Чем правильнее будет твой ответ, тем больше баллов заработаешь!");

            gameChatStorage.Upsert(chatId, null, GameState.NeedCityChoose);

            var cities = serviceClient.Cities();
          
            await SendChooseCity(chatId, cities);
        }

        private async Task SendChooseCity(long chatId, List<string> cities)
        {
            await Bot.SendTextMessageAsync(
                chatId,
                "Для начала, выбери город, в котором хочешь сыграть:",
                replyMarkup: new ReplyKeyboardMarkup(cities
                    .Select(e => new KeyboardButton(e))
                    .ToArray()
                    .ChunkBy(3)));
        }

        private async Task HandleCityChoose(string city, long chatId)
        {
            var cities = serviceClient.Cities();

            var chosenCity = cities.FirstOrDefault(e => e == city);
            if (chosenCity == null)
            {
                await SendChooseCity(chatId, cities);
                return;
            }
            
            var game = await serviceClient.CreateGame(chosenCity);
            
            gameChatStorage.Upsert(chatId, game.GameId, GameState.Started);
          
            await Bot.SendTextMessageAsync(
                chatId,
                "Отлично, будем играть в " + chosenCity + "!");

            await SendNewTip(chatId);
        }
        
        private async Task HandleGameStarted(Message message, long chatId)
        {
            var chatGame = gameChatStorage.FindGameChat(chatId);

            var command = message.Text;

            if (message.Location != null)
            {
                var latitude = message.Location.Latitude;
                var longitude = message.Location.Longitude;
                
                var finishResult = await serviceClient.FinishGame(chatGame.GameId.Value, latitude, longitude);
                
                await Bot.SendTextMessageAsync(
                    chatId,
                    "Правильный ответ:" );
                
                var rightLatitude = finishResult.RightCoordinates[0];
                var rightLongtitude = finishResult.RightCoordinates[1];
                
                await Bot.SendLocationAsync(chatId, (float) rightLatitude, (float) rightLongtitude);

                await Bot.SendTextMessageAsync(
                    chatId,
                    $"Вы ошиблись на {Math.Round(finishResult.Distance, 0, MidpointRounding.AwayFromZero)} метров и заработали {Math.Round(finishResult.Score, 0, MidpointRounding.AwayFromZero)} очков. Поздравляю!");
                    
                gameChatStorage.Delete(chatId);
                
                var rows = new[]
                {
                    new KeyboardButton("Играть еще раз")
                }.ChunkBy(2);
            
                var keyboard = new ReplyKeyboardMarkup(
                    rows,
                    true,
                    true);
            
                await Bot
                    .SendTextMessageAsync(
                        chatId,
                        "Хотите сыграть еще?",
                        disableWebPagePreview: true,
                        replyMarkup: keyboard)
                    .ConfigureAwait(false);


                return;

            }

            if (command == "Хочу подсказку")
            {
                await serviceClient.AskForTip(chatGame.GameId.Value);
                await SendNewTip(chatId);
                return;
            }
            
            if (command == "Иду на север")
            {
                await serviceClient.MoveTo(chatGame.GameId.Value, "north");
                await SendNewTip(chatId);
                return;
            }
            
            if (command == "Иду на юг")
            {
                await serviceClient.MoveTo(chatGame.GameId.Value, "south");
                await SendNewTip(chatId);
                await SendNewTip(chatId);
                return;
            }
            
            if (command == "Иду на восток")
            {
                await serviceClient.MoveTo(chatGame.GameId.Value, "east");
                await SendNewTip(chatId);
                await SendNewTip(chatId);
                return;
            }
            
            if (command == "Иду на запад")
            {
                await serviceClient.MoveTo(chatGame.GameId.Value, "west");
                await SendNewTip(chatId);
                await SendNewTip(chatId);
                return;
            }
        }
        
        private async Task SendNewTip(long chatId)
        {
            var gameChat = gameChatStorage.FindGameChat(chatId);

            var allTips = await serviceClient.GetTipsAsync(gameChat.GameId.Value);
            var tipsToSend = allTips.Where(tip  => !gameChat.AlreadySentTips.Contains(tip));

            if (!tipsToSend.Any())
            {
                await Bot.SendTextMessageAsync(
                    chatId,
                    "К сожалению, вокруг больше ничего интересного, попробуй пройти немного в какую-нибудь сторону" );
                await SendKeyboard(chatId);

            }
            else
            {
                var first = tipsToSend.First();
                
                gameChat.AlreadySentTips.Add(first);

                await Bot.SendTextMessageAsync(chatId, first);
                await SendKeyboard(chatId);
            }
        }
        
        private async Task SendKeyboard(long chatId)
        {
            var rows = new[]
            {
                new KeyboardButton("Иду на север"),
                new KeyboardButton("Иду на юг"),
                new KeyboardButton("Иду на запад"),
                new KeyboardButton("Иду на восток"),
                new KeyboardButton("Хочу подсказку")
            }.ChunkBy(2);
            
            
            var keyboard = new ReplyKeyboardMarkup(
                rows,
                true,
                true);
            
            await Bot
                .SendTextMessageAsync(
                    chatId,
                    "Отправьте Локейшн точки, если поняли где находитесь",
                    disableWebPagePreview: true,
                    replyMarkup: keyboard)
                .ConfigureAwait(false);
        }

        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message);
        }
     
    }
}