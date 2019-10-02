using System;

namespace BinanceTrader.Core.Models
{
    public class UserProfitReport
    {
        public string CurrencySymbol { get; set; }

        public double ProfitPerHour { get; set; }

        public int WalletsCount { get; set; }

        public int SucceededTradesCount { get; set; }

        public int FailedTradesCount { get; set; }

        public double SuccessFailureRatio { get; set; }

        public TimeSpan AverageTradeThreshold { get; set; }

        public TimeSpan MinimalTradeThreshold { get; set; }

        public decimal StartBalance { get; set; }

        public decimal EndBalance { get; set; }

        public bool IsFullReport { get; set; }
    }
}