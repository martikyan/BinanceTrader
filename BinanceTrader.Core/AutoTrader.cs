using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core
{
    public class AutoTrader : IAutoTrader
    {
        protected readonly CoreConfiguration config;
        protected readonly ILogger logger;

        protected SymbolAmountPair _walletBalance;
        protected List<string> _attachedUserIds;

        public AutoTrader(CoreConfiguration config, ILogger logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => HandleEvent;

        private void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            throw new NotImplementedException($"{nameof(AutoTrader)}'s handler is not implemented yet.");
        }
    }
}