namespace BinanceTrader.Core
{
    public class CoreConfiguration
    {
        public string BinanceApiKey { get; set; }

        public string BinanceApiSecret { get; set; }

        public string FirstSymbol { get; set; }

        public string SecondSymbol { get; set; }

        public string TargetCurrencySymbol { get; set; }

        public double MaxTradeFeePercentage { get; set; }

        public int MemoryInSeconds { get; set; }

        public int MinimalTraderActivityThresholdSeconds { get; set; }

        public int MinimalTraderTrades { get; set; }

        public double MinimalTraderProfitPercentage { get; set; }
    }
}