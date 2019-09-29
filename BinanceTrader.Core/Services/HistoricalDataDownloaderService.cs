using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Extensions;
using BinanceTrader.Core.Utils;
using CryptoExchange.Net.Authentication;

namespace BinanceTrader.Core.Services
{
    public class HistoricalDataDownloaderService
    {
        private readonly CoreConfiguration _config;
        private readonly SmartStorage _smartStorage;

        public event EventHandler<RecognizedUserTradesEventArgs> RecognizedUserTrades;

        public HistoricalDataDownloaderService(CoreConfiguration config, SmartStorage smartStorage)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _smartStorage = smartStorage ?? throw new ArgumentNullException(nameof(smartStorage));

            _smartStorage.RecognizedUserTraded += (s, e) => {
                RecognizedUserTrades?.Invoke(s, e);
            };
        }

        public async Task DownloadToRepositoryAsync()
        {
            var symbolPair = SymbolUtilities.ConstructSymbolPair(_config.FirstSymbol, _config.SecondSymbol);
            var tradeLimit = 1000; // Maximum allowed by Binance.
            var firstTradeDate = DateTime.UtcNow - TimeSpan.FromSeconds(_config.MemoryInSeconds);
            var credentials = new ApiCredentials(_config.BinanceApiKey, _config.BinanceApiSecret);
            var clientOptions = new BinanceClientOptions()
            {
                ApiCredentials = credentials,
            };

            using (var client = new BinanceClient(clientOptions))
            {
                long? tradeIdContinuation = null;

                while (true)
                {
                    var data = await client.GetHistoricalTradesAsync(symbolPair, tradeLimit, tradeIdContinuation);
                    var trades = data.Data
                        .Select(recentTrade => BinanceModelExtensions.ToTradeModel(recentTrade, symbolPair))
                        .ToList();

                    foreach (var trade in trades)
                    {
                        _smartStorage.RegisterTrade(trade);
                    }

                    if (trades.Min(t => t.TradeTime) < firstTradeDate)
                    {
                        break;
                    }

                    tradeIdContinuation = trades.Min(t => t.TradeId) - tradeLimit;
                }
            }
        }
    }
}