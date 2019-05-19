using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace WhereAreYou.TelegramBot
{
    public class ServiceClient
    {
        private IRestClient client;
        
        public ServiceClient()
        {
            client = new RestClient("http://localhost:8080");
        }

        public async Task<List<Game>> Games()
        {
            return await client
                .GetAsync<List<Game>>(new RestRequest("/api/games"))
                .ConfigureAwait(false);
        }
        
        public List<string> Cities()
        {
            var restResponse = client.Get<GetCityResult>(new RestRequest("/api/cities"));
            return restResponse.Data.Cities;
        }
        
        public async Task<Game> CreateGame(string city)
        {
            var restRequest = new RestRequest($"/api/games", Method.POST);
            restRequest.AddJsonBody(new {city});
            
            var game = await client
                .PostAsync<Game>(restRequest)
                .ConfigureAwait(false);
            
            Console.WriteLine(restRequest.Resource);
            
            return game;
        }
        
        public async Task<List<string>> GetTipsAsync(Guid gameId)
        {
            var getTipsResult = await client
                .GetAsync<GetTipsResult>(new RestRequest($"/api/games/{gameId}/tips"))
                .ConfigureAwait(false);
            
            return getTipsResult.Tips;
        }
        
        public async Task<List<string>> AskForTip(Guid gameId)
        {
            var getTipsResult = await client
                .PostAsync<GetTipsResult>(new RestRequest($"/api/games/{gameId}/ask-tip", Method.POST))
                .ConfigureAwait(false);
            
            return getTipsResult.Tips;
        }
        
        public async Task<List<string>> MoveTo(Guid gameId, string direction)
        {
            var getTipsResult = await client
                .PostAsync<GetTipsResult>(new RestRequest($"/api/games/{gameId}/move/{direction}", Method.POST))
                .ConfigureAwait(false);

            return getTipsResult.Tips;
        }
        
        public async Task<FinishGameResult> FinishGame(Guid gameId, double latitude, double longtitude)
        {
            return await client
                .PostAsync<FinishGameResult>(new RestRequest($"/api/games/{gameId}/finish/{latitude.ToString().Replace(",", ".")}/{longtitude.ToString().Replace(",", ".")}", Method.POST))
                .ConfigureAwait(false);
        }
    }

    public class Game
    {
        [JsonProperty("game_id")]
        public Guid GameId { get; set; }
        
        [JsonProperty("min_lat")]
        public double MinLat { get; set; }

        [JsonProperty("max_lat")]
        public double MaxLat { get; set; }

        [JsonProperty("min_lon")]
        public double MinLon { get; set; }
        
        [JsonProperty("max_lon")]
        public double MaxLon { get; set; }
    }
    
    public class FinishGameResult
    {
        [JsonProperty("game_id")]
        public Guid GameId { get; set; }
        
        [JsonProperty("right_coordinates")]
        public List<double> RightCoordinates { get; set; }

        [JsonProperty("distance")]
        public double Distance { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
        
        [JsonProperty("route")]
        public List<List<double>> Route { get; set; }
        
        [JsonProperty("score")]
        public double Score { get; set; }
    }

    public class GetCityResult
    {
        [JsonProperty("cities")]
        public List<string> Cities { get; set; }
    }
    
    public class GetTipsResult
    {
        [JsonProperty("tips")]
        public List<string> Tips { get; set; }
    }
}