using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using BinanceTrader.Core.Models;
using BinanceTrader.Core.Utils;

namespace BinanceTrader.Core.DataAccess
{
    // TODO MemoryCache is not working well
    public class Repository : IRepository
    {
        private readonly MemoryCache _tradesCache;
        private readonly MemoryCache _usersCache;
        private readonly CoreConfiguration _config;

        public Repository(CoreConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _tradesCache = new MemoryCache(Constants.Names.TradeCacheName);
            _usersCache = new MemoryCache(Constants.Names.UserCacheName);
        }

        public void AddOrUpdateTrade(Trade trade)
        {
            var key = trade.TradeId.ToString();
            if (_tradesCache.Get(key) != null)
            {
                _tradesCache.Remove(key);
            }

            _tradesCache.Add(key, trade, GetTimeoutPolicy());
        }

        public void AddOrUpdateUser(BinanceUser user)
        {
            var key = user.Identifier;
            if (_usersCache.Get(key) != null)
            {
                _usersCache.Remove(key);
            }

            _usersCache.Add(key, user, GetTimeoutPolicy());
        }

        public Trade GetTradeById(long tradeId)
        {
            return (Trade)_tradesCache.Get(tradeId.ToString());
        }

        public BinanceUser GetUserById(string userId)
        {
            return (BinanceUser)_usersCache.Get(userId);
        }

        public List<BinanceUser> GetUsersWithBalanceInRange(decimal lBalance, decimal hBalance, string symbol)
        {
            var allUsers = _usersCache.Select(u => u.Value).Cast<BinanceUser>();

            return allUsers.Where(u => u.Wallets.Any(w =>
                string.Equals(w.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                IsInRange(w.Balance, lBalance, hBalance)))
                .ToList();
        }

        private CacheItemPolicy GetTimeoutPolicy()
        {
            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_config.MemoryInSeconds),
            };

            return policy;
        }

        private bool IsInRange(decimal number, decimal low, decimal high)
        {
            return number >= low && number <= high;
        }
    }
}