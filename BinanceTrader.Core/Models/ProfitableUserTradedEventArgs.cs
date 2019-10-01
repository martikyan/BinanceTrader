namespace BinanceTrader.Core.Models
{
    public class ProfitableUserTradedEventArgs : UserTradedEventArgs
    {
        public UserProfitReport Report { get; set; }
    }
}