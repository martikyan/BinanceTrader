using System;
using System.Collections.Generic;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.DataAccess;
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
        private readonly IRepository _repo;
        private readonly ILogger _logger;
        private bool _isStarted;

        public event EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTraded;

        public TradeProcessingService(CoreConfiguration config, TradeRegistrarService tradeRegistrar, UserProcessingService ups, IRepository repo, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tradeRegistrar = tradeRegistrar ?? throw new ArgumentNullException(nameof(tradeRegistrar));
            _ups = ups ?? throw new ArgumentNullException(nameof(ups));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
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

            _isStarted = true;
            _logger.Debug("Starting processing live trades.");
            _tradeRegistrar.UserTraded += OnUserTraded;
            var symbolPair = SymbolPair.Create(_config.FirstSymbol, _config.SecondSymbol);

            _client.SubscribeToTradesStream(symbolPair.ToString(), trade =>
            {
                var ping = (DateTime.UtcNow - trade.TradeTime).TotalSeconds;
                if (ping > _config.Limiters.MaximalAllowedTradeSyncSeconds / 2 ||
                    ping < _config.Limiters.MaximalAllowedTradeSyncSeconds / -2)
                {
                    _logger.Warning($"Detected trade sync time downgrade with ping {ping} seconds. Try synchronizing machine time or checking the config value with name: {nameof(_config.Limiters.MaximalAllowedTradeSyncSeconds)}");
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

            if (IsProfitableUser(userProfit, out BadUserProfitReport badProfit))
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
            else
            {
                _repo.AddOrUpdateBadUserProfitReport(badProfit);
            }
        }

        private bool IsProfitableUser(UserProfitReport userProfit, out BadUserProfitReport badProfit)
        {
            var l = _config.Limiters;
            var result = userProfit.IsFullReport &&
                userProfit.TotalTradesCount >= l.MinimalTraderTradesCount &&
                userProfit.SuccessFailureRatio >= l.MinimalSuccessFailureRatio &&
                userProfit.AverageTradesPerHour <= l.MaximalTraderTradesPerHour &&
                userProfit.AverageProfitPerHour >= l.MinimalTraderProfitPerHourPercentage &&
                userProfit.MinimalTradeThreshold >= TimeSpan.FromSeconds(l.MinimalTraderActivityThresholdSeconds);

            badProfit = null;
            if (result == false && userProfit.IsFullReport)
            {
                var reasonSet = new HashSet<string>();
                badProfit = BadUserProfitReport.Create(userProfit, reasonSet);

                if (userProfit.TotalTradesCount < l.MinimalTraderTradesCount)
                {
                    reasonSet.Add(nameof(userProfit.TotalTradesCount));
                }

                if (userProfit.SuccessFailureRatio < l.MinimalSuccessFailureRatio)
                {
                    reasonSet.Add(nameof(userProfit.SuccessFailureRatio));
                }

                if (userProfit.AverageTradesPerHour > l.MaximalTraderTradesPerHour)
                {
                    reasonSet.Add(nameof(userProfit.AverageTradesPerHour));
                }

                if (userProfit.AverageProfitPerHour < l.MinimalTraderProfitPerHourPercentage)
                {
                    reasonSet.Add(nameof(userProfit.AverageProfitPerHour));
                }

                if (userProfit.MinimalTradeThreshold < TimeSpan.FromSeconds(l.MinimalTraderActivityThresholdSeconds))
                {
                    reasonSet.Add(nameof(userProfit.MinimalTradeThreshold));
                }
            }

            return result;
        }
    }
}