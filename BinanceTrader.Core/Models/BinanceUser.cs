using System.Collections.Generic;
using Binance.Net.Objects;
using BinanceTrader.Core.Utils;

namespace BinanceTrader.Core.Models
{
    public class BinanceUser
    {
        public string Identifier { get; set; }

        public double? FeePercentage { get; set; }

        public Wallet CurrentWallet { get; set; }

        public List<Wallet> WalletsHistory { get; set; }

        public List<long> TradeIds { get; set; }
    }
}