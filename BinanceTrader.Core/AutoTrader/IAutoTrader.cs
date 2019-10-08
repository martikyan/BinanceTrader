using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.AutoTrader
{
    public interface IAutoTrader
    {
        void PauseTrading();

        void ResumeTrading();

        List<SymbolAmountPair> WalletHistory { get; }

        List<string> AttachedUsersHistory { get; }

        SymbolAmountPair CurrentWallet { get; }

        BinanceUser AttachedUser { get; }

        UserProfitReport AttachedUserProfit { get; }

        EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; }

        void DetachAttachedUser();
    }
}