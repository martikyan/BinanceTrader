using Binance.Net.Objects;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.Extensions
{
    public static class BinanceModelExtensions
    {
        public static Trade ToTradeModel(this BinanceRecentTrade recentTrade, SymbolPair symbolPair)
        {
            return new Trade()
            {
                SymbolPair = symbolPair,
                TradeId = recentTrade.Id,
                TradeTime = recentTrade.Time,
                Price = recentTrade.Price,
                Quantity = recentTrade.Quantity,
            };
        }

        public static Trade ToTradeModel(this BinanceStreamTrade streamTrade, SymbolPair symbolPair)
        {
            return new Trade()
            {
                SymbolPair = symbolPair,
                TradeId = streamTrade.TradeId,
                TradeTime = streamTrade.TradeTime,
                Price = streamTrade.Price,
                Quantity = streamTrade.Quantity,
            };
        }
    }
}