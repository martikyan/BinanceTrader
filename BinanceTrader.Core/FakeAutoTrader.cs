using System;
using System.Collections.Generic;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core
{
    public class FakeAutoTrader : AutoTraderBase
    {
        private readonly IRepository _repo;
        private DateTime _lastTradeDate;
        private UserProfitReport _attachedUserProfit;

        public FakeAutoTrader(CoreConfiguration config, IRepository repo, ILogger logger)
            : base(config, logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            _walletBalance = SymbolAmountPair.Create(config.TargetCurrencySymbol, 11m);
            _attachedUserIds = new List<string>(capacity: 1);
        }

        protected override void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            if (_attachedUserIds.Contains(e.UserId))
            {
                _logger.Warning($"Attached user traded. Repeating actions.");
                _lastTradeDate = DateTime.UtcNow;
                var trade = _repo.GetTradeById(e.TradeId);
                var user = _repo.GetUserById(e.UserId);
                if (user.CurrentWallet.Symbol != _walletBalance.Symbol)
                {
                    return;
                }

                _logger.Warning($"Wallet balance was {_walletBalance.Amount}{_walletBalance.Symbol}");
                _logger.Warning($"Selling {_walletBalance.Amount}{_walletBalance.Symbol} and buying {user.CurrentWallet.Symbol}");
                _walletBalance = CalculateWalletBalanceAfterTrade(_walletBalance, trade.Price);
                _logger.Warning($"After selling balance is {_walletBalance.Amount}{_walletBalance.Symbol}");
            }
            else if (_attachedUserIds.Count == 0)
            {
                _logger.Warning($"Attaching to user with Id: {e.UserId}");

                _attachedUserProfit = e.Report;
                _attachedUserIds.Add(e.UserId);
            }
            else
            {
                _logger.Warning($"Checking if last trade was ${_attachedUserProfit.AverageTradeThreshold * 5} seconds before.");
                if (DateTime.UtcNow - _lastTradeDate > _attachedUserProfit.AverageTradeThreshold * 5)
                {
                    _logger.Warning($"Last trade was in {_lastTradeDate}. Too long before.");
                    // Reseting
                    _logger.Warning("Clearing attached user list.");
                    _attachedUserIds.Clear();
                    HandleEvent(this, e);
                }
            }
        }

        private SymbolAmountPair CalculateWalletBalanceAfterTrade(SymbolAmountPair initial, decimal price)
        {
            if (initial.Symbol == _config.FirstSymbol)
            {
                return SymbolAmountPair.Create(_config.SecondSymbol, price * initial.Amount);
            }
            else
            {
                return SymbolAmountPair.Create(_config.FirstSymbol, initial.Amount / price);
            }
        }
    }
}