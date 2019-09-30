using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.Services
{
    public class UserProcessingService
    {
        private readonly IRepository _repository;
        private readonly CoreConfiguration _config;

        public UserProcessingService(IRepository repo, CoreConfiguration config)
        {
            _repository = repo ?? throw new ArgumentNullException(nameof(repo));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void UpdateUserTradeFee(string userId)
        {
            var user = _repository.GetUserById(userId);
            UpdateUserTradeFee(user);
        }

        public void UpdateUserTradeFee(BinanceUser user)
        {
            var trades = new List<Trade>(capacity: user.TradeIds.Count);
            foreach (var tradeId in user.TradeIds.OrderBy(tId => tId))
            {
                var trade = _repository.GetTradeById(tradeId);
                trades.Add(trade);
            }
        }

        public UserProfitReport GetUserProfit(string userId)
        {
            var user = _repository.GetUserById(userId);
            return GetUserProfit(user);
        }

        public UserProfitReport GetUserProfit(BinanceUser user)
        {
            var profit = new UserProfitReport()
            {
                TradesCount = user.TradeIds.Count,
                CurrencySymbol = _config.TargetCurrencySymbol,
            };

            var trades = new List<Trade>(capacity: user.TradeIds.Count);
            foreach (var tradeId in user.TradeIds.OrderBy(tId => tId))
            {
                var trade = _repository.GetTradeById(tradeId);
                trades.Add(trade);
            }

            var wallets = new List<Wallet>(user.WalletsHistory);
            wallets.Add(user.CurrentWallet);

            // Sorting and filtering wallets. // TODO sorting should not depend on tradeId
            wallets = wallets.Where(w => w.Symbol == _config.TargetCurrencySymbol).OrderBy(w => w.WalletCreatedFromTradeId).ToList();

            if (wallets.Count == 0)
            {
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
                foreach (var wallet in wallets)
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

            return profit;
        }

        private static double CalculateProfitPercentage(decimal startBalance, decimal endBalance)
        {
            return (double)(endBalance * 100m / startBalance - 100m);
        }
    }
}