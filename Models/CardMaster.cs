namespace CardClickerRPG.Models
{
    public class CardMaster
    {
        public string CardId { get; set; }      // card_0001
        public string Name { get; set; }        // "불타는 드래곤 전사"
        public string Rarity { get; set; }      // common/rare/epic/legendary
        public int HP { get; set; }
        public int ATK { get; set; }
        public int DEF { get; set; }
        public string Ability { get; set; }     // 카드 능력

        // 기본 전투력 계산
        public int GetBasePower()
        {
            return HP + (ATK * 2) + DEF;
        }
        
        // 능력 설명
        public string GetAbilityDescription()
        {
            return Ability switch
            {
                "AUTO_CLICK" => "자동 클릭 (5초마다 +1)",
                "CLICK_MULTIPLY" => "클릭 배율 (×2)",
                "DUST_BONUS" => "분해 보너스 (+50%)",
                "UPGRADE_DISCOUNT" => "강화 할인 (-30%)",
                "LUCKY" => "행운 (등급 상승 10%)",
                _ => "없음"
            };
        }
    }
}