using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BinanceTrader.Core.Models;
using BinanceTrader.Core.Utils;

namespace BinanceTrader.Core.DataAccess
{
    public class SmartStorage
    {
        private readonly CoreConfiguration _config;
        private readonly IRepository _repository;

        public SmartStorage(CoreConfiguration config, IRepository repository)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public void RegisterTrade(Trade trade)
        {
            if (string.IsNullOrEmpty(trade.SymbolPair))
            {
                trade.SymbolPair = SymbolUtilities.ConstructSymbolPair(_config.FirstSymbol, _config.SecondSymbol);
            }

            _repository.AddOrUpdateTrade(trade);

            // TODO: do the same for seller
            var buyerMaxBalance = trade.Price * trade.Quantity;
            var buyerMinBalance = SubstractPercentage(buyerMaxBalance, _config.MaxTradeFeePercentage);

            var usersWithBalance = _repository.GetUsersWithBalanceInRange(buyerMinBalance, buyerMaxBalance, _config.FirstSymbol);
            if (usersWithBalance.Count == 0)
            {
                // No buyers found, lets add them ourselves.
                var user = new BinanceUser()
                {
                    Identifier = IdentificationUtilities.GetRandomIdentifier(),
                    Wallets = new List<Wallet>(capacity: 1),
                };

                user.Wallets[0] = new Wallet()
                {
                    Symbol = _config.FirstSymbol,
                    OwnerId = user.Identifier,
                    Balance = buyerMaxBalance,
                };

                _repository.AddOrUpdateUser(user);
            }
            else
            {
                
            }
        }

        //private RegisterAsRecognizedBuyer(BinanceUser recognizedBuyer, Trade trade)
        //{ 
        //    var newWallet = new Wallet()
        //    {
        //        OwnerId = recognizedBuyer.Identifier,
        //        Symbol = _config.SecondSymbol,
        //        Balance = trade.Quantity,
        //    };
        //    recognizedBuyer.Wallets[0] = newWallet; // overriding their wallet.
        //    _repository.AddOrUpdateUser(user);
        //}

        private decimal SubstractPercentage(decimal fromValue, double percentage)
        {
            return fromValue * (100m - (decimal)percentage) / 100m;
        }
    }
}
