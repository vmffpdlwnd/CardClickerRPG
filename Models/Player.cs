namespace CardClickerRPG.Models
{
    public class Player
    {
        public string UserId { get; set; }           // PlayFab ID
        public int ClickCount { get; set; }          // 현재 클릭 횟수 (0-99)
        public int Dust { get; set; }                // 보유 가루
        public int TotalClicks { get; set; }         // 총 클릭 수
        public int DeckPower { get; set; }           // 덱 총 전투력
        public string LastSaveTime { get; set; }     // 마지막 저장 시간

        public Player()
        {
            ClickCount = 0;
            Dust = 0;
            TotalClicks = 0;
            DeckPower = 0;
            LastSaveTime = DateTime.UtcNow.ToString("o");
        }
    }
}