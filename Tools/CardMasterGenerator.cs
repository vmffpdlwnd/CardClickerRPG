using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CardClickerRPG.Config;

namespace CardClickerRPG.Tools
{
    public class CardMasterGenerator
    {
        // 카드 이름 조합 요소
        private static readonly string[] Prefixes = 
        {
            "불타는", "얼어붙은", "어둠의", "빛나는", "고대의", "전설의",
            "신성한", "사악한", "광폭한", "고요한", "폭풍의", "대지의",
            "천상의", "지옥의", "영원한", "순수한", "타락한", "축복받은",
            "저주받은", "신비한", "강철의", "황금의", "은빛의", "검은",
            "붉은", "푸른", "녹색의", "보라빛", "무지개", "투명한"
        };

        private static readonly string[] Monsters = 
        {
            "드래곤", "슬라임", "고블린", "위저드", "나이트", "로그",
            "팔라딘", "워리어", "아처", "메이지", "프리스트", "서머너",
            "늑대", "곰", "호랑이", "독수리", "뱀", "전갈",
            "스켈레톤", "좀비", "뱀파이어", "리치", "데몬", "엔젤",
            "고렘", "엘프", "드워프", "오크", "트롤", "오우거",
            "페닉스", "유니콘", "그리핀", "하이드라", "와이번", "키메라",
            "미노타우로스", "세이렌", "스핑크스", "켄타우로스"
        };

        private static readonly string[] Suffixes = 
        {
            "전사", "마법사", "암살자", "수호자", "사냥꾼", "광전사",
            "현자", "예언자", "파괴자", "창조자", "심판자", "집행자",
            "수련자", "달인", "장인", "명인", "영웅", "전설",
            "군주", "제왕", "왕", "여왕", "기사", "용사",
            "마왕", "천사", "악마", "신", "반신", "불사자"
        };

        private static readonly Random random = new Random();

        public static async Task GenerateAndUploadAsync(int count = 10000)
        {
            Console.WriteLine($"CardMaster 데이터 {count}개 생성 시작...");
            Console.WriteLine();

            var region = RegionEndpoint.GetBySystemName(AppConfig.AWSRegion);
            var client = new AmazonDynamoDBClient(region);

            int batchSize = 25; // DynamoDB BatchWrite 최대 25개
            int totalBatches = (int)Math.Ceiling(count / (double)batchSize);

            for (int batch = 0; batch < totalBatches; batch++)
            {
                var writeRequests = new List<WriteRequest>();
                int startIdx = batch * batchSize;
                int endIdx = Math.Min(startIdx + batchSize, count);

                for (int i = startIdx; i < endIdx; i++)
                {
                    string cardId = $"card_{(i + 1):D4}";
                    
                    // 랜덤 이름 조합
                    string name = $"{GetRandom(Prefixes)} {GetRandom(Monsters)} {GetRandom(Suffixes)}";
                    
                    // 랜덤 등급 결정 (확률적)
                    string rarity = GetRandomRarity();
                    
                    // 등급에 따른 능력치 생성
                    var (hp, atk, def) = GenerateStats(rarity);

                    var item = new Dictionary<string, AttributeValue>
                    {
                        ["cardId"] = new AttributeValue { S = cardId },
                        ["name"] = new AttributeValue { S = name },
                        ["rarity"] = new AttributeValue { S = rarity },
                        ["hp"] = new AttributeValue { N = hp.ToString() },
                        ["atk"] = new AttributeValue { N = atk.ToString() },
                        ["def"] = new AttributeValue { N = def.ToString() }
                    };

                    writeRequests.Add(new WriteRequest
                    {
                        PutRequest = new PutRequest { Item = item }
                    });
                }

                // BatchWrite 실행
                var batchRequest = new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        [AppConfig.CardMasterTableName] = writeRequests
                    }
                };

                try
                {
                    await client.BatchWriteItemAsync(batchRequest);
                    
                    int progress = (int)((batch + 1) / (double)totalBatches * 100);
                    Console.Write($"\r진행률: {progress}% ({endIdx}/{count}개)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nBatch {batch} 업로드 실패: {ex.Message}");
                }

                // API 제한 방지 (초당 요청 제한)
                await Task.Delay(100);
            }

            Console.WriteLine();
            Console.WriteLine($"\n완료! 총 {count}개의 카드가 생성되었습니다.");
        }

        private static string GetRandom(string[] array)
        {
            return array[random.Next(array.Length)];
        }

        private static string GetRandomRarity()
        {
            int roll = random.Next(100);
            
            if (roll < 50) return "common";      // 50%
            if (roll < 80) return "rare";        // 30%
            if (roll < 95) return "epic";        // 15%
            return "legendary";                   // 5%
        }

        private static (int hp, int atk, int def) GenerateStats(string rarity)
        {
            return rarity switch
            {
                "common" => (
                    random.Next(100, 201),  // HP: 100-200
                    random.Next(10, 21),    // ATK: 10-20
                    random.Next(5, 11)      // DEF: 5-10
                ),
                "rare" => (
                    random.Next(200, 351),  // HP: 200-350
                    random.Next(20, 36),    // ATK: 20-35
                    random.Next(10, 21)     // DEF: 10-20
                ),
                "epic" => (
                    random.Next(350, 451),  // HP: 350-450
                    random.Next(35, 46),    // ATK: 35-45
                    random.Next(20, 26)     // DEF: 20-25
                ),
                "legendary" => (
                    random.Next(450, 501),  // HP: 450-500
                    random.Next(45, 51),    // ATK: 45-50
                    random.Next(25, 31)     // DEF: 25-30
                ),
                _ => (100, 10, 5)
            };
        }
    }
}