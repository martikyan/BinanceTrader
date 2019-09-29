using System;

namespace BinanceTrader.Core.Models
{
    public class Trade
    {
        public SymbolPair SymbolPair { get; set; }

        public long TradeId { get; set; }

        public DateTime TradeTime { get; set; }

        public decimal Price { get; set; }

        public decimal Quantity { get; set; }
    }
}