using System;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core
{
    public class UserTradedEventArgs : EventArgs
    {
        public string UserId { get; set; }

        public long TradeId { get; set; }

        public static UserTradedEventArgs Create(string userId, long tradeId)
        {
            return new UserTradedEventArgs()
            {
                UserId = userId,
                TradeId = tradeId,
            };
        }
    }
}