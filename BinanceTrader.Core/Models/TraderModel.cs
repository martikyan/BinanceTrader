using System.Collections.Generic;
using Binance.Net.Objects;
using BinanceTrader.Core.Utils;

namespace BinanceTrader.Core.Models
{
    public class TraderModel
    {
        public string Identifier { get; set; }

        public string Symbol { get; set; }

        public List<long> TradeIds { get; set; }

        public decimal Balance { get; set; }

        public double? PaidFee { get; set; }

        public bool IsBuyer { get; set; }

        public static (TraderModel buyer, TraderModel seller) CreatePairFromTrade(BinanceStreamTrade trade)
        {
            var tradeIds = new List<long>() { trade.TradeId };

            var buyer = new TraderModel()
            {
                IsBuyer = true,
                Balance = trade.Quantity * trade.Price,
                Identifier = IdentificationUtilities.GetRandomIdentifier(),
                TradeIds = new List<long>(tradeIds),
            };

            var seller = new TraderModel()
            {
                IsBuyer = false,
                Balance = trade.Quantity,
                Identifier = IdentificationUtilities.GetRandomIdentifier(),
                TradeIds = new List<long>(tradeIds),
            };

            return (buyer, seller);
        }
    }
}