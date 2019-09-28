using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task DownloadToRepositoryAsync(ITradeRepository repo)
        {
            var tradeLimit = 1000; // Maximum allowed by Binance.
            var firstTradeDate = DateTime.UtcNow - TimeSpan.FromSeconds(_config.TradeExpirationInSeconds);
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
                    var data = await client.GetHistoricalTradesAsync(SymbolUtilities.ConstructSymbolPair(_config.FirstSymbol, _config.SecondSymbol), tradeLimit, tradeIdContinuation);
                    var trades = data.Data
                        .Select(recentTrade => BinanceModelExtensions.ToBinanceStreamTrade(recentTrade))
                        .ToList();

                    foreach (var trade in trades)
                    {
                        if (trade.TradeTime < firstTradeDate)
                        {
                            break;
                        }

                        repo.SaveTrade(trade);
                    }

                    tradeIdContinuation = trades.Min(t => t.TradeId) - tradeLimit;
                }
            }
        }
    }
}