using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace sc2dsstats.shared
{
    public class DsRankingResponse
    {
        public string Playername { get; set; } = "PAX";
        public int Games { get; set; }
        public int Wins { get; set; }
        public int MVPs { get; set; }
        public string MainCommander { get; set; } = "Abathur";
        public int GamesMain { get; set; }
        public int Teamgames { get; set; }

        [NotMapped]
        [JsonIgnore]
        public double Winrate => Games == 0 ? 0 : Math.Round((double)Wins * 100 / (double)Games, 2);
        [NotMapped]
        [JsonIgnore]
        public double Mvp => Games == 0 ? 0 : Math.Round((double)MVPs * 100 / (double)Games, 2);
        [NotMapped]
        [JsonIgnore]
        public double Main => Games == 0 ? 0 : Math.Round((double)GamesMain * 100 / (double)Games, 2);
        [NotMapped]
        [JsonIgnore]
        public double Team => Games == 0 ? 0 : Math.Round((double)Teamgames * 100 / (double)Games, 2);
    }
}
