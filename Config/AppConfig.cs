namespace CardClickerRPG.Config
{
    public static class AppConfig
    {
        // PlayFab 설정
        public static string PlayFabTitleId = "1D6F25"; // PlayFab Title ID 입력
        
        // AWS 설정
        public static string AWSRegion = "ap-northeast-2"; // 서울 리전
        
        // DynamoDB 테이블 이름
        public static string PlayersTableName = "CardClicker_Players";
        public static string PlayerCardsTableName = "CardClicker_PlayerCards";
        public static string CardMasterTableName = "CardClicker_CardMaster";
        
        // 게임 밸런스
        public static int ClicksForCard = 100;  // 카드 획득에 필요한 클릭 수
        public static int TotalCardCount = 1000;  // 총 카드 수
        
        // 카드 분해 보상
        public static int DustCommon = 10;
        public static int DustRare = 30;
        public static int DustEpic = 100;
        public static int DustLegendary = 300;
        
        // 카드 강화 비용
        public static int GetUpgradeCost(int currentLevel)
        {
            return currentLevel * 50;
        }
        
        // 등급별 가루 보상
        public static int GetDustByRarity(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => DustCommon,
                "rare" => DustRare,
                "epic" => DustEpic,
                "legendary" => DustLegendary,
                _ => 10
            };
        }
    }
}