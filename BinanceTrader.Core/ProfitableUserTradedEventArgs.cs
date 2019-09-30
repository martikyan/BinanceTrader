using BinanceTrader.Core.Models;

namespace BinanceTrader.Core
{
    public class ProfitableUserTradedEventArgs : UserTradedEventArgs
    {
        public UserProfitReport Report { get; set; }

        public ProfitableUserTradedEventArgs()
        {
        }
    }
}