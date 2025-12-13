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
            
            foreach (var card in _playerCards)
            {
                card.MasterData = await _dynamoDBService.GetCardMasterAsync(card.CardId);
            }
            
            return true;
        }

        // 덱에서 활성화된 능력 개수 세기 (최대 3장 제한)
        private Dictionary<string, int> GetActiveAbilities()
        {
            var deck = GetDeck();
            var abilities = new Dictionary<string, int>();

            foreach (var card in deck)
            {
                if (card.MasterData?.Ability != null)
                {
                    string ability = card.MasterData.Ability;
                    if (!abilities.ContainsKey(ability))
                        abilities[ability] = 0;
                    
                    // 같은 능력은 최대 3장까지만 카운트
                    if (abilities[ability] < 3)
                    {
                        abilities[ability]++;
                    }
                }
            }

            return abilities;
        }

        // 클릭 배율 계산 (곱셈 중첩, 최대 ×8)
        private int GetClickMultiplier()
        {
            var abilities = GetActiveAbilities();
            int multiplier = 1;
            
            if (abilities.ContainsKey("CLICK_MULTIPLY"))
            {
                int count = abilities["CLICK_MULTIPLY"]; // 최대 3
                multiplier = (int)Math.Pow(2, count); // 2^count
            }
            
            return multiplier;
        }

        // 클릭
        public async Task<(bool cardObtained, PlayerCard newCard)> ClickAsync()
        {
            int clickMultiplier = GetClickMultiplier();
            _currentPlayer.ClickCount += clickMultiplier;
            _currentPlayer.TotalClicks += clickMultiplier;

            // 100 클릭 달성?
            if (_currentPlayer.ClickCount >= AppConfig.ClicksForCard)
            {
                _currentPlayer.ClickCount = 0;
                
                // 랜덤 카드 획득
                string randomCardId = await _dynamoDBService.GetRandomCardIdAsync();
                var cardMaster = await _dynamoDBService.GetCardMasterAsync(randomCardId);
                
                if (cardMaster == null)
                    return (false, null);

                // LUCKY 능력 체크 (등급 상승)
                var abilities = GetActiveAbilities();
                if (abilities.ContainsKey("LUCKY"))
                {
                    int luckyCount = abilities["LUCKY"];
                    int chance = luckyCount * 20; // 1장당 20% 확률 (최대 60%)
                    
                    var random = new Random();
                    if (random.Next(100) < chance)
                    {
                        cardMaster.Rarity = UpgradeRarity(cardMaster.Rarity);
                        Console.WriteLine($"[LUCKY] 등급 상승! → {cardMaster.Rarity}");
                    }
                }
                
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

        // 등급 상승
        private string UpgradeRarity(string rarity)
        {
            return rarity switch
            {
                "common" => "rare",
                "rare" => "epic",
                "epic" => "legendary",
                _ => rarity
            };
        }

        // 카드 분해
        public async Task<bool> DisenchantCardAsync(string instanceId)
        {
            var card = _playerCards.FirstOrDefault(c => c.InstanceId == instanceId);
            if (card == null || card.MasterData == null)
                return false;

            // 가루 획득
            int dustGain = AppConfig.GetDustByRarity(card.MasterData.Rarity);
            
            // DUST_BONUS 능력 적용
            var abilities = GetActiveAbilities();
            if (abilities.ContainsKey("DUST_BONUS"))
            {
                int bonusCount = abilities["DUST_BONUS"];
                float bonusMultiplier = 1.0f + (bonusCount * 0.5f); // 1장당 +50%
                dustGain = (int)(dustGain * bonusMultiplier);
                Console.WriteLine($"[DUST_BONUS] 가루 ×{bonusMultiplier:F1}!");
            }

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
            
            // UPGRADE_DISCOUNT 능력 적용
            var abilities = GetActiveAbilities();
            if (abilities.ContainsKey("UPGRADE_DISCOUNT"))
            {
                int discountCount = abilities["UPGRADE_DISCOUNT"];
                float discountMultiplier = 1.0f - (discountCount * 0.3f); // 1장당 -30%
                if (discountMultiplier < 0.1f) discountMultiplier = 0.1f; // 최소 10%
                
                cost = (int)(cost * discountMultiplier);
                Console.WriteLine($"[UPGRADE_DISCOUNT] 비용 할인! {discountMultiplier * 100:F0}%");
            }

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

        // 활성화된 능력 표시
        public void ShowActiveAbilities()
        {
            var abilities = GetActiveAbilities();
            
            if (abilities.Count == 0)
            {
                Console.WriteLine("활성화된 능력 없음");
                return;
            }

            Console.WriteLine("=== 덱 활성 능력 ===");
            foreach (var ability in abilities)
            {
                string desc = ability.Key switch
                {
                    "AUTO_CLICK" => $"자동 클릭 ×{ability.Value}",
                    "CLICK_MULTIPLY" => $"클릭 배율 ×{(int)Math.Pow(2, ability.Value)}",
                    "DUST_BONUS" => $"분해 보너스 +{ability.Value * 50}%",
                    "UPGRADE_DISCOUNT" => $"강화 할인 -{ability.Value * 30}%",
                    "LUCKY" => $"행운 {ability.Value * 20}%",
                    _ => ability.Key
                };
                Console.WriteLine($"  • {desc}");
            }
        }
    }
}