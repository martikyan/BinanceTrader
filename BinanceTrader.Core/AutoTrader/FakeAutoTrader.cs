using System;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core.AutoTrader
{
    public class FakeAutoTrader : AutoTraderBase
    {
        private readonly IRepository _repo;
        private DateTime _lastTradeDate;
        private BinanceUser _attachedUser;
        private UserProfitReport _attachedUserProfit;

        public FakeAutoTrader(CoreConfiguration config, IRepository repo, ILogger logger)
            : base(config, logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _walletBalance = SymbolAmountPair.Create(config.TargetCurrencySymbol, 11m);
        }

        protected override void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            if (e.Report.CurrencySymbol != _config.TargetCurrencySymbol)
            {
                _logger.Information("Report was not targeting our currency symbol.");
                return;
            }

            if (_attachedUser == null || _attachedUser.Identifier == e.UserId)
            {
                if (_attachedUser == null)
                {
                    _logger.Information($"Attaching to user with Id: {e.UserId}");
                    _attachedUserProfit = e.Report;
                    _attachedUser = _repo.GetUserById(e.UserId);
                }

                _logger.Information("Attached user traded. Repeating actions.");
                var trade = _repo.GetTradeById(e.TradeId);
                _lastTradeDate = trade.TradeTime;
                if (_attachedUser.CurrentWallet.Symbol == _walletBalance.Symbol)
                {
                    _logger.Information("Currently the trader holds the currency that we already have.");
                    return;
                }

                _logger.Warning($"Wallet balance was {_walletBalance.Amount}{_walletBalance.Symbol}");
                _logger.Warning($"Selling {_walletBalance.Amount}{_walletBalance.Symbol} and buying {_attachedUser.CurrentWallet.Symbol}");
                _walletBalance = CalculateWalletBalanceAfterTrade(_walletBalance, trade.Price);
                _logger.Warning($"After selling balance is {_walletBalance.Amount}{_walletBalance.Symbol}");
            }
            else
            {
                var maxTimeToWaitForAttachedUser = _attachedUserProfit.AverageTradeThreshold * 2;
                if (DateTime.UtcNow - _lastTradeDate > maxTimeToWaitForAttachedUser ||
                    e.Report.ProfitPerHour > _attachedUserProfit.ProfitPerHour)
                {
                    _logger.Information("Detaching current user.");
                    _attachedUser = null;
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