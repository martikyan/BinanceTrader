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

        public event EventHandler<RecognizedUserTradesEventArgs> RecognizedUserTraded;

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

            var quantity = trade.Quantity;
            var qp = trade.Quantity * trade.Price;

            var buyerMinBalance = SubstractPercentage(quantity, _config.MaxTradeFeePercentage);
            var sellerMinBalance = SubstractPercentage(qp, _config.MaxTradeFeePercentage);

            RegisterUser(buyerMinBalance, quantity, qp, _config.FirstSymbol, _config.SecondSymbol, trade);
            RegisterUser(sellerMinBalance, qp, quantity, _config.SecondSymbol, _config.FirstSymbol, trade);
        }

        private void RegisterUser(decimal minBalance, decimal maxBalance, decimal balanceIfRec, string symbol, string symbolIfRec, Trade trade)
        {
            var users = _repository.GetUsersWithBalanceInRange(minBalance, maxBalance, _config.FirstSymbol);
            if (users.Count == 0)
            {
                RegisterAsNewUser(symbol, maxBalance);
            }
            else
            {
                RecognizedUserTraded?.Invoke(this, RecognizedUserTradesEventArgs.Create(users[0].Identifier, trade.TradeId));
                RegisterAsRecognizedUser(users[0], symbolIfRec, balanceIfRec);
            }
        }

        private void RegisterAsNewUser(string symbol, decimal balance)
        {
            var user = new BinanceUser()
            {
                Identifier = IdentificationUtilities.GetRandomIdentifier(),
                Wallets = new List<Wallet>(capacity: 1),
            };

            user.Wallets.Add(new Wallet()
            {
                Symbol = _config.FirstSymbol,
                OwnerId = user.Identifier,
                Balance = balance,
            });

            _repository.AddOrUpdateUser(user);
        }

        private void RegisterAsRecognizedUser(BinanceUser recognizedBuyer, string newSymbol, decimal newBalance)
        {
            var newWallet = new Wallet()
            {
                OwnerId = recognizedBuyer.Identifier,
                Symbol = newSymbol,
                Balance = newBalance,
            };
            recognizedBuyer.Wallets[0] = newWallet; // overriding their wallet.
            _repository.AddOrUpdateUser(recognizedBuyer);
        }

        private decimal SubstractPercentage(decimal fromValue, double percentage)
        {
            return fromValue * (100m - (decimal)percentage) / 100m;
        }
    }
}
