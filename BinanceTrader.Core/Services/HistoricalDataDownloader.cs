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
    public class HistoricalDataDownloader
    {
        private readonly CoreConfiguration _config;

        public HistoricalDataDownloader(CoreConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task DownloadToRepositoryAsync(IRepository repo)
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
                        repo.AddOrUpdateTrade(trade);
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