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

        public async Task<bool> InitializeAsync()
        {
            string userId = _playFabService.PlayFabId;
            
            // 플레이어 데이터 로드
            _currentPlayer = await _dynamoDBService.GetPlayerAsync(userId);
            
            if (_currentPlayer == null)
            {
                _currentPlayer = new Player { UserId = userId };
                await _dynamoDBService.CreatePlayerAsync(_currentPlayer);
            }
            
            // 카드 데이터 로드
            _playerCards = await _dynamoDBService.GetPlayerCardsAsync(userId);
            
            // CardMaster 정보 로드 및 유효하지 않은 카드 제거
            var invalidCards = new List<PlayerCard>();
            for (int i = 0; i < _playerCards.Count; i++)
            {
                var card = _playerCards[i];
                card.MasterData = await _dynamoDBService.GetCardMasterAsync(card.CardId);
                
                if (card.MasterData == null)
                {
                    invalidCards.Add(card);
                }
            }
            
            // 유효하지 않은 카드 삭제
            foreach (var card in invalidCards)
            {
                await _dynamoDBService.DeleteCardAsync(userId, card.InstanceId);
                _playerCards.Remove(card);
            }
            
            if (invalidCards.Count > 0)
            {
                Console.WriteLine($"[INFO] 유효하지 않은 카드 {invalidCards.Count}개 제거됨");
            }
            
            // 디버그 메시지 제거 (모든 [DEBUG] Console.WriteLine 삭제)
            
            return true;
        }

        public async Task<(bool cardObtained, PlayerCard newCard)> ClickAsync()
        {
            _currentPlayer.ClickCount++;
            _currentPlayer.TotalClicks++;

            if (_currentPlayer.ClickCount >= AppConfig.ClicksForCard)
            {
                _currentPlayer.ClickCount = 0;
                
                string randomCardId = await _dynamoDBService.GetRandomCardIdAsync();
                Console.WriteLine($"[DEBUG] 랜덤 카드 ID 생성: {randomCardId}");
                
                var cardMaster = await _dynamoDBService.GetCardMasterAsync(randomCardId);
                
                if (cardMaster == null)
                {
                    Console.WriteLine($"[ERROR] CardMaster 조회 실패: {randomCardId}");
                    return (false, null);
                }
                
                Console.WriteLine($"[DEBUG] CardMaster 로드 성공: {cardMaster.Name}");
                
                var newCard = new PlayerCard
                {
                    UserId = _currentPlayer.UserId,
                    CardId = randomCardId,
                    MasterData = cardMaster
                };
                
                Console.WriteLine($"[DEBUG] PlayerCard 생성: instanceId={newCard.InstanceId}");
                
                bool addSuccess = await _dynamoDBService.AddPlayerCardAsync(newCard);
                Console.WriteLine($"[DEBUG] DB 저장 결과: {addSuccess}");
                
                if (addSuccess)
                {
                    _playerCards.Add(newCard);
                    Console.WriteLine($"[DEBUG] 메모리 추가 완료, 현재 카드 수: {_playerCards.Count}");
                }
                
                await RecalculateDeckPowerAsync();
                
                return (true, newCard);
            }

            return (false, null);
        }

        public async Task<bool> DisenchantCardAsync(string instanceId)
        {
            var card = _playerCards.FirstOrDefault(c => c.InstanceId == instanceId);
            if (card == null || card.MasterData == null)
                return false;

            int dustGain = AppConfig.GetDustByRarity(card.MasterData.Rarity);
            _currentPlayer.Dust += dustGain;

            await _dynamoDBService.DeleteCardAsync(_currentPlayer.UserId, instanceId);
            _playerCards.Remove(card);

            await RecalculateDeckPowerAsync();

            Console.WriteLine($"[{card.MasterData.Rarity}] {card.MasterData.Name} 분해 → 가루 +{dustGain}");
            return true;
        }

        public async Task<bool> UpgradeCardAsync(string instanceId)
        {
            var card = _playerCards.FirstOrDefault(c => c.InstanceId == instanceId);
            if (card == null || card.MasterData == null)
                return false;

            int cost = AppConfig.GetUpgradeCost(card.Level);
            if (_currentPlayer.Dust < cost)
            {
                Console.WriteLine($"가루 부족! (필요: {cost}, 보유: {_currentPlayer.Dust})");
                return false;
            }

            _currentPlayer.Dust -= cost;
            card.Level++;
            
            await _dynamoDBService.UpgradeCardAsync(_currentPlayer.UserId, instanceId, card.Level);

            await RecalculateDeckPowerAsync();

            Console.WriteLine($"{card.MasterData.Name} Lv.{card.Level - 1} → Lv.{card.Level} (가루 -{cost})");
            return true;
        }

        public async Task RecalculateDeckPowerAsync()
        {
            var topCards = _playerCards
                .OrderByDescending(c => c.GetPower())
                .Take(5)
                .ToList();

            _currentPlayer.DeckPower = topCards.Sum(c => c.GetPower());
            
            await _playFabService.UpdateLeaderboardAsync(_currentPlayer.DeckPower);
        }

        public List<PlayerCard> GetCardsSortedByPower()
        {
            return _playerCards
                .OrderByDescending(c => c.GetPower())
                .ToList();
        }

        public List<PlayerCard> GetDeck()
        {
            return _playerCards
                .OrderByDescending(c => c.GetPower())
                .Take(5)
                .ToList();
        }

        public async Task<List<(string PlayerName, int Score, int Rank)>> GetLeaderboardAsync()
        {
            return await _playFabService.GetLeaderboardAsync(10);
        }

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