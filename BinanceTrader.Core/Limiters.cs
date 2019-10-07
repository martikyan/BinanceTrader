namespace BinanceTrader.Core
{
    public class Limiters
    {
        public int MemoryInSeconds { get; set; }

        public decimal MinimalTradeQuantity { get; set; }

        public int MinimalTraderWalletsCount { get; set; }

        public double MaximalTradeFeePercentage { get; set; }

        public int MaximalTraderTradesCountPerHour { get; set; }

        public double MaximalAllowedTradeSyncSeconds { get; set; }

        public double MaximalSecondsToWaitForTheTrader { get; set; }

        public int MinimalTraderActivityThresholdSeconds { get; set; }

        public double MinimalTraderProfitPerHourPercentage { get; set; }
    }
}