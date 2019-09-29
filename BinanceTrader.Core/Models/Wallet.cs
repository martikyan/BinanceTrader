using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceTrader.Core.Models
{
    public class Wallet
    {
        public string OwnerId { get; set; }

        public string Symbol { get; set; }

        public decimal Balance { get; set; }

        public long WalletCreatedFromTradeId { get; set; }
    }
}
