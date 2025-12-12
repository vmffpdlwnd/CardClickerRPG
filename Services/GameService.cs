using CardClickerRPG.Models;
using CardClickerRPG.Config;

namespace CardClickerRPG.Services
{
    public class GameService
    {
        private readonly PlayFabService _playFabService;
        private readonly DynamoDBService _dynamoDBService;
        
        private Player _currentPlayer;
        private List<PlayerCard> _playerCards;

        public Player CurrentPlayer => _currentPlayer;
        public List<PlayerCard> PlayerCards => _playerCards;

        public GameService(PlayFabService playFabService, DynamoDBService dynamoDBService)
        {
            _playFabService = playFabService;
            _dynamoDBService = dynamoDBService;
            _playerCards = new List<PlayerCard>();
        }

        // 게임 초기화 (로그인 후 데이터 로드)
        public async Task<bool> InitializeAsync()
        {
            string userId = _playFabService.PlayFabId;
            
            // 플레이어 데이터 로드
            _currentPlayer = await _dynamoDBService.GetPlayerAsync(userId);
            
            // 신규 플레이어면 생성
            if (_currentPlayer == null)
            {
                _currentPlayer = new Player { UserId = userId };
                await _dynamoDBService.CreatePlayerAsync(_currentPlayer);
                Console.WriteLine("신규 플레이어 생성!");
            }
            
            // 카드 데이터 로드
            _playerCards = await _dynamoDBService.GetPlayerCardsAsync(userId);
            Console.WriteLine($"[DEBUG] 로드된 카드 수: {_playerCards.Count}");
            
            // CardMaster 정보 로드
            foreach (var card in _playerCards)
            {
                card.MasterData = await _dynamoDBService.GetCardMasterAsync(card.CardId);
                Console.WriteLine($"[DEBUG] 카드 로드: {card.CardId} - {card.MasterData?.Name}");
            }
            
            return true;
        }

        // 클릭
        public async Task<(bool cardObtained, PlayerCard newCard)> ClickAsync()
        {
            _currentPlayer.ClickCount++;
            _currentPlayer.TotalClicks++;

            // 100 클릭 달성?
            if (_currentPlayer.ClickCount >= AppConfig.ClicksForCard)
            {
                _currentPlayer.ClickCount = 0;
                
                // 랜덤 카드 획득
                string randomCardId = await _dynamoDBService.GetRandomCardIdAsync();
                var cardMaster = await _dynamoDBService.GetCardMasterAsync(randomCardId);
                
                var newCard = new PlayerCard
                {
                    UserId = _currentPlayer.UserId,
                    CardId = randomCardId,
                    MasterData = cardMaster
                };
                
                await _dynamoDBService.AddPlayerCardAsync(newCard);
                _playerCards.Add(newCard);
                
                // 덱 전투력 재계산
                await RecalculateDeckPowerAsync();
                
                return (true, newCard);
            }

            return (false, null);
        }

        // 카드 분해
        public async Task<bool> DisenchantCardAsync(string instanceId)
        {
            var card = _playerCards.FirstOrDefault(c => c.InstanceId == instanceId);
            if (card == null || card.MasterData == null)
                return false;

            // 가루 획득
            int dustGain = AppConfig.GetDustByRarity(card.MasterData.Rarity);
            _currentPlayer.Dust += dustGain;

            // 카드 삭제
            await _dynamoDBService.DeleteCardAsync(_currentPlayer.UserId, instanceId);
            _playerCards.Remove(card);

            // 덱 전투력 재계산
            await RecalculateDeckPowerAsync();

            Console.WriteLine($"[{card.MasterData.Rarity}] {card.MasterData.Name} 분해 → 가루 +{dustGain}");
            return true;
        }

        // 카드 강화
        public async Task<bool> UpgradeCardAsync(string instanceId)
        {
            var card = _playerCards.FirstOrDefault(c => c.InstanceId == instanceId);
            if (card == null || card.MasterData == null)
                return false;

            // 강화 비용 확인
            int cost = AppConfig.GetUpgradeCost(card.Level);
            if (_currentPlayer.Dust < cost)
            {
                Console.WriteLine($"가루 부족! (필요: {cost}, 보유: {_currentPlayer.Dust})");
                return false;
            }

            // 강화 실행
            _currentPlayer.Dust -= cost;
            card.Level++;
            
            await _dynamoDBService.UpgradeCardAsync(_currentPlayer.UserId, instanceId, card.Level);

            // 덱 전투력 재계산
            await RecalculateDeckPowerAsync();

            Console.WriteLine($"{card.MasterData.Name} Lv.{card.Level - 1} → Lv.{card.Level} (가루 -{cost})");
            return true;
        }

        // 덱 전투력 재계산
        public async Task RecalculateDeckPowerAsync()
        {
            // 전투력 높은 순으로 상위 5장 선택
            var topCards = _playerCards
                .OrderByDescending(c => c.GetPower())
                .Take(5)
                .ToList();

            _currentPlayer.DeckPower = topCards.Sum(c => c.GetPower());
            
            // PlayFab 리더보드 업데이트
            await _playFabService.UpdateLeaderboardAsync(_currentPlayer.DeckPower);
        }

        // 내 카드 보기 (전투력 순)
        public List<PlayerCard> GetCardsSortedByPower()
        {
            return _playerCards
                .OrderByDescending(c => c.GetPower())
                .ToList();
        }

        // 내 덱 보기 (상위 5장)
        public List<PlayerCard> GetDeck()
        {
            return _playerCards
                .OrderByDescending(c => c.GetPower())
                .Take(5)
                .ToList();
        }

        // 리더보드 조회
        public async Task<List<(string PlayerName, int Score, int Rank)>> GetLeaderboardAsync()
        {
            return await _playFabService.GetLeaderboardAsync(10);
        }

        // 저장
        public async Task<bool> SaveAsync()
        {
            bool success = await _dynamoDBService.UpdatePlayerAsync(_currentPlayer);
            if (success)
            {
                Console.WriteLine("저장 완료!");
            }
            return success;
        }
    }
}