using System;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.Extensions;
using BinanceTrader.Core.Models;
using CryptoExchange.Net.Authentication;
using Serilog;

namespace BinanceTrader.Core.Services
{
    public class TradeProcessingService
    {
        private readonly CoreConfiguration _config;
        private readonly BinanceSocketClient _client;
        private readonly TradeRegistrarService _tradeRegistrar;
        private readonly UserProcessingService _ups;
        private readonly ILogger _logger;
        private bool _isStarted;

        public event EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTraded;

        public TradeProcessingService(CoreConfiguration config, TradeRegistrarService tradeRegistrar, UserProcessingService ups, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tradeRegistrar = tradeRegistrar ?? throw new ArgumentNullException(nameof(tradeRegistrar));
            _ups = ups ?? throw new ArgumentNullException(nameof(ups));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var clientOptions = new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(_config.BinanceApiKey, _config.BinanceApiSecret),
                AutoReconnect = true,
            };

            _client = new BinanceSocketClient(clientOptions);
        }

        public void StartProcessingLiveTrades()
        {
            if (_isStarted)
            {
                throw new InvalidOperationException($"{nameof(TradeProcessingService)} was already started processing live trades.");
            }
            _logger.Debug("Starting processing live trades.");
            var symbolPair = new SymbolPair(_config.FirstSymbol, _config.SecondSymbol);

            _isStarted = true;
            _tradeRegistrar.UserTraded += OnUserTraded;

            _client.SubscribeToTradesStream(symbolPair.ToString(), trade =>
            {
                var ping = (DateTime.UtcNow - trade.TradeTime).TotalSeconds;
                if (ping > _config.Limiters.MaximumAllowedTradeSyncSeconds / 2 ||
                    ping < _config.Limiters.MaximumAllowedTradeSyncSeconds / -2)
                {
                    _logger.Error($"Detected trade sync time downgrade with ping {ping} seconds. Try synchronizing machine time or checking the config value with name: {nameof(_config.Limiters.MaximumAllowedTradeSyncSeconds)}");
                    Environment.Exit(1);
                }

                if (trade.Quantity <= _config.Limiters.MinimalTradeQuantity)
                {
                    return;
                }

                _logger.Verbose($"Detected trade with Id {trade.TradeId}");
                _tradeRegistrar.RegisterTrade(trade.ToTradeModel(symbolPair));
            });
        }

        private void OnUserTraded(object sender, UserTradedEventArgs e)
        {
            _logger.Verbose($"The user with Id {e.UserId} seems to be trading.");
            var userProfit = _ups.GetUserProfit(e.UserId);

            if (IsProfitableUser(userProfit))
            {
                _logger.Information($"The user with Id {e.UserId} seems to be profitable.");
                var eventArgs = new ProfitableUserTradedEventArgs()
                {
                    UserId = e.UserId,
                    TradeId = e.TradeId,
                    Report = userProfit,
                };

                ProfitableUserTraded?.Invoke(this, eventArgs);
            }
        }

        private bool IsProfitableUser(UserProfitReport userProfit)
        {
            return
                userProfit.IsFullReport &&
                userProfit.MinimalTradeThreshold >= TimeSpan.FromSeconds(_config.Limiters.MinimalTraderActivityThresholdSeconds) &&
                userProfit.WalletsCount >= _config.Limiters.MinimalTraderWalletsCount &&
                userProfit.ProfitPerHour >= _config.Limiters.MinimalTraderProfitPerHourPercentage &&
                userProfit.SuccessFailureRatio >= _config.Limiters.MinimalSuccessFailureRatio &&
                userProfit.CurrencySymbol == _config.TargetCurrencySymbol;
        }
    }
}