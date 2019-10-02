using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.AutoTrader
{
    public interface IAutoTrader
    {
        List<SymbolAmountPair> WalletHistory { get; }

        List<string> AttachedUsersHistory { get; }

        SymbolAmountPair CurrentWallet { get; }

        BinanceUser AttachedUser { get; }

        UserProfitReport AttachedUserProfit { get; }

        EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; }
    }
}