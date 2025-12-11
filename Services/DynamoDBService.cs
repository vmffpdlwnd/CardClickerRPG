using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using CardClickerRPG.Config;
using CardClickerRPG.Models;
using Newtonsoft.Json;

namespace CardClickerRPG.Services
{
    public class DynamoDBService
    {
        private readonly AmazonDynamoDBClient _client;

        public DynamoDBService()
        {
            // ~/.aws/credentials 파일에서 자동으로 인증 정보 로드
            var region = RegionEndpoint.GetBySystemName(AppConfig.AWSRegion);
            _client = new AmazonDynamoDBClient(region);
        }

        #region Player CRUD

        // 플레이어 생성
        public async Task<bool> CreatePlayerAsync(Player player)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = player.UserId },
                ["clickCount"] = new AttributeValue { N = player.ClickCount.ToString() },
                ["dust"] = new AttributeValue { N = player.Dust.ToString() },
                ["totalClicks"] = new AttributeValue { N = player.TotalClicks.ToString() },
                ["deckPower"] = new AttributeValue { N = player.DeckPower.ToString() },
                ["lastSaveTime"] = new AttributeValue { S = player.LastSaveTime }
            };

            var request = new PutItemRequest
            {
                TableName = AppConfig.PlayersTableName,
                Item = item
            };

            try
            {
                await _client.PutItemAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"플레이어 생성 실패: {ex.Message}");
                return false;
            }
        }

        // 플레이어 조회
        public async Task<Player> GetPlayerAsync(string userId)
        {
            var request = new GetItemRequest
            {
                TableName = AppConfig.PlayersTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = userId }
                }
            };

            try
            {
                var response = await _client.GetItemAsync(request);
                
                if (!response.IsItemSet)
                    return null;

                var item = response.Item;
                return new Player
                {
                    UserId = item["userId"].S,
                    ClickCount = int.Parse(item["clickCount"].N),
                    Dust = int.Parse(item["dust"].N),
                    TotalClicks = int.Parse(item["totalClicks"].N),
                    DeckPower = int.Parse(item["deckPower"].N),
                    LastSaveTime = item["lastSaveTime"].S
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"플레이어 조회 실패: {ex.Message}");
                return null;
            }
        }

        // 플레이어 업데이트
        public async Task<bool> UpdatePlayerAsync(Player player)
        {
            player.LastSaveTime = DateTime.UtcNow.ToString("o");

            var request = new UpdateItemRequest
            {
                TableName = AppConfig.PlayersTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = player.UserId }
                },
                UpdateExpression = "SET clickCount = :cc, dust = :d, totalClicks = :tc, deckPower = :dp, lastSaveTime = :lst",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":cc"] = new AttributeValue { N = player.ClickCount.ToString() },
                    [":d"] = new AttributeValue { N = player.Dust.ToString() },
                    [":tc"] = new AttributeValue { N = player.TotalClicks.ToString() },
                    [":dp"] = new AttributeValue { N = player.DeckPower.ToString() },
                    [":lst"] = new AttributeValue { S = player.LastSaveTime }
                }
            };

            try
            {
                await _client.UpdateItemAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"플레이어 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region PlayerCard CRUD

        // 카드 추가
        public async Task<bool> AddPlayerCardAsync(PlayerCard card)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["userId"] = new AttributeValue { S = card.UserId },
                ["instanceId"] = new AttributeValue { S = card.InstanceId },
                ["cardId"] = new AttributeValue { S = card.CardId },
                ["level"] = new AttributeValue { N = card.Level.ToString() },
                ["acquiredAt"] = new AttributeValue { S = card.AcquiredAt }
            };

            var request = new PutItemRequest
            {
                TableName = AppConfig.PlayerCardsTableName,
                Item = item
            };

            try
            {
                await _client.PutItemAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"카드 추가 실패: {ex.Message}");
                return false;
            }
        }

        // 플레이어의 모든 카드 조회
        public async Task<List<PlayerCard>> GetPlayerCardsAsync(string userId)
        {
            var request = new QueryRequest
            {
                TableName = AppConfig.PlayerCardsTableName,
                KeyConditionExpression = "userId = :uid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":uid"] = new AttributeValue { S = userId }
                }
            };

            try
            {
                var response = await _client.QueryAsync(request);
                var cards = new List<PlayerCard>();

                foreach (var item in response.Items)
                {
                    cards.Add(new PlayerCard
                    {
                        UserId = item["userId"].S,
                        InstanceId = item["instanceId"].S,
                        CardId = item["cardId"].S,
                        Level = int.Parse(item["level"].N),
                        AcquiredAt = item["acquiredAt"].S
                    });
                }

                return cards;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"카드 조회 실패: {ex.Message}");
                return new List<PlayerCard>();
            }
        }

        // 카드 레벨업
        public async Task<bool> UpgradeCardAsync(string userId, string instanceId, int newLevel)
        {
            var request = new UpdateItemRequest
            {
                TableName = AppConfig.PlayerCardsTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = userId },
                    ["instanceId"] = new AttributeValue { S = instanceId }
                },
                UpdateExpression = "SET #lvl = :newLevel",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#lvl"] = "level"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":newLevel"] = new AttributeValue { N = newLevel.ToString() }
                }
            };

            try
            {
                await _client.UpdateItemAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"카드 강화 실패: {ex.Message}");
                return false;
            }
        }

        // 카드 삭제 (분해)
        public async Task<bool> DeleteCardAsync(string userId, string instanceId)
        {
            var request = new DeleteItemRequest
            {
                TableName = AppConfig.PlayerCardsTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["userId"] = new AttributeValue { S = userId },
                    ["instanceId"] = new AttributeValue { S = instanceId }
                }
            };

            try
            {
                await _client.DeleteItemAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"카드 삭제 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region CardMaster

        // 카드 마스터 조회
        public async Task<CardMaster> GetCardMasterAsync(string cardId)
        {
            var request = new GetItemRequest
            {
                TableName = AppConfig.CardMasterTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["cardId"] = new AttributeValue { S = cardId }
                }
            };

            try
            {
                var response = await _client.GetItemAsync(request);
                
                if (!response.IsItemSet)
                    return null;

                var item = response.Item;
                return new CardMaster
                {
                    CardId = item["cardId"].S,
                    Name = item["name"].S,
                    Rarity = item["rarity"].S,
                    HP = int.Parse(item["hp"].N),
                    ATK = int.Parse(item["atk"].N),
                    DEF = int.Parse(item["def"].N)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"카드 마스터 조회 실패: {ex.Message}");
                return null;
            }
        }

        // 랜덤 카드 ID 가져오기 (간단 버전 - 나중에 개선)
        public async Task<string> GetRandomCardIdAsync()
        {
            // TODO: 실제로는 Scan으로 전체 조회 후 랜덤 선택
            // 지금은 임시로 card_0001 ~ card_10000 범위에서 랜덤
            var random = new Random();
            var randomId = random.Next(1, 10001);
            return $"card_{randomId:D4}";
        }

        #endregion
    }
}