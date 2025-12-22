using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CardClickerRPG.Models;
using Newtonsoft.Json;

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

        private async Task<T> CallLambdaAsync<T>(string url, object payload) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Lambda Error] {response.StatusCode} on {url}: {responseBody}");
                    return null;
                }

                // Directly deserialize to the target type T
                return JsonConvert.DeserializeObject<T>(responseBody);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[Lambda JsonException] on {url}: {ex.Message}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Lambda HttpRequestException] on {url}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lambda Exception] on {url}: {ex.Message}");
                return null;
            }
        }

        // 플레이어 조회
        public async Task<Player> GetPlayerAsync(string userId)
        {
            var response = await CallLambdaAsync<GetPlayerResponse>(GET_PLAYER_URL, new { userId });
            return response?.Player;
        }

        // 플레이어 생성
        public async Task<bool> CreatePlayerAsync(Player player)
        {
            var response = await CallLambdaAsync<SuccessResponse>(CREATE_PLAYER_URL, new { player });
            return response?.Success ?? false;
        }

        // 플레이어 업데이트
        public async Task<bool> UpdatePlayerAsync(Player player)
        {
            var response = await CallLambdaAsync<SuccessResponse>(UPDATE_PLAYER_URL, new { player });
            return response?.Success ?? false;
        }

        // 플레이어 카드 목록 조회
        public async Task<List<PlayerCard>> GetPlayerCardsAsync(string userId)
        {
            var response = await CallLambdaAsync<GetPlayerCardsResponse>(GET_PLAYER_CARDS_URL, new { userId });
            return response?.Cards ?? new List<PlayerCard>();
        }

        // 카드 추가
        public async Task<bool> AddPlayerCardAsync(PlayerCard card)
        {
            var response = await CallLambdaAsync<SuccessResponse>(ADD_PLAYER_CARD_URL, new { card });
            return response?.Success ?? false;
        }

        // 카드 삭제
        public async Task<bool> DeleteCardAsync(string userId, string instanceId)
        {
            var response = await CallLambdaAsync<SuccessResponse>(DELETE_CARD_URL, new { userId, instanceId });
            return response?.Success ?? false;
        }

        // 카드 강화
        public async Task<bool> UpgradeCardAsync(string userId, string instanceId, int newLevel)
        {
            var response = await CallLambdaAsync<SuccessResponse>(UPGRADE_CARD_URL, new { userId, instanceId, newLevel });
            return response?.Success ?? false;
        }

        // 카드 IsNew 업데이트
        public async Task<bool> UpdateCardIsNewAsync(string userId, string instanceId, bool isNew)
        {
            var response = await CallLambdaAsync<SuccessResponse>(UPDATE_CARD_IS_NEW_URL, new { userId, instanceId, isNew });
            return response?.Success ?? false;
        }

        // 카드 마스터 조회
        public async Task<CardMaster> GetCardMasterAsync(string cardId)
        {
            var response = await CallLambdaAsync<GetCardMasterResponse>(GET_CARD_MASTER_URL, new { cardId });
            return response?.CardMaster;
        }

        // 랜덤 카드 ID
        public async Task<string> GetRandomCardIdAsync()
        {
            var response = await CallLambdaAsync<GetRandomCardIdResponse>(GET_RANDOM_CARD_ID_URL, new { });
            return response?.CardId;
        }
    }
}