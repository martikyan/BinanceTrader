using System.Collections.Generic;
using Binance.Net.Objects;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.DataAccess
{
    public interface IRepository
    {
        void AddOrUpdateTrade(Trade trade);

        void AddOrUpdateUser(BinanceUser user);

        Trade GetTradeById(long tradeId);

        BinanceUser GetUserById(string userId);

        List<BinanceUser> GetUsersWithBalanceInRange(decimal lBalance, decimal hBalance, string symbol);
    }
}