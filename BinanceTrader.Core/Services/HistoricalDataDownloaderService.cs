using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.Extensions;
using BinanceTrader.Core.Models;
using CryptoExchange.Net.Authentication;

namespace BinanceTrader.Core.Services
{
    public class HistoricalDataDownloaderService
    {
        private readonly CoreConfiguration _config;
        private readonly ITradeRegistrarService _tradeRegistrar;
        private const int _tradeRetrievalLimit = 1000; // Maximum allowed by Binance.

        public HistoricalDataDownloaderService(CoreConfiguration config, ITradeRegistrarService tradeRegistrar)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tradeRegistrar = tradeRegistrar ?? throw new ArgumentNullException(nameof(tradeRegistrar));
        }

        public async Task DownloadToRepositoryAsync()
        {
            var symbolPair = new SymbolPair(_config.FirstSymbol, _config.SecondSymbol);
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
                    var data = await client.GetHistoricalTradesAsync(symbolPair.ToString(), _tradeRetrievalLimit, tradeIdContinuation);
                    var trades = data.Data
                        .Select(recentTrade => BinanceModelExtensions.ToTradeModel(recentTrade, symbolPair))
                        .ToList();

                    foreach (var trade in trades)
                    {
                        _tradeRegistrar.RegisterTrade(trade);
                    }

                    if (trades.Min(t => t.TradeTime) < firstTradeDate)
                    {
                        break;
                    }

                    tradeIdContinuation = trades.Min(t => t.TradeId) - _tradeRetrievalLimit;
                }
            }
        }
    }
}