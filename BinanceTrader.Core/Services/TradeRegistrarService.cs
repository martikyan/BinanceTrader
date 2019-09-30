using System;
using System.Collections.Generic;
using System.Diagnostics;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using BinanceTrader.Core.Utils;

namespace BinanceTrader.Core.Services
{
    public class TradeRegistrarService : ITradeRegistrarService
    {
        private readonly CoreConfiguration _config;
        private readonly IRepository _repository;

        public event EventHandler<RecognizedUserTradesEventArgs> RecognizedUserTraded;

        public TradeRegistrarService(CoreConfiguration config, IRepository repository)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public TradeRegistrationContext RegisterTrade(Trade trade)
        {
            var context = new TradeRegistrationContext()
            {
                TradeId = trade.TradeId,
            };

            var quantity = trade.Quantity;
            var qp = trade.Quantity * trade.Price;

            context.BuyingPair = (_config.FirstSymbol, quantity);
            context.SellingPair = (_config.SecondSymbol, qp);

            context = SetTradeComplexity(context, trade);

            if (!context.IsComplexTrade)
            {
                return context;
            }

            _repository.AddOrUpdateTrade(trade);
            context.IsTradeRegistered = true;

            var buyerMin = SubstractPercentage(context.BuyingPair.Amount, _config.MaxTradeFeePercentage);
            var sellerMin = SubstractPercentage(context.SellingPair.Amount, _config.MaxTradeFeePercentage);

            var buyerAssociates = _repository.GetUsersWithBalanceInRange(buyerMin, context.BuyingPair.Amount, context.BuyingPair.Symbol);
            var sellerAssociates = _repository.GetUsersWithBalanceInRange(sellerMin, context.SellingPair.Amount, context.SellingPair.Symbol);

            context.BuyerAssociatedUsers = buyerAssociates;
            context.SellerAssociatedUsers = sellerAssociates;

            context = RegisterBuyerFromContext(context);
            context = RegisterSellerFromContext(context);

            return context;
        }

        private TradeRegistrationContext SetTradeComplexity(TradeRegistrationContext context, Trade trade)
        {
            context.IsComplexTrade =
                IsComplexAmount(trade.SymbolPair.Symbol1, trade.Quantity) &&
                IsComplexAmount(trade.SymbolPair.Symbol2, trade.Price * trade.Quantity);

            return context;
        }

        private bool IsComplexAmount(string symbol, decimal amount)
        {
            var l = _repository.GetCommonAmountLength(symbol);
            return ((double)amount).ToString().Length >= l;
        }

        private TradeRegistrationContext RegisterBuyerFromContext(TradeRegistrationContext context)
        {
            var users = context.BuyerAssociatedUsers;
            if (users.Count == 0)
            {
                context = RegisterNewUser(context, context.BuyingPair.Symbol, context.BuyingPair.Amount);
            }
            else
            {
                context = ProcessRegisteredUsers(context, users);
            }

            context.IsBuyerRegistered = true;
            return context;
        }

        private TradeRegistrationContext RegisterSellerFromContext(TradeRegistrationContext context)
        {
            var users = context.SellerAssociatedUsers;
            if (users.Count == 0)
            {
                RegisterNewUser(context, context.SellingPair.Symbol, context.SellingPair.Amount);
            }
            else
            {
                ProcessRegisteredUsers(context, users);
            }

            context.IsSellerRegistered = true;
            return context;
        }

        private TradeRegistrationContext RegisterNewUser(TradeRegistrationContext context, string symbol, decimal balance)
        {
            var user = new BinanceUser()
            {
                Wallets = new List<Wallet>(),
                Identifier = IdentificationUtilities.GetRandomIdentifier(),
            };

            user.Wallets.Add(new Wallet()
            {
                Symbol = symbol,
                OwnerId = user.Identifier,
                Balance = balance,
                WalletCreatedFromTradeId = context.TradeId,
            });

            _repository.AddOrUpdateUser(user);
            return context;
        }

        private TradeRegistrationContext ProcessRegisteredUsers(TradeRegistrationContext context, List<BinanceUser> users)
        {
            foreach (var user in users)
            {
                bool isOldBuyer = string.Equals(context.BuyingPair.Symbol, user.CurrentWallet.Symbol);
                var pair = isOldBuyer ? context.SellingPair : context.BuyingPair;

                Debug.Assert(!isOldBuyer ? string.Equals(context.SellingPair.Symbol, user.CurrentWallet.Symbol) : true);

                var newWallet = new Wallet()
                {
                    OwnerId = user.Identifier,
                    Symbol = pair.Symbol,
                    Balance = pair.Amount,
                    WalletCreatedFromTradeId = context.TradeId,
                };

                user.Wallets.Add(newWallet);
                _repository.AddOrUpdateUser(user);
                RecognizedUserTraded?.Invoke(this, RecognizedUserTradesEventArgs.Create(user.Identifier, context.TradeId));
            }

            return context;
        }

        private decimal SubstractPercentage(decimal fromValue, double percentage)
        {
            return fromValue * (100m - (decimal)percentage) / 100m;
        }
    }
}