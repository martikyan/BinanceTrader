using System;
using System.Collections.Generic;
using System.Diagnostics;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using BinanceTrader.Core.Utils;
using Serilog;

namespace BinanceTrader.Core.Services
{
    public class TradeRegistrarService
    {
        private readonly CoreConfiguration _config;
        private readonly IRepository _repository;
        private readonly ILogger _logger;

        public event EventHandler<UserTradedEventArgs> UserTraded;

        public TradeRegistrarService(CoreConfiguration config, IRepository repository, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TradeRegistrationContext RegisterTrade(Trade trade)
        {
            _logger.Verbose($"Registering trade with Id {trade.TradeId}");
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
                _logger.Verbose($"Trade with Id {trade.TradeId} was not complex. q = {trade.Quantity} p = {trade.Price} sp = {trade.SymbolPair}");
                return context;
            }

            _logger.Verbose($"Trade with Id {trade.TradeId} was a complex trade. Registering.");
            _repository.AddOrUpdateTrade(trade);
            context.IsTradeRegistered = true;

            var buyerAssociates = _repository.GetUsersWithBalance(context.BuyingPair.Amount, context.BuyingPair.Symbol, _config.MaximumTradeFeePercentage);
            var sellerAssociates = _repository.GetUsersWithBalance(context.SellingPair.Amount, context.SellingPair.Symbol, _config.MaximumTradeFeePercentage);

            context.BuyerAssociatedUsers = buyerAssociates;
            context.SellerAssociatedUsers = sellerAssociates;

            if (buyerAssociates.Count > sellerAssociates.Count)
            {
                context = RegisterBuyerFromContext(context);
            }
            else if (sellerAssociates.Count > buyerAssociates.Count)
            {
                context = RegisterSellerFromContext(context);
            }
            else
            {
                context = RegisterBuyerFromContext(context);
                context = RegisterSellerFromContext(context);
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
            _logger.Debug($"Registered a new user with Id: {user.Identifier}");
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
                _logger.Debug($"User with Id {user.Identifier} got updated.");
                UserTraded?.Invoke(this, UserTradedEventArgs.Create(user.Identifier, context.TradeId));
            }

            return context;
        }
    }
}