namespace BinanceTrader.Core
{
    public class Limiters
    {
        public int MemoryInSeconds { get; set; }
        public int AttempsPassCount { get; set; }
        public int AttempMemoryInSeconds { get; set; }
        public decimal MinimalTradeQuantity { get; set; }
        public int MinimalTraderTradesCount { get; set; }
        public double MaximalTradeFeePercentage { get; set; }
        public double MinimalSuccessFailureRatio { get; set; }
        public int MaximalTraderTradesPerHour { get; set; }
        public double MaximalAllowedTradeSyncSeconds { get; set; }
        public double MaximalSecondsToWaitForTheTrader { get; set; }
        public int MinimalTraderActivityThresholdSeconds { get; set; }
        public double MinimalTraderProfitPerHourPercentage { get; set; }
    }
}