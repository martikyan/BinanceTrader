namespace BinanceTrader.Core
{
    public class Limiters
    {
        public int MemoryInSeconds { get; set; }

        public int MinimalTraderActivityThresholdSeconds { get; set; }

        public int MinimalTraderWalletsCount { get; set; }

        public decimal MinimalTradeQuantity { get; set; }

        public double MaximumTradeFeePercentage { get; set; }

        public double MaximumAllowedTradeSyncSeconds { get; set; }

        public double MinimalTraderProfitPerHourPercentage { get; set; }
    }
}