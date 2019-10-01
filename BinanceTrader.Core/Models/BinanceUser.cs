using System.Collections.Generic;
using System.Linq;

namespace BinanceTrader.Core.Models
{
    public class BinanceUser
    {
        public string Identifier { get; set; }

        public List<Wallet> Wallets { get; set; }

        public Wallet CurrentWallet => Wallets.LastOrDefault();
    }
}