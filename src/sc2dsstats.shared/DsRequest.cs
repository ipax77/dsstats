using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace sc2dsstats.shared
{
    public class DsRequest
    {
        public DsRequest()
        {
        }

        public string Mode { get; private set; } = "Winrate";
        public string? Interest { get; set; }
        public string? Versus { get; set; }
        public string? Timespan { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Player { get; set; }

        public string GenHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Mode);
            sb.Append(Interest);
            sb.Append(Versus);
            sb.Append(StartTime.ToString("yyyyMMdd"));
            if (EndTime != DateTime.Today)
                sb.Append(EndTime.ToString("yyyyMMdd"));
            sb.Append(Player);
            return sb.ToString();
        }

        public string GenCountHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Interest);
            sb.Append(Versus);
            sb.Append(StartTime.ToString("yyyyMMdd"));
            if (EndTime != DateTime.Today)
                sb.Append(EndTime.ToString("yyyyMMdd"));
            sb.Append(Player);
            return sb.ToString();
        }
    }
}
