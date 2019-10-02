using System;
using System.Collections.Generic;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.AutoTrader
{
    public class BinanceAutoTrader : IAutoTrader
    {
        public List<SymbolAmountPair> WalletHistory => throw new NotImplementedException();

        public BinanceUser AttachedUser => throw new NotImplementedException();

        public UserProfitReport AttachedUserProfit => throw new NotImplementedException();

        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => throw new NotImplementedException();

        public List<string> AttachedUsersHistory => throw new NotImplementedException();

        public SymbolAmountPair CurrentWallet => throw new NotImplementedException();

        public void DetachAttachedUser()
        {
            throw new NotImplementedException();
        }
    }
}