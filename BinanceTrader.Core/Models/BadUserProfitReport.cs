using System.Collections.Generic;

namespace BinanceTrader.Core.Models
{
    public class BadUserProfitReport : UserProfitReport
    {
        public List<string> Reasons { get; private set; }
        private BadUserProfitReport()
        { }

        public static BadUserProfitReport Create(UserProfitReport profit, List<string> reasons)
        {
            var result = new BadUserProfitReport()
            {
                AverageProfitPerHour = profit.AverageProfitPerHour,
                AverageTradesPerHour = profit.AverageTradesPerHour,
                AverageTradeThreshold = profit.AverageTradeThreshold,
                CurrencySymbol = profit.CurrencySymbol,
                EndBalance = profit.EndBalance,
                IsFullReport = profit.IsFullReport,
                MinimalTradeThreshold = profit.MinimalTradeThreshold,
                StartBalance = profit.StartBalance,
                SuccessFailureRatio = profit.SuccessFailureRatio,
                TotalTradesCount = profit.TotalTradesCount,
                UserId = profit.UserId,
            };

            result.Reasons = reasons;

            return result;
        }
    }
}
