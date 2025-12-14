using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CardClickerRPG.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                    return default;
                }

                // API Gateway의 전체 응답을 JObject로 파싱
                var gatewayResponse = JObject.Parse(responseBody);
                // 실제 Lambda 응답은 'body' 프로퍼티에 문자열 형태로 존재
                var lambdaBody = gatewayResponse["body"]?.ToString();

                if (string.IsNullOrEmpty(lambdaBody))
                {
                    Console.WriteLine($"[Lambda Warning] {endpoint}: Response body is null or empty.");
                    return default;
                }
                
                // Lambda의 'body' 문자열을 실제 타겟 객체로 역직렬화
                return JsonConvert.DeserializeObject<T>(lambdaBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lambda Exception] {endpoint}: {ex.Message}");
                return default;
            }
        }

        // 플레이어 조회
        public async Task<Player> GetPlayerAsync(string userId)
        {
            var response = await CallLambdaAsync<GetPlayerResponse>("getPlayer", new { userId });
            return response?.Player;
        }

        // 플레이어 카드 목록 조회
        public async Task<List<PlayerCard>> GetPlayerCardsAsync(string userId)
        {
            var response = await CallLambdaAsync<GetPlayerCardsResponse>("getPlayerCards", new { userId });
            return response?.Cards ?? new List<PlayerCard>();
        }

        // 카드 마스터 조회
        public async Task<CardMaster> GetCardMasterAsync(string cardId)
        {
            var response = await CallLambdaAsync<GetCardMasterResponse>("getCardMaster", new { cardId });
            return response?.CardMaster;
        }
    }
}