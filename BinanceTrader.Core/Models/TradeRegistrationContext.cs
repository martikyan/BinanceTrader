using System.Collections.Generic;

namespace BinanceTrader.Core.Models
{
    public class TradeRegistrationContext
    {
        public Trade Trade { get; set; }

        public bool IsTradeRegistered { get; set; }

        public bool IsComplexTrade { get; set; }

        public bool IsBuyerRegistered { get; set; }

        public bool IsSellerRegistered { get; set; }

        public SymbolAmountPair SellingPair { get; set; }

        public SymbolAmountPair BuyingPair { get; set; }

        public List<BinanceUser> BuyerAssociatedUsers { get; set; }

        public List<BinanceUser> SellerAssociatedUsers { get; set; }
    }
}