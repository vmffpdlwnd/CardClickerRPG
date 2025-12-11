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

        // 기본 전투력 계산
        public int GetBasePower()
        {
            return HP + (ATK * 2) + DEF;
        }
    }
}