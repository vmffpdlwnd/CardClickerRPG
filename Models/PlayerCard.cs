namespace CardClickerRPG.Models
{
    public class PlayerCard
    {
        public string UserId { get; set; }          // 소유자 ID
        public string InstanceId { get; set; }      // 카드 인스턴스 ID (GUID)
        public string CardId { get; set; }          // CardMaster 참조
        public int Level { get; set; }              // 강화 레벨
        public string AcquiredAt { get; set; }      // 획득 시간

        // CardMaster 정보 (조인 후 채워짐)
        public CardMaster MasterData { get; set; }

        public PlayerCard()
        {
            InstanceId = Guid.NewGuid().ToString();
            Level = 1;
            AcquiredAt = DateTime.UtcNow.ToString("o");
        }

        // 전투력 계산 (레벨 포함)
        public int GetPower()
        {
            if (MasterData == null) return 0;
            
            int basePower = MasterData.GetBasePower();
            double levelMultiplier = 1.0 + (Level * 0.1);
            return (int)(basePower * levelMultiplier);
        }
    }
}