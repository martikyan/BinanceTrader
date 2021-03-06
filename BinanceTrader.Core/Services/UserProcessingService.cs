﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using Serilog;

namespace BinanceTrader.Core.Services
{
    public class UserProcessingService
    {
        private readonly CoreConfiguration _config;
        private readonly IRepository _repository;
        private readonly ILogger _logger;

        public UserProcessingService(CoreConfiguration config, IRepository repo, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public UserProfitReport GetUserProfit(string userId)
        {
            _logger.Debug($"Getting user {userId} from repo with for profit calculation.");
            var user = _repository.GetUserById(userId);

            return GetUserProfit(user);
        }

        public UserProfitReport GetUserProfit(BinanceUser user)
        {
            _logger.Verbose($"Starting profit calculation for user with Id: {user.Identifier}");
            var trades = new List<Trade>();
            var profit = new UserProfitReport();

            Debug.Assert(user.WalletsHistory[1].WalletCreatedFromTradeId == user.WalletsHistory[0].WalletCreatedFromTradeId);
            foreach (var tradeId in user.WalletsHistory.Select(w => w.WalletCreatedFromTradeId).Skip(1)) // First two wallets are created from the same trade.
            {
                var trade = _repository.GetTradeById(tradeId);
                trades.Add(trade);
            }

            var selectedWallets = user.WalletsHistory.Where(w => w.Symbol == _config.TargetCurrencySymbol).ToList();
            profit.AverageProfitPerHour = CalculateProfitPerHour(selectedWallets);
            profit.CurrencySymbol = _config.TargetCurrencySymbol;

            var lastTrade = _repository.GetTradeById(user.CurrentWallet.WalletCreatedFromTradeId);
            if (DateTime.UtcNow - lastTrade.TradeTime > TimeSpan.FromSeconds(_config.Limiters.MaximalAllowedTradeSyncSeconds))
            {
                _logger.Information($"User with Id {user.Identifier} had last trade with Id {lastTrade.TradeId} out of sync.");
                profit.IsFullReport = false;
                return profit;
            }

            if (selectedWallets.Count < 3 || profit.AverageProfitPerHour == default)
            {
                _logger.Verbose($"User with Id {user.Identifier} had small amount of information. Aborting report calculation.");
                profit.IsFullReport = false;
                return profit;
            }

            var dateDiffList = new List<TimeSpan>(capacity: trades.Count - 1);
            for (int i = 1; i < trades.Count; i++)
            {
                dateDiffList.Add(trades[i].TradeTime - trades[i - 1].TradeTime);
            }

            profit.IsFullReport = true;
            profit.EndBalance = selectedWallets.Last().Balance;
            profit.StartBalance = selectedWallets.First().Balance;
            profit.TotalTradesCount = user.WalletsHistory.Count - 1;
            profit.SuccessFailureRatio = GetSuccessFailureRate(selectedWallets);
            profit.MinimalTradeThreshold = TimeSpan.FromSeconds(dateDiffList.Min(diff => diff.TotalSeconds));
            profit.AverageTradeThreshold = TimeSpan.FromSeconds(dateDiffList.Average(diff => diff.TotalSeconds));
            profit.AverageTradesPerHour = GetTradesPerHour(profit.TotalTradesCount, profit.AverageTradeThreshold);

            return profit;
        }

        private static double GetTradesPerHour(int totalTrades, TimeSpan tradeAverageThreshold)
        {
            if (tradeAverageThreshold.TotalMilliseconds == 0.0)
            {
                return default;
            }

            return totalTrades * TimeSpan.FromHours(1).TotalMilliseconds / tradeAverageThreshold.TotalMilliseconds;
        }

        private static double GetSuccessFailureRate(List<Wallet> wallets)
        {
            var succeeded = 0;
            var failed = 0;
            for (int i = 1; i < wallets.Count; i++)
            {
                Debug.Assert(wallets[i].Symbol == wallets[i - 1].Symbol);
                if (wallets[i].Balance > wallets[i - 1].Balance)
                {
                    succeeded++;
                }
                else
                {
                    failed++;
                }
            }

            if (failed != 0)
            {
                return (double)succeeded / failed;
            }
            else
            {
                return succeeded * Math.E;
            }
        }

        private static double CalculateProfitPerHour(List<Wallet> wallets)
        {
            var firstWallet = wallets.FirstOrDefault();
            var lastWallet = wallets.LastOrDefault();

            if (firstWallet == null || lastWallet == null || firstWallet == lastWallet)
            {
                return default;
            }

            var hoursPassed = (lastWallet.WalletCreationDate - firstWallet.WalletCreationDate).TotalHours;
            var actualPercentage = (double)(lastWallet.Balance * 100m / firstWallet.Balance - 100m);

            if (hoursPassed == 0.0)
            {
                hoursPassed = TimeSpan.FromSeconds(1).TotalHours;
            }

            return actualPercentage / hoursPassed;
        }
    }
}