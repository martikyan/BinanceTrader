using System;
using Serilog;

namespace BinanceTrader.Core
{
    public class AutoTrader : AutoTraderBase
    {
        public AutoTrader(CoreConfiguration config, ILogger logger)
            : base(config, logger)
        {
        }

        protected override void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
