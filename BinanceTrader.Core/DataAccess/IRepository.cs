using System.Collections.Generic;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.DataAccess
{
    public interface IRepository
    {
        void BlacklistOrderId(long orderId);

        bool IsOrderIdBlackListed(long orderId);

        int GetCommonAmountLength(string symbol);

        void AddOrUpdateTrade(Trade trade);

        void AddOrUpdateUser(BinanceUser user);

        void DeleteTrade(long tradeId);

        void DeleteUser(string userId);

        void DeleteUsers(IEnumerable<string> userIds);

        Trade GetTradeById(long tradeId);

        BinanceUser GetUserById(string userId);

        List<BinanceUser> GetUsersWithBalance(decimal balance, string symbol, double maxGivenFee);
    }
}