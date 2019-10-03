using System.Collections.Generic;
using System.Linq;

namespace BinanceTrader.Core.Models
{
    public class BinanceUser
    {
        public string Identifier { get; set; }

        public List<Wallet> WalletsHistory { get; set; }

        public Wallet CurrentWallet => WalletsHistory.LastOrDefault();
    }
}