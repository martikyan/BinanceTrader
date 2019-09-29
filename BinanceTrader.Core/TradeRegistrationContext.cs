using System.Collections.Generic;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core
{
    public class TradeRegistrationContext
    {
        public long TradeId { get; set; }

        public bool IsTradeRegistered { get; set; }

        public bool IsComplexTrade { get; set; }

        public bool IsBuyerRegistered { get; set; }

        public bool IsSellerRegistered { get; set; }

        public SymbolAmountPair SellingPair { get; set; }

        public SymbolAmountPair BuyingPair { get; set; }

        public List<string> AssociatedUserIds { get; set; }
    }
}