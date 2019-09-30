using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core
{
    public class FakeAutoTrader : AutoTrader
    {
        public FakeAutoTrader(CoreConfiguration config, ILogger logger)
            : base(config, logger)
        {
            _walletBalance = SymbolAmountPair.Create(config.TargetCurrencySymbol, 11m);
        }

        public override EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => HandleEvent;

        private void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
        }
    }
}