using System;

namespace BinanceTrader.Core.Models
{
    public class Wallet
    {
        public string Symbol { get; set; }
        public decimal Balance { get; set; }
        public string OwnerId { get; set; }
        public long WalletCreatedFromTradeId { get; set; }
        public DateTime WalletCreationDate { get; } = DateTime.UtcNow;

        public Wallet Clone()
        {
            return new Wallet()
            {
                Symbol = Symbol,
                Balance = Balance,
                OwnerId = OwnerId,
                WalletCreatedFromTradeId = WalletCreatedFromTradeId,
            };
        }
    }
}