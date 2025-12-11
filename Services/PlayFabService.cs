using PlayFab;
using PlayFab.ClientModels;
using CardClickerRPG.Config;

namespace CardClickerRPG.Services
{
    public class PlayFabService
    {
        private string _playFabId;
        
        public string PlayFabId => _playFabId;

        public PlayFabService()
        {
            PlayFabSettings.staticSettings.TitleId = AppConfig.PlayFabTitleId;
        }

        // 커스텀 ID로 로그인 (자동 회원가입)
        public async Task<bool> LoginWithCustomIdAsync(string customId)
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = customId,
                CreateAccount = true, // 없으면 자동 생성
                TitleId = AppConfig.PlayFabTitleId
            };

            try
            {
                var result = await PlayFabClientAPI.LoginWithCustomIDAsync(request);
                
                if (result.Error != null)
                {
                    Console.WriteLine($"로그인 실패: {result.Error.ErrorMessage}");
                    return false;
                }

                _playFabId = result.Result.PlayFabId;
                Console.WriteLine($"로그인 성공! PlayFabId: {_playFabId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그인 예외: {ex.Message}");
                return false;
            }
        }

        // 리더보드 업데이트
        public async Task<bool> UpdateLeaderboardAsync(int deckPower)
        {
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = "DeckPower",  // 이 이름이 PlayFab에서 만든 리더보드 이름과 정확히 일치해야 함
                        Value = deckPower
                    }
                }
            };

            try
            {
                var result = await PlayFabClientAPI.UpdatePlayerStatisticsAsync(request);
                
                if (result.Error != null)
                {
                    Console.WriteLine($"리더보드 업데이트 실패: {result.Error.ErrorMessage}");
                    return false;
                }
                
                Console.WriteLine($"리더보드 업데이트 성공: {deckPower}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"리더보드 업데이트 예외: {ex.Message}");
                return false;
            }
        }

        // 리더보드 조회
        public async Task<List<(string PlayerName, int Score, int Rank)>> GetLeaderboardAsync(int maxResults = 10)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = "DeckPower",
                StartPosition = 0,
                MaxResultsCount = maxResults
            };

            try
            {
                var result = await PlayFabClientAPI.GetLeaderboardAsync(request);
                
                if (result.Error != null)
                    return new List<(string, int, int)>();

                var leaderboard = new List<(string, int, int)>();
                foreach (var entry in result.Result.Leaderboard)
                {
                    leaderboard.Add((
                        entry.DisplayName ?? entry.PlayFabId,
                        entry.StatValue,
                        entry.Position + 1
                    ));
                }

                return leaderboard;
            }
            catch
            {
                return new List<(string, int, int)>();
            }
        }
    }
}