using System;

namespace BinanceTrader.Core.Models
{
    public class Wallet
    {
        public string OwnerId { get; set; }

        public string Symbol { get; set; }

        public decimal Balance { get; set; }

        public long WalletCreatedFromTradeId { get; set; }

        public DateTime WalletCreationDate { get; set; }
    }
}