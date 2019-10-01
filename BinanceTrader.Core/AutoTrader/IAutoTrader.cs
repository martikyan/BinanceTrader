using System;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.AutoTrader
{
    public interface IAutoTrader
    {
        EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; }
    }
}