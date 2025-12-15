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
        private const string API_BASE_URL = "https://h3ecwc0m9g.execute-api.ap-northeast-2.amazonaws.com/prod";

        public LambdaService()
        {
            _httpClient = new HttpClient();
        }

        private async Task<T> CallLambdaAsync<T>(string endpoint, object payload)
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{API_BASE_URL}/{endpoint}", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Lambda Error] {endpoint}: {responseBody}");
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
                Console.WriteLine($"[Lambda Exception] {endpoint}: {ex.Message}");
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
            var result = await CallLambdaAsync<JObject>("createPlayer", new { player });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 플레이어 업데이트
        public async Task<bool> UpdatePlayerAsync(Player player)
        {
            var result = await CallLambdaAsync<JObject>("updatePlayer", new { player });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 플레이어 카드 목록 조회
        public async Task<List<PlayerCard>> GetPlayerCardsAsync(string userId)
        {
            var result = await CallLambdaAsync<JObject>("getPlayerCards", new { userId });
            
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
            var result = await CallLambdaAsync<JObject>("addPlayerCard", new { card });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 삭제
        public async Task<bool> DeleteCardAsync(string userId, string instanceId)
        {
            var result = await CallLambdaAsync<JObject>("deleteCard", new { userId, instanceId });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 강화
        public async Task<bool> UpgradeCardAsync(string userId, string instanceId, int newLevel)
        {
            var result = await CallLambdaAsync<JObject>("upgradeCard", new { userId, instanceId, newLevel });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 IsNew 업데이트
        public async Task<bool> UpdateCardIsNewAsync(string userId, string instanceId, bool isNew)
        {
            var result = await CallLambdaAsync<JObject>("updateCardIsNew", new { userId, instanceId, isNew });
            return result?["success"]?.ToObject<bool>() ?? false;
        }

        // 카드 마스터 조회
        public async Task<CardMaster> GetCardMasterAsync(string cardId)
        {
            var result = await CallLambdaAsync<JObject>("getCardMaster", new { cardId });
            
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
            var result = await CallLambdaAsync<JObject>("getRandomCardId", new { });
            return result?["cardId"]?.ToString();
        }
    }
}