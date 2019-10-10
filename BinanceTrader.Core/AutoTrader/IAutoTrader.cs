using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.AutoTrader
{
    public interface IAutoTrader
    {
        void PauseTrading();

        void ResumeTrading();

        void DetachAttachedUser();

        void UpdateCurrentWallet();

        BinanceUser AttachedUser { get; }
        SymbolAmountPair CurrentWallet { get; }
        List<string> AttachedUsersHistory { get; }
        UserProfitReport AttachedUserProfit { get; }
        List<SymbolAmountPair> Wallets { get; }
        EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; }
    }
}