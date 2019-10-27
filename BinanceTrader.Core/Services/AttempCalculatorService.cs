using System;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;

namespace BinanceTrader.Core.Services
{
    public class AttempCalculatorService
    {
        private int _rotatingKey;
        private readonly CoreConfiguration _config;
        private readonly MemoryCache _tradeAttempts;

        public AttempCalculatorService(CoreConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var cacheConfig = new NameValueCollection();
            cacheConfig.Add(nameof(_tradeAttempts.PollingInterval), TimeSpan.FromSeconds(10).ToString());
            _tradeAttempts = new MemoryCache(nameof(_tradeAttempts), config: cacheConfig);
        }

        public bool IsSucceededAttemp()
        {
            var tradeIndicators = _tradeAttempts.ToList();
            if (tradeIndicators.Count >= _config.Limiters.AttempsPassCount)
            {
                foreach (var i in tradeIndicators)
                {
                    _tradeAttempts.Remove(i.Key);
                }

                return true;
            }

            _tradeAttempts.Add(_rotatingKey.ToString(), true, GetTimeoutPolicy());
            _rotatingKey = unchecked(_rotatingKey + 1);

            return false;
        }

        private CacheItemPolicy GetTimeoutPolicy()
        {
            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_config.Limiters.AttempMemoryInSeconds),
            };

            return policy;
        }
    }
}