using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.Extensions;
using BinanceTrader.Core.Models;
using CryptoExchange.Net.Authentication;

namespace BinanceTrader.Core.Services
{
    /// <summary>
    /// TODO delete the downloader service. It contains bugs and it's hard to syncronize with realtime listener.
    /// </summary>
    public class HistoricalDataDownloaderService
    {
        private readonly CoreConfiguration _config;
        private readonly TradeRegistrarService _tradeRegistrar;
        private List<BinanceRecentTrade> _tradesList;
        private const int _tradeRetrievalLimit = 1000; // Maximum allowed by Binance.

        public HistoricalDataDownloaderService(CoreConfiguration config, TradeRegistrarService tradeRegistrar)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tradeRegistrar = tradeRegistrar ?? throw new ArgumentNullException(nameof(tradeRegistrar));

            _tradesList = new List<BinanceRecentTrade>();
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
                    var trades = data.Data;
                    _tradesList.AddRange(trades);

                    if (trades.Min(t => t.Time) < firstTradeDate)
                    {
                        break;
                    }

                    tradeIdContinuation = trades.Min(t => t.Id) - _tradeRetrievalLimit - 1;
                }
                _tradesList = _tradesList.OrderBy(t => t.Id).ToList();
                foreach (var trade in _tradesList)
                {
                    _tradeRegistrar.RegisterTrade(trade.ToTradeModel(new SymbolPair(_config.FirstSymbol, _config.SecondSymbol)));
                }

                Console.WriteLine("Done downloading historical data.");
            }
        }
    }
}