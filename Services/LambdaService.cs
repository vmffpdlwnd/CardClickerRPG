using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CardClickerRPG.Models;

namespace CardClickerRPG.Services
{
    public class LambdaService
    {
        private readonly HttpClient _httpClient;
        
        // Lambda Function URLs
        private const string GET_PLAYER_URL = "https://v6hs3vpwqzoddosa5b7qjkudey0ayxbs.lambda-url.ap-northeast-2.on.aws/";
        private const string CREATE_PLAYER_URL = "https://soh7l2fkxzbjsmx2ziqltcawfe0kljjx.lambda-url.ap-northeast-2.on.aws/";
        private const string UPDATE_PLAYER_URL = "https://gwrei56ilaooh7s77awvrorrsq0rvnzf.lambda-url.ap-northeast-2.on.aws/";
        private const string GET_PLAYER_CARDS_URL = "https://lol3sm7xvil7di4mguevlk2sva0xukcd.lambda-url.ap-northeast-2.on.aws/";
        private const string ADD_PLAYER_CARD_URL = "https://46vfasnjekvoqez3a773j32y2a0bjpfa.lambda-url.ap-northeast-2.on.aws/";
        private const string DELETE_CARD_URL = "https://3kul7icolhv3gw2h7ppvrjjpia0gykij.lambda-url.ap-northeast-2.on.aws/";
        private const string UPGRADE_CARD_URL = "https://osk2cazo75xoqbb24ozksjtwui0wzblm.lambda-url.ap-northeast-2.on.aws/";
        private const string UPDATE_CARD_IS_NEW_URL = "https://ws3wtn4bleattq7libm3pzrkau0ftpsq.lambda-url.ap-northeast-2.on.aws/";
        private const string GET_CARD_MASTER_URL = "https://gd3ehsqahzb7wkcz7htve7oiiy0wmlmx.lambda-url.ap-northeast-2.on.aws/";
        private const string GET_RANDOM_CARD_ID_URL = "https://waitmpje5jdxhpqscacrgsh5m40ljycf.lambda-url.ap-northeast-2.on.aws/";

        public LambdaService()
        {
            _httpClient = new HttpClient();
        }

        private async Task<T> CallLambdaAsync<T>(string url, object payload)
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Lambda Error] {url}: {responseBody}");
                    return default(T);
                }

                var result = JObject.Parse(responseBody);
                var body = result["body"]?.ToString();
                
                if (string.IsNullOrEmpty(body))
                    return default(T);

                var data = JObject.Parse(body);
                return data.ToObject<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lambda Exception] {url}: {ex.Message}");
                return default(T);
            }
        }

        // 플레이어 조회
        public async Task<Player> GetPlayerAsync(string userId)
        {
            var result = await CallLambdaAsync<JObject>("getPlayer", new { userId });
            
            if (result?["player"] != null)
            {
                var playerData = result["player"];
                return new Player
                {
                    UserId = playerData["userId"]?.ToString(),
                    ClickCount = playerData["clickCount"]?.ToObject<int>() ?? 0,
                    Dust = playerData["dust"]?.ToObject<int>() ?? 0,
                    TotalClicks = playerData["totalClicks"]?.ToObject<int>() ?? 0,
                    DeckPower = playerData["deckPower"]?.ToObject<int>() ?? 0,
                    LastSaveTime = playerData["lastSaveTime"]?.ToString(),
                    DeckCardIds = playerData["deckCardIds"]?.ToObject<List<string>>() ?? new List<string>()
                };
            }
            
            return null;
        }

        // 플레이어 생성
        public async Task<bool> CreatePlayerAsync(Player player)
        {
            var result = await CallLambdaAsync<JObject>(CREATE_PLAYER_URL, new { player });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 플레이어 업데이트
        public async Task<bool> UpdatePlayerAsync(Player player)
        {
            var result = await CallLambdaAsync<JObject>(UPDATE_PLAYER_URL, new { player });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 플레이어 카드 목록 조회
        public async Task<List<PlayerCard>> GetPlayerCardsAsync(string userId)
        {
            var result = await CallLambdaAsync<JObject>(GET_PLAYER_CARDS_URL, new { userId });
            
            if (result?["cards"] is JArray cardsArray)
            {
                var cards = new List<PlayerCard>();
                
                foreach (var cardToken in cardsArray)
                {
                    cards.Add(new PlayerCard
                    {
                        UserId = cardToken["userId"]?.ToString() ?? userId,
                        InstanceId = cardToken["instanceId"]?.ToString() ?? Guid.NewGuid().ToString(),
                        CardId = cardToken["cardId"]?.ToString(),
                        Level = cardToken["level"]?.ToObject<int>() ?? 1,
                        AcquiredAt = cardToken["acquiredAt"]?.ToString() ?? DateTime.UtcNow.ToString("o"),
                        IsNew = cardToken["isNew"]?.ToObject<bool>() ?? true
                    });
                }
                
                return cards;
            }
            
            return new List<PlayerCard>();
        }

        // 카드 추가
        public async Task<bool> AddPlayerCardAsync(PlayerCard card)
        {
            var result = await CallLambdaAsync<JObject>(ADD_PLAYER_CARD_URL, new { card });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 삭제
        public async Task<bool> DeleteCardAsync(string userId, string instanceId)
        {
            var result = await CallLambdaAsync<JObject>(DELETE_CARD_URL, new { userId, instanceId });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 강화
        public async Task<bool> UpgradeCardAsync(string userId, string instanceId, int newLevel)
        {
            var result = await CallLambdaAsync<JObject>(UPGRADE_CARD_URL, new { userId, instanceId, newLevel });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 IsNew 업데이트
        public async Task<bool> UpdateCardIsNewAsync(string userId, string instanceId, bool isNew)
        {
            var result = await CallLambdaAsync<JObject>(UPDATE_CARD_IS_NEW_URL, new { userId, instanceId, isNew });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 마스터 조회
        public async Task<CardMaster> GetCardMasterAsync(string cardId)
        {
            var result = await CallLambdaAsync<JObject>(GET_CARD_MASTER_URL, new { cardId });
            
            if (result?["cardMaster"] != null)
            {
                var masterData = result["cardMaster"];
                return new CardMaster
                {
                    CardId = masterData["cardId"]?.ToString(),
                    Name = masterData["name"]?.ToString() ?? "Unknown",
                    Rarity = masterData["rarity"]?.ToString() ?? "common",
                    HP = masterData["hp"]?.ToObject<int>() ?? 100,
                    ATK = masterData["atk"]?.ToObject<int>() ?? 10,
                    DEF = masterData["def"]?.ToObject<int>() ?? 5,
                    Ability = masterData["ability"]?.ToString() ?? "NONE"
                };
            }
            
            return null;
        }

        // 랜덤 카드 ID
        public async Task<string> GetRandomCardIdAsync()
        {
            var result = await CallLambdaAsync<JObject>(GET_RANDOM_CARD_ID_URL, new { });
            return result?["cardId"]?.ToString();
        }
    }
}