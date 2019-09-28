using System.Collections.Generic;
using Binance.Net.Objects;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.DataAccess
{
    public interface ITradeRepository
    {
        List<TraderModel> GetTradersWithBalance(decimal balance, decimal maxFees = 0.1m);

        BinanceStreamTrade GetTradeById(long tradeId);

        void SaveTrade(BinanceStreamTrade trade);

        void SaveTrades(List<BinanceStreamTrade> trades);

        void Flush();
    }
}