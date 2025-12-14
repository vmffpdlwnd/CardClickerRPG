using System.Collections.Generic;
using Newtonsoft.Json;

namespace CardClickerRPG.Models
{
    public class GetPlayerResponse
    {
        [JsonProperty("player")]
        public Player Player { get; set; }
    }

    public class GetPlayerCardsResponse
    {
        [JsonProperty("cards")]
        public List<PlayerCard> Cards { get; set; }
    }

    public class GetCardMasterResponse
    {
        [JsonProperty("cardMaster")]
        public CardMaster CardMaster { get; set; }
    }
}
