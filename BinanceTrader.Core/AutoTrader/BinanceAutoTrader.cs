using System;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core.AutoTrader
{
    public class BinanceAutoTrader : AutoTraderBase
    {
        public BinanceAutoTrader(CoreConfiguration config, ILogger logger)
            : base(config, logger)
        {
        }

        protected override void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}