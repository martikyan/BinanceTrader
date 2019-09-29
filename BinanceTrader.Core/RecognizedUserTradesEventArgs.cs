using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceTrader.Core
{
    public class RecognizedUserTradesEventArgs : EventArgs
    {
        public string UserId { get; set; }
        public long TradeId { get; set; }

        public static RecognizedUserTradesEventArgs Create(string userId, long tradeId)
        {
            return new RecognizedUserTradesEventArgs()
            {
                UserId = userId,
                TradeId = tradeId,
            };
        }
    }
}
