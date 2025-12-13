using CardClickerRPG.Services;
using CardClickerRPG.Config;

namespace CardClickerRPG
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 카드 클릭커 RPG ===");
            Console.WriteLine();

            // 서비스 초기화
            var playFabService = new PlayFabService();
            var gameService = new GameService(playFabService);

            // 로그인
            Console.Write("사용자 ID 입력 (아무거나): ");
            string customId = Console.ReadLine();
            
            Console.WriteLine("로그인 중...");
            bool loginSuccess = await playFabService.LoginWithCustomIdAsync(customId);
            
            if (!loginSuccess)
            {
                Console.WriteLine("로그인 실패!");
                return;
            }

            // 게임 데이터 로드
            Console.WriteLine("데이터 로딩 중...");
            await gameService.InitializeAsync();
            
            Console.WriteLine("로딩 완료!");
            Console.WriteLine();

            // 메인 루프
            bool running = true;
            while (running)
            {
                ShowMainMenu(gameService);
                
                string input = Console.ReadLine();
                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        await HandleClick(gameService);
                        break;
                    case "2":
                        await ShowMyCards(gameService);
                        break;
                    case "3":
                        await HandleDeckManagement(gameService);
                        break;
                    case "4":
                        await HandleDisenchant(gameService);
                        break;
                    case "5":
                        await HandleUpgrade(gameService);
                        break;
                    case "6":
                        await ShowLeaderboard(gameService);
                        break;
                    case "7":
                        await gameService.SaveAsync();
                        break;
                    case "8":
                        Console.WriteLine("저장 중...");
                        await gameService.SaveAsync();
                        gameService.StopAutoClick();  // 타이머 정지
                        Console.WriteLine("게임 종료!");
                        running = false;
                        break;
                    default:
                        Console.WriteLine("잘못된 입력!");
                        break;
                }

                if (running)
                {
                    Console.WriteLine("\n계속하려면 Enter...");
                    Console.ReadLine();
                }
            }
        }

        static void ShowMainMenu(GameService game)
        {
            Console.Clear();
            Console.WriteLine("=== 카드 클릭커 RPG ===");
            Console.WriteLine($"현재 클릭: {game.CurrentPlayer.ClickCount}/{AppConfig.ClicksForCard}");
            Console.WriteLine($"보유 가루: {game.CurrentPlayer.Dust}");
            Console.WriteLine($"보유 카드: {game.PlayerCards.Count}장");
            Console.WriteLine($"덱 전투력: {game.CurrentPlayer.DeckPower}");
            Console.WriteLine();
            Console.WriteLine("[1] 클릭하기");
            Console.WriteLine("[2] 내 카드 보기");
            Console.WriteLine("[3] 내 덱 편성");
            Console.WriteLine("[4] 카드 분해");
            Console.WriteLine("[5] 카드 강화");
            Console.WriteLine("[6] 리더보드");
            Console.WriteLine("[7] 저장하기");
            Console.WriteLine("[8] 종료");
            Console.WriteLine();
            Console.Write("선택: ");
        }

        static async Task HandleClick(GameService game)
        {
            var (cardObtained, newCard) = await game.ClickAsync();
            
            if (cardObtained && newCard?.MasterData != null)
            {
                Console.WriteLine("★ 카드 획득! ★");
                Console.WriteLine($"[{newCard.MasterData.Rarity}] {newCard.MasterData.Name}");
                Console.WriteLine($"HP:{newCard.MasterData.HP} ATK:{newCard.MasterData.ATK} DEF:{newCard.MasterData.DEF}");
                Console.WriteLine($"전투력: {newCard.GetPower()}");
                Console.WriteLine($"능력: {newCard.MasterData.GetAbilityDescription()}");
            }
            else
            {
                Console.WriteLine($"클릭! ({game.CurrentPlayer.ClickCount}/{AppConfig.ClicksForCard})");
            }
        }

        static async Task ShowMyCards(GameService game)
        {
            var cards = game.GetCardsSortedByPower();
            var deck = game.GetDeck();
            var deckIds = deck.Select(c => c.InstanceId).ToHashSet();
            
            if (cards.Count == 0)
            {
                Console.WriteLine("보유한 카드가 없습니다.");
                return;
            }

            Console.WriteLine("=== 보유 카드 목록 (전투력 순) ===");
            Console.WriteLine();
            
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card.MasterData == null) continue;

                Console.Write($"{i + 1}. ");
                
                // NEW 태그 (노란색 강조)
                if (card.IsNew)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("(NEW) ");
                    Console.ResetColor();
                }
                
                // 덱 편성 중 태그 (청록색)
                if (deckIds.Contains(card.InstanceId))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("[덱 편성 중] ");
                    Console.WriteLine($"[{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"[{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level}");
                }
                
                Console.WriteLine($"   HP:{card.MasterData.HP} ATK:{card.MasterData.ATK} DEF:{card.MasterData.DEF} | 전투력: {card.GetPower()}");
                Console.WriteLine($"   능력: {card.MasterData.GetAbilityDescription()}");
            }
            
            // 카드를 확인한 후 NEW 플래그 제거 (DB 반영)
            await game.ClearNewFlagsAsync();
        }

        static async Task HandleDeckManagement(GameService game)
        {
            while (true)
            {
                Console.Clear();
                
                // 덱 표시
                var deck = game.GetDeck();
                
                if (deck.Count == 0)
                {
                    Console.WriteLine("덱에 카드가 없습니다.");
                    Console.WriteLine("\n[0] 돌아가기");
                    Console.Write("선택: ");
                    Console.ReadLine();
                    break;
                }

                Console.WriteLine("=== 내 덱 (상위 5장) ===");
                Console.WriteLine($"총 전투력: {game.CurrentPlayer.DeckPower}");
                Console.WriteLine();
                
                for (int i = 0; i < deck.Count; i++)
                {
                    var card = deck[i];
                    if (card.MasterData == null) continue;

                    Console.WriteLine($"{i + 1}. [{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level} - 전투력 {card.GetPower()}");
                    Console.WriteLine($"   능력: {card.MasterData.GetAbilityDescription()}");
                }
                
                Console.WriteLine();
                game.ShowActiveAbilities();
                Console.WriteLine("※ 같은 능력은 최대 3장까지만 적용됩니다.");
                
                Console.WriteLine();
                Console.WriteLine("[1-5] 해당 슬롯 카드 교체");
                Console.WriteLine("[9] 자동 편성으로 리셋");
                Console.WriteLine("[0] 돌아가기");
                Console.Write("선택: ");
                
                string input = Console.ReadLine();
                Console.WriteLine();

                if (input == "0") break;

                if (input == "9")
                {
                    Console.Write("정말 자동 편성으로 리셋하시겠습니까? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        game.ResetDeckToAuto();
                        await game.RecalculateDeckPowerAsync();
                        Console.WriteLine("자동 편성으로 전환되었습니다!");
                        Console.WriteLine("\n계속하려면 Enter...");
                        Console.ReadLine();
                    }
                    continue;
                }

                // 슬롯 선택 (1-5)
                if (!int.TryParse(input, out int slot) || slot < 1 || slot > 5)
                {
                    continue;
                }

                // 덱 외 카드 목록 표시
                var deckIds = deck.Select(c => c.InstanceId).ToHashSet();
                var availableCards = game.PlayerCards
                    .Where(c => !deckIds.Contains(c.InstanceId))
                    .OrderByDescending(c => c.GetPower())
                    .ToList();

                if (availableCards.Count == 0)
                {
                    Console.WriteLine("교체 가능한 카드가 없습니다.");
                    Console.WriteLine("\n계속하려면 Enter...");
                    Console.ReadLine();
                    continue;
                }

                Console.WriteLine("=== 교체 가능한 카드 ===");
                for (int i = 0; i < availableCards.Count; i++)
                {
                    var card = availableCards[i];
                    if (card.MasterData == null) continue;
                    Console.WriteLine($"{i + 1}. [{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level} - 전투력 {card.GetPower()}");
                    Console.WriteLine($"   능력: {card.MasterData.GetAbilityDescription()}");
                }

                Console.Write("\n교체할 카드 번호 (취소: 0): ");
                if (!int.TryParse(Console.ReadLine(), out int cardIdx) || cardIdx < 1 || cardIdx > availableCards.Count)
                {
                    continue;
                }

                var selectedCard = availableCards[cardIdx - 1];
                if (game.SwapDeckCard(slot - 1, selectedCard.InstanceId))
                {
                    await game.RecalculateDeckPowerAsync();
                    Console.WriteLine("교체 완료!");
                    Console.WriteLine("\n계속하려면 Enter...");
                    Console.ReadLine();
                }
            }
        }

        static async Task HandleDisenchant(GameService game)
        {
            var cards = game.GetCardsSortedByPower();
            var deck = game.GetDeck();
            var deckIds = deck.Select(c => c.InstanceId).ToHashSet();
            
            if (cards.Count == 0)
            {
                Console.WriteLine("보유한 카드가 없습니다.");
                Console.WriteLine("\n계속하려면 Enter...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("=== 보유 카드 목록 (전투력 순) ===");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("※ [덱 편성 중] 카드는 분해할 수 없습니다.");
            Console.ResetColor();
            
            Console.WriteLine();
            
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card.MasterData == null) continue;

                // 덱 카드는 색상 강조
                if (deckIds.Contains(card.InstanceId))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"{i + 1}. [덱 편성 중] ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write($"{i + 1}. ");
                }
                
                Console.WriteLine($"[{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level}");
                Console.WriteLine($"   HP:{card.MasterData.HP} ATK:{card.MasterData.ATK} DEF:{card.MasterData.DEF} | 전투력: {card.GetPower()}");
                Console.WriteLine($"   능력: {card.MasterData.GetAbilityDescription()}");
            }

            Console.WriteLine();
            Console.Write("분해할 카드 번호 (취소: 0): ");
            
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= cards.Count)
            {
                var card = cards[index - 1];
                
                if (deckIds.Contains(card.InstanceId))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("========================================");
                    Console.WriteLine("  ⚠️  덱에 편성된 카드는 분해할 수 없습니다!  ⚠️");
                    Console.WriteLine("========================================");
                    Console.ResetColor();
                    return;
                }
                
                Console.Write($"정말 분해하시겠습니까? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    await game.DisenchantCardAsync(card.InstanceId);
                }
            }
        }

        static async Task HandleUpgrade(GameService game)
        {
            await ShowMyCards(game);
            
            if (game.PlayerCards.Count == 0)
                return;

            Console.WriteLine();
            Console.Write("강화할 카드 번호 (취소: 0): ");
            
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= game.PlayerCards.Count)
            {
                var cards = game.GetCardsSortedByPower();
                var card = cards[index - 1];
                
                int cost = AppConfig.GetUpgradeCost(card.Level);
                Console.WriteLine($"강화 비용: {cost} 가루 (보유: {game.CurrentPlayer.Dust})");
                Console.Write($"강화하시겠습니까? (y/n): ");
                
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    await game.UpgradeCardAsync(card.InstanceId);
                }
            }
        }

        static async Task ShowLeaderboard(GameService game)
        {
            Console.WriteLine("=== 전투력 랭킹 TOP 10 ===");
            Console.WriteLine();
            
            var leaderboard = await game.GetLeaderboardAsync();
            
            if (leaderboard.Count == 0)
            {
                Console.WriteLine("아직 랭킹이 없습니다.");
                return;
            }

            foreach (var (playerName, score, rank) in leaderboard)
            {
                string marker = playerName.Contains(game.CurrentPlayer.UserId) ? " ★ YOU" : "";
                Console.WriteLine($"{rank}. {playerName} - {score}{marker}");
            }
        }
    }
}