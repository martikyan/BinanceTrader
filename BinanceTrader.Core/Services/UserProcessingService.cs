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
                WalletsCount = user.Wallets.Count,
            };

            var trades = new List<Trade>();
            foreach (var tradeId in user.Wallets.Select(w => w.WalletCreatedFromTradeId))
            {
                var trade = _repository.GetTradeById(tradeId);
                trades.Add(trade);
            }

            var walletsForCurrency1 = user.Wallets.Where(w => w.Symbol == _config.FirstSymbol);
            var walletsForCurrency2 = user.Wallets.Where(w => w.Symbol == _config.SecondSymbol);
            var profit1 = CalculateProfitPerHour(walletsForCurrency1);
            var profit2 = CalculateProfitPerHour(walletsForCurrency2);

            var selectedWallets = profit1 > profit2 ? walletsForCurrency1.ToList() : walletsForCurrency2.ToList();
            profit.ProfitPerHour = profit1 > profit2 ? profit1 : profit2;
            profit.CurrencySymbol = profit1 > profit2 ? _config.FirstSymbol : _config.SecondSymbol;

            if (selectedWallets.Count < 2 || profit.ProfitPerHour == default)
            {
                _logger.Verbose($"User with Id {user.Identifier} had small amount of information. Aborting report calculation.");
                profit.IsFullReport = false;
                return profit;
            }

            for (int i = 1; i < selectedWallets.Count; i++)
            {
                if (selectedWallets[i - 1].Balance < selectedWallets[i].Balance)
                {
                    profit.SucceededTradesCount++;
                }
                else if (selectedWallets[i - 1].Balance > selectedWallets[i].Balance)
                {
                    profit.FailedTradesCount++;
                }
            }

            var diffList = new List<TimeSpan>(capacity: trades.Count - 1);
            for (int i = 1; i < trades.Count; i++)
            {
                diffList.Add(trades[i].TradeTime - trades[i - 1].TradeTime);
            }

            profit.SuccessFailureRatio = CalculateSuccessFailureRatio(profit.SucceededTradesCount, profit.FailedTradesCount);
            profit.AverageTradeThreshold = TimeSpan.FromSeconds(diffList.Average(diff => diff.TotalSeconds));
            profit.MinimalTradeThreshold = TimeSpan.FromSeconds(diffList.Min(diff => diff.TotalSeconds));
            profit.StartBalance = selectedWallets.First().Balance;
            profit.EndBalance = selectedWallets.Last().Balance;
            profit.IsFullReport = true;

            return profit;
        }

        private static double CalculateSuccessFailureRatio(int successCount, int failureCount)
        {
            if (failureCount == 0)
            {
                return double.MaxValue;
            }

            return (double)successCount / failureCount;
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
                hoursPassed = 0.016; // 1 minute.
            }

            return actualPercentage / hoursPassed;
        }
    }
}