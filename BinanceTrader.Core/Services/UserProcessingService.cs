using System;
using System.Collections.Generic;
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
            var profit = new UserProfitReport()
            {
                WalletsCount = user.WalletsHistory.Count,
            };

            var trades = new List<Trade>();
            foreach (var tradeId in user.WalletsHistory.Select(w => w.WalletCreatedFromTradeId))
            {
                var trade = _repository.GetTradeById(tradeId);
                trades.Add(trade);
            }

            var walletsForCurrency1 = user.WalletsHistory.Where(w => w.Symbol == _config.FirstSymbol);
            var walletsForCurrency2 = user.WalletsHistory.Where(w => w.Symbol == _config.SecondSymbol);
            var profit1 = CalculateProfitPerHour(walletsForCurrency1);
            var profit2 = CalculateProfitPerHour(walletsForCurrency2);

            var selectedWallets = profit1 > profit2 ? walletsForCurrency1.ToList() : walletsForCurrency2.ToList();
            profit.AverageProfitPerHour = profit1 > profit2 ? profit1 : profit2;
            profit.CurrencySymbol = profit1 > profit2 ? _config.FirstSymbol : _config.SecondSymbol;

            var lastTrade = _repository.GetTradeById(user.CurrentWallet.WalletCreatedFromTradeId);
            if (DateTime.UtcNow - lastTrade.TradeTime > TimeSpan.FromSeconds(_config.Limiters.MaximalAllowedTradeSyncSeconds))
            {
                _logger.Information($"User with Id {user.Identifier} had last trade with Id {lastTrade.TradeId} out of sync.");
                profit.IsFullReport = true;
                return profit;
            }

            if (selectedWallets.Count < 2 || profit.AverageProfitPerHour == default)
            {
                _logger.Verbose($"User with Id {user.Identifier} had small amount of information. Aborting report calculation.");
                profit.IsFullReport = true;
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
            profit.TotalTradesCount = user.WalletsHistory.Count + 1;
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

        private static double CalculateProfitPerHour(IEnumerable<Wallet> wallets)
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