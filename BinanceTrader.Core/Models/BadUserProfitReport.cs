using System.Collections.Generic;

namespace BinanceTrader.Core.Models
{
    public class BadUserProfitReport : UserProfitReport
    {
        private BadUserProfitReport()
        { }

        public List<string> Reasons { get; private set; }

        public static BadUserProfitReport Create(UserProfitReport profit, List<string> reasons)
        {
            var result = new BadUserProfitReport()
            {
                UserId = profit.UserId,
                EndBalance = profit.EndBalance,
                StartBalance = profit.StartBalance,
                IsFullReport = profit.IsFullReport,
                TotalTradesCount = profit.TotalTradesCount,
                SuccessFailureRatio = profit.SuccessFailureRatio,
                AverageTradesPerHour = profit.AverageTradesPerHour,
                AverageProfitPerHour = profit.AverageProfitPerHour,
                AverageTradeThreshold = profit.AverageTradeThreshold,
                MinimalTradeThreshold = profit.MinimalTradeThreshold,
            };

            result.Reasons = reasons;
            return result;
        }
    }
}