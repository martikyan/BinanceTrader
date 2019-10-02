namespace BinanceTrader.Core
{
    public class CoreConfiguration
    {
        public string BinanceApiKey { get; set; }

        public string BinanceApiSecret { get; set; }

        public string FirstSymbol { get; set; }

        public string SecondSymbol { get; set; }

        public string TargetCurrencySymbol { get; set; }

        public bool EnableAutoTrade { get; set; }

        public Limiters Limiters { get; set; }
    }
}