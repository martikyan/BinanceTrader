using System;
using System.Collections.Specialized;
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
            var result = false;
            var tradesCount = _tradeAttempts.GetCount();
            if (tradesCount >= _config.Limiters.AttempsPassCount)
            {
                result = true;
            }

            _tradeAttempts.Add(_rotatingKey.ToString(), true, GetTimeoutPolicy());
            _rotatingKey = unchecked(_rotatingKey + 1);

            return result;
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