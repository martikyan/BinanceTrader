using System;

namespace BinanceTrader.Core.Models
{
    public class UserProfitReport    {        public string UserId { get; set; }        public bool IsFullReport { get; set; }        public int TotalTradesCount { get; set; }        public decimal StartBalance { get; set; }        public decimal EndBalance { get; set; }        public double SuccessFailureRatio { get; set; }        public double AverageProfitPerHour { get; set; }        public double AverageTradesPerHour { get; set; }
        public TimeSpan AverageTradeThreshold { get; set; }
        public TimeSpan MinimalTradeThreshold { get; set; }
    }
}