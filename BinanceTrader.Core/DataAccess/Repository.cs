using System;
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
        private readonly CacheItemPolicy _cachePolicy;
        private readonly CoreConfiguration _config;

        public Repository(CoreConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));

            _cachePolicy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(_config.MemoryInSeconds),
                Priority = CacheItemPriority.NotRemovable, // ToDo temp
            };

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

            Console.WriteLine("Adding trade to trades cache");
            _tradesCache.Add(key, trade, _cachePolicy);
        }

        public void AddOrUpdateUser(BinanceUser user)
        {
            var key = user.Identifier;
            if (_usersCache.Get(key) != null)
            {
                _usersCache.Remove(key);
            }
            Console.WriteLine("Adding user to users cache");
            _usersCache.Add(key, user, _cachePolicy);
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
            Console.WriteLine($"UsersCache had {_usersCache.ToList().Count()} elementes");
            var allUsers = _usersCache.Cast<BinanceUser>();
            Console.WriteLine($"All users had {allUsers.Count()} people");
            return allUsers.Where(u => u.Wallets.Any(w =>
                string.Equals(w.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                IsInRange(w.Balance, lBalance, hBalance)))
                .ToList();
        }

        private bool IsInRange(decimal number, decimal low, decimal high)
        {
            return number >= low && number <= high;
        }
    }
}