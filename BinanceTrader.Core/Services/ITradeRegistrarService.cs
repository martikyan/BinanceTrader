using System;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.Services
{
    public interface ITradeRegistrarService
    {
        event EventHandler<RecognizedUserTradesEventArgs> RecognizedUserTraded;

        TradeRegistrationContext RegisterTrade(Trade trade);
    }
}