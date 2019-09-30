using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core
{
    public class ProfitableUserTradedEventArgs : UserTradedEventArgs
    {
        public UserProfitReport Report { get; set; }

        public ProfitableUserTradedEventArgs()
        {
        }
    }
}
