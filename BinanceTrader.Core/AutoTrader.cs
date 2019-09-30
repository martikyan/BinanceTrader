using System;

namespace BinanceTrader.Core
{
    public class AutoTrader : IAutoTrader
    {
        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => throw new NotImplementedException();
    }
}