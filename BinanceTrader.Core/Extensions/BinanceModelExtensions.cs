using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects;

namespace BinanceTrader.Core.Extensions
{
    public static class BinanceModelExtensions
    {
        public static BinanceStreamTrade ToBinanceStreamTrade(BinanceRecentTrade recentTrade)
        {
            return new BinanceStreamTrade()
            {
                TradeId = recentTrade.Id,
                TradeTime = recentTrade.Time,
                Price = recentTrade.Price,
                Quantity = recentTrade.Quantity,
            };
        }

        public static BinanceRecentTrade ToBinanceRecentTrade(BinanceStreamTrade streamTrade)
        {
            return new BinanceRecentTrade()
            {
                Id = streamTrade.TradeId,
                Time = streamTrade.TradeTime,
                Price = streamTrade.Price,
                Quantity = streamTrade.Quantity,
            };
        }
    }
}
