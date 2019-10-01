using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core
{
    public abstract class AutoTraderBase : IAutoTrader
    {
        protected readonly CoreConfiguration _config;
        protected readonly ILogger _logger;

        protected SymbolAmountPair _walletBalance;
        protected List<string> _attachedUserIds;

        public AutoTraderBase(CoreConfiguration config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => HandleEvent;

        protected abstract void HandleEvent(object sender, ProfitableUserTradedEventArgs e);
    }
}