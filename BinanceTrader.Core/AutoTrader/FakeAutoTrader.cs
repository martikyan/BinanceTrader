using System;
using System.Collections.Generic;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core.AutoTrader
{
    public class FakeAutoTrader : IAutoTrader
    {
        private readonly CoreConfiguration _config;
        private readonly IRepository _repo;
        private readonly ILogger _logger;
        private DateTime _lastTradeDate;

        public BinanceUser AttachedUser { get; private set; }
        public UserProfitReport AttachedUserProfit { get; private set; }
        public List<SymbolAmountPair> WalletHistory { get; private set; }
        public List<string> AttachedUsersHistory { get; private set; }
        public SymbolAmountPair CurrentWallet { get; private set; }
        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => HandleEvent;

        public FakeAutoTrader(CoreConfiguration config, IRepository repo, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CurrentWallet = SymbolAmountPair.Create(config.TargetCurrencySymbol, 11m);
            WalletHistory = new List<SymbolAmountPair>() { CurrentWallet };
            AttachedUsersHistory = new List<string>();
        }

        private void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            if (e.Report.CurrencySymbol != _config.TargetCurrencySymbol)
            {
                _logger.Information("Report was not targeting our currency symbol.");
                return;
            }

            var eventOwnerUser = _repo.GetUserById(e.UserId);
            if (eventOwnerUser.CurrentWallet.Symbol == CurrentWallet.Symbol)
            {
                _logger.Information($"Skipping report due to trader and our wallets currency equality.");
            }

            if (AttachedUser == null || AttachedUser.Identifier == e.UserId)
            {
                if (AttachedUser == null)
                {
                    _logger.Information($"Attaching to user with Id: {e.UserId}");
                    AttachedUsersHistory.Add(e.UserId);
                    AttachedUserProfit = e.Report;
                    AttachedUser = eventOwnerUser;
                }

                _logger.Information("Attached user traded. Repeating actions.");
                var trade = _repo.GetTradeById(e.TradeId);
                _lastTradeDate = trade.TradeTime;
                if (AttachedUser.CurrentWallet.Symbol == CurrentWallet.Symbol)
                {
                    _logger.Information("Currently the trader holds the currency that we already have.");
                    return;
                }

                _logger.Warning($"Wallet balance was {CurrentWallet.Amount}{CurrentWallet.Symbol}");
                _logger.Warning($"Selling {CurrentWallet.Amount}{CurrentWallet.Symbol} and buying {AttachedUser.CurrentWallet.Symbol}");
                CurrentWallet = CalculateWalletBalanceAfterTrade(CurrentWallet, trade.Price);
                WalletHistory.Add(CurrentWallet);
                _logger.Warning($"After selling balance is {CurrentWallet.Amount}{CurrentWallet.Symbol}");
            }
            else
            {
                var maxTimeToWaitForAttachedUser = TimeSpan.FromTicks(AttachedUserProfit.AverageTradeThreshold.Ticks * 2);

                if (DateTime.UtcNow - _lastTradeDate > maxTimeToWaitForAttachedUser ||
                e.Report.AverageProfitPerHour > AttachedUserProfit.AverageProfitPerHour)
                {
                    DetachAttachedUser();
                    HandleEvent(this, e);
                }
            }
        }

        private SymbolAmountPair CalculateWalletBalanceAfterTrade(SymbolAmountPair initial, decimal price)
        {
            if (initial.Symbol == _config.FirstSymbol)
            {
                return SymbolAmountPair.Create(_config.SecondSymbol, price * initial.Amount * 0.999m);
            }
            else
            {
                return SymbolAmountPair.Create(_config.FirstSymbol, initial.Amount / price * 0.999m);
            }
        }

        public void DetachAttachedUser()
        {
            AttachedUser = null;
        }
    }
}