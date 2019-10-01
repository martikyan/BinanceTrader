using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.DataAccess
{
    public class Repository : IRepository
    {
        private readonly MemoryCache _usersCache;
        private readonly MemoryCache _tradesCache;
        private readonly MemoryCache _commonAmountCache;
        private readonly CoreConfiguration _config;

        public Repository(CoreConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tradesCache = new MemoryCache(nameof(_tradesCache));
            _usersCache = new MemoryCache(nameof(_usersCache));
            _commonAmountCache = new MemoryCache(nameof(_commonAmountCache));
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

        public void DeleteTrade(long tradeId)
        {
            _tradesCache.Remove(tradeId.ToString());
        }

        public void DeleteUser(string userId)
        {
            _usersCache.Remove(userId);
        }

        public void DeleteUsers(IEnumerable<string> userIds)
        {
            foreach (var uId in userIds)
            {
                _usersCache.Remove(uId);
            }
        }

        public int GetCommonAmountLength(string symbol)
        {
            if (_commonAmountCache[symbol] != null)
            {
                return (int)_commonAmountCache[symbol];
            }

            var r1 = _tradesCache.Select(t => t.Value).Cast<Trade>().Where(t => t.SymbolPair.Symbol1 == symbol).Take(100);
            var r2 = _tradesCache.Select(t => t.Value).Cast<Trade>().Where(t => t.SymbolPair.Symbol2 == symbol).Take(100);

            var a1 = r1.Select(t => t.Quantity);
            var a2 = r2.Select(t => t.Price);

            var unionList = new List<decimal>();
            unionList.AddRange(a1);
            unionList.AddRange(a2);

            if (unionList.Count < 25)
            {
                return 0;
            }

            var minLen = unionList.Min(a => ((double)a).ToString().Length);
            var maxLen = unionList.Max(a => ((double)a).ToString().Length);
            var commonLength = (minLen + maxLen) / 2;

            _commonAmountCache[symbol] = commonLength;

            return commonLength;
        }

        public Trade GetTradeById(long tradeId)
        {
            return (Trade)_tradesCache.Get(tradeId.ToString());
        }

        public BinanceUser GetUserById(string userId)
        {
            return (BinanceUser)_usersCache.Get(userId);
        }

        public List<BinanceUser> GetUsersWithBalance(decimal balance, string symbol, double maxGivenFee)
        {
            var allUsers = _usersCache.Select(u => u.Value).Cast<BinanceUser>();

            return allUsers.Where(u => string.Equals(u.CurrentWallet.Symbol, symbol) &&
                IsInRange(balance, SubstractPercentage(u.CurrentWallet.Balance, maxGivenFee), u.CurrentWallet.Balance))
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

        private decimal SubstractPercentage(decimal fromValue, double percentage)
        {
            var p = fromValue * (100m - (decimal)percentage) / 100m;
            return p;
        }

        private bool IsInRange(decimal number, decimal low, decimal high)
        {
            return number >= low && number <= high;
        }
    }
}