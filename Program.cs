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
            var dynamoDBService = new DynamoDBService();
            var gameService = new GameService(playFabService, dynamoDBService);

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
                        ShowMyCards(gameService);
                        break;
                    case "3":
                        ShowDeck(gameService);
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
            Console.WriteLine("[3] 내 덱 보기");
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
            }
            else
            {
                Console.WriteLine($"클릭! ({game.CurrentPlayer.ClickCount}/{AppConfig.ClicksForCard})");
            }
        }

        static void ShowMyCards(GameService game)
        {
            var cards = game.GetCardsSortedByPower();
            
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

                Console.WriteLine($"{i + 1}. [{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level}");
                Console.WriteLine($"   HP:{card.MasterData.HP} ATK:{card.MasterData.ATK} DEF:{card.MasterData.DEF} | 전투력: {card.GetPower()}");
            }
        }

        static void ShowDeck(GameService game)
        {
            var deck = game.GetDeck();
            
            if (deck.Count == 0)
            {
                Console.WriteLine("덱에 카드가 없습니다.");
                return;
            }

            Console.WriteLine("=== 내 덱 (상위 5장 자동 편성) ===");
            Console.WriteLine($"총 전투력: {game.CurrentPlayer.DeckPower}");
            Console.WriteLine();
            
            for (int i = 0; i < deck.Count; i++)
            {
                var card = deck[i];
                if (card.MasterData == null) continue;

                Console.WriteLine($"{i + 1}. [{card.MasterData.Rarity}] {card.MasterData.Name} Lv.{card.Level} - 전투력 {card.GetPower()}");
            }
        }

        static async Task HandleDisenchant(GameService game)
        {
            ShowMyCards(game);
            
            if (game.PlayerCards.Count == 0)
                return;

            Console.WriteLine();
            Console.Write("분해할 카드 번호 (취소: 0): ");
            
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= game.PlayerCards.Count)
            {
                var cards = game.GetCardsSortedByPower();
                var card = cards[index - 1];
                
                Console.Write($"정말 분해하시겠습니까? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    await game.DisenchantCardAsync(card.InstanceId);
                }
            }
        }

        static async Task HandleUpgrade(GameService game)
        {
            ShowMyCards(game);
            
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