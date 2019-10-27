using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using BinanceTrader.Core.Services;
using Serilog;

namespace BinanceTrader.Core.AutoTrader
{
    public class FakeAutoTrader : IAutoTrader
    {
        private readonly CoreConfiguration _config;
        private readonly AttempCalculatorService _attempCalculator;
        private readonly IRepository _repo;
        private readonly ILogger _logger;
        private DateTime _lastTradeDate;

        public BinanceUser AttachedUser { get; private set; }
        public UserProfitReport AttachedUserProfit { get; private set; }
        public List<SymbolAmountPair> Wallets { get; } = new List<SymbolAmountPair>();
        public List<string> AttachedUsersHistory { get; } = new List<string>();
        public SymbolAmountPair CurrentWallet => Wallets.LastOrDefault();
        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; private set; }

        public FakeAutoTrader(CoreConfiguration config, AttempCalculatorService attempCalculator, IRepository repo, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _attempCalculator = attempCalculator ?? throw new ArgumentNullException(nameof(attempCalculator));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            UpdateCurrentWallet();
            ProfitableUserTradedHandler = HandleEvent;
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
                if (!_attempCalculator.IsSucceededAttemp())
                {
                    _logger.Information("Skipping trade due to low attemps count.");
                    return;
                }

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
                var newWallet = CalculateWalletBalanceAfterTrade(CurrentWallet, trade.Price);
                Wallets.Add(newWallet);
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
            _logger.Information("Detached current user.");
            AttachedUser = null;
        }

        public void PauseTrading()
        {
            _logger.Information("Paused trading.");
            ProfitableUserTradedHandler = null;
        }

        public void ResumeTrading()
        {
            _logger.Information("Resumed trading.");
            ProfitableUserTradedHandler = HandleEvent;
        }

        public void UpdateCurrentWallet()
        {
            var nw = SymbolAmountPair.Create("USDT", 100m);
            if (nw != CurrentWallet)
            {
                Wallets.Add(nw);
            }
        }
    }
}