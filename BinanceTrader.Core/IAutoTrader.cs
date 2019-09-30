using System;

namespace BinanceTrader.Core
{
    public interface IAutoTrader
    {
        EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; }
    }
}