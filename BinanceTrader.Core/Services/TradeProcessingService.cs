using System;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.Extensions;
using BinanceTrader.Core.Models;
using CryptoExchange.Net.Authentication;

namespace BinanceTrader.Core.Services
{
    public class TradeProcessingService
    {
        private readonly CoreConfiguration _config;
        private readonly BinanceSocketClient _client;
        private readonly TradeRegistrarService _tradeRegistrar;
        private readonly UserProcessingService _ups;
        private bool _isStarted = false;

        public event EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTraded;

        public TradeProcessingService(CoreConfiguration config, TradeRegistrarService tradeRegistrar, UserProcessingService ups)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tradeRegistrar = tradeRegistrar ?? throw new ArgumentNullException(nameof(tradeRegistrar));
            _ups = ups ?? throw new ArgumentNullException(nameof(ups));

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

            var symbolPair = new SymbolPair(_config.FirstSymbol, _config.SecondSymbol);

            _isStarted = true;
            _tradeRegistrar.UserTraded += OnUserTraded;

            _client.SubscribeToTradesStream(symbolPair.ToString(), trade =>
            {
                _tradeRegistrar.RegisterTrade(trade.ToTradeModel(symbolPair));
            });
        }

        private void OnUserTraded(object sender, UserTradedEventArgs e)
        {
            var userProfit = _ups.GetUserProfit(e.UserId);

            if (IsProfitableUser(userProfit))
            {
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
                userProfit.MinimalTradeThreshold >= TimeSpan.FromSeconds(_config.MinimalTraderActivityThresholdSeconds) &&
                userProfit.WalletsCount >= _config.MinimalTraderTrades &&
                userProfit.ProfitPercentage >= _config.MinimalTraderProfitPercentage &&
                userProfit.CurrencySymbol == _config.TargetCurrencySymbol;
        }
    }
}