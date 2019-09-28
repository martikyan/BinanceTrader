namespace BinanceTrader.Core
{
    public class CoreConfiguration
    {
        public string BinanceApiKey { get; set; }

        public string BinanceApiSecret { get; set; }

        public string FirstSymbol { get; set; }

        public string SecondSymbol { get; set; }

        public double MaxTradeFeePercentage { get; set; }

        public int MemoryInSeconds { get; set; }
    }
}