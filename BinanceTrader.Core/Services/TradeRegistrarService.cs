using System;
using System.Collections.Generic;
using System.Linq;
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
                AssociatedUserIds = new List<string>(),
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

            context = RegisterBuyerFromContext(context);

            if (context.AssociatedUserIds.Count == 0)
            {
                RegisterSellerFromContext(context);
            }

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
            var minBalance = SubstractPercentage(context.BuyingPair.Amount, _config.MaxTradeFeePercentage);
            var users = _repository.GetUsersWithBalanceInRange(minBalance, context.BuyingPair.Amount, context.BuyingPair.Symbol);
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
            var minBalance = SubstractPercentage(context.SellingPair.Amount, _config.MaxTradeFeePercentage);
            var users = _repository.GetUsersWithBalanceInRange(minBalance, context.SellingPair.Amount, context.SellingPair.Symbol);
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
                Identifier = IdentificationUtilities.GetRandomIdentifier(),
                TradeIds = new List<long>(capacity: 1),
                WalletsHistory = new List<Wallet>(),
            };

            user.TradeIds.Add(context.TradeId);
            user.CurrentWallet = new Wallet()
            {
                Symbol = symbol,
                OwnerId = user.Identifier,
                Balance = balance,
                WalletCreatedFromTradeId = context.TradeId,
            };

            _repository.AddOrUpdateUser(user);
            return context;
        }

        private TradeRegistrationContext ProcessRegisteredUsers(TradeRegistrationContext context, List<BinanceUser> users)
        {
            foreach (var user in users)
            {
                bool isOldBuyerSelling = string.Equals(context.BuyingPair.Symbol, user.CurrentWallet.Symbol, StringComparison.OrdinalIgnoreCase) && user.CurrentWallet.Balance != 0m;
                var pair = isOldBuyerSelling ? context.SellingPair : context.BuyingPair;

                user.WalletsHistory.Add(user.CurrentWallet);
                user.CurrentWallet = new Wallet()
                {
                    OwnerId = user.Identifier,
                    Symbol = pair.Symbol,
                    Balance = pair.Amount,
                    WalletCreatedFromTradeId = context.TradeId,
                };

                user.TradeIds.Add(context.TradeId);
                _repository.AddOrUpdateUser(user);
                RecognizedUserTraded?.Invoke(this, RecognizedUserTradesEventArgs.Create(user.Identifier, context.TradeId));
            }

            context.AssociatedUserIds.AddRange(users.Select(u => u.Identifier));
            return context;
        }

        private decimal SubstractPercentage(decimal fromValue, double percentage)
        {
            return fromValue * (100m - (decimal)percentage) / 100m;
        }
    }
}