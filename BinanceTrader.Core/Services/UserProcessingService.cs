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
        private readonly IRepository _repository;
        private readonly ILogger _logger;

        public UserProcessingService(IRepository repo, ILogger logger)
        {
            _repository = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public UserProfitReport GetUserProfit(string userId)
        {
            _logger.Debug($"Getting user {userId} from repo with for profit calculation Id.");
            var user = _repository.GetUserById(userId);

            return GetUserProfit(user);
        }

        public UserProfitReport GetUserProfit(BinanceUser user)
        {
            _logger.Verbose($"Starting profit calculation for user with Id: {user.Identifier}");
            var profit = new UserProfitReport()
            {
                WalletsCount = user.Wallets.Count,
                CurrencySymbol = user.Wallets.First().Symbol,
            };

            var trades = new List<Trade>();
            foreach (var tradeId in user.Wallets.Select(w => w.WalletCreatedFromTradeId))
            {
                var trade = _repository.GetTradeById(tradeId);
                trades.Add(trade);
            }

            var wallets = new List<Wallet>(user.Wallets);

            if (wallets.Count < 2 || wallets.First().Symbol != wallets.Last().Symbol)
            {
                _logger.Verbose($"User with Id {user.Identifier} had small amount of information. Aborting report calculation.");
                profit.IsFullReport = false;
                return profit;
            }

            var diffList = new List<TimeSpan>(capacity: trades.Count - 1);
            for (int i = 1; i < trades.Count; i++)
            {
                diffList.Add(trades[i].TradeTime - trades[i - 1].TradeTime);
            }
            profit.AverageTradeThreshold = TimeSpan.FromSeconds(diffList.Average(diff => diff.TotalSeconds));
            profit.MinimalTradeThreshold = TimeSpan.FromSeconds(diffList.Min(diff => diff.TotalSeconds));

            var startBalance = wallets.First().Balance;
            var endBalance = wallets.Last().Balance;

            profit.StartBalance = startBalance;
            profit.EndBalance = endBalance;

            {
                var lastBalance = startBalance;
                foreach (var wallet in wallets.Where(w => w.Symbol == profit.CurrencySymbol))
                {
                    var profitPercentage = CalculateProfitPercentage(lastBalance, wallet.Balance);
                    if (profitPercentage > 0.0)
                    {
                        profit.SucceededTradesCount++;
                    }
                    else if (profitPercentage < 0.0)
                    {
                        profit.FailedTradesCount++;
                    }

                    lastBalance = wallet.Balance;
                }
            }

            profit.ProfitPercentage = CalculateProfitPercentage(startBalance, endBalance);
            profit.IsFullReport = true;

            return profit;
        }

        private static double CalculateProfitPercentage(decimal startBalance, decimal endBalance)
        {
            return (double)(endBalance * 100m / startBalance - 100m);
        }
    }
}