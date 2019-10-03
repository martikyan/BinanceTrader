using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using CryptoExchange.Net.Authentication;
using Serilog;

namespace BinanceTrader.Core.AutoTrader
{
    public class BinanceAutoTrader : IAutoTrader
    {
        private readonly CoreConfiguration _config;
        private readonly SymbolPair _symbolPair;
        private readonly IRepository _repo;
        private readonly ILogger _logger;

        private DateTime _lastTradeDate;
        private DateTime _lastLockTime;
        private long _lockedDueToOrderId;
        private bool _isTradingLocked;

        public BinanceAutoTrader(CoreConfiguration config, IRepository repo, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _symbolPair = SymbolPair.Create(_config.FirstSymbol, _config.SecondSymbol);
            CurrentWallet = GetCurrentWalletAsync().Result;
        }

        public List<SymbolAmountPair> WalletHistory { get; private set; }

        public BinanceUser AttachedUser { get; private set; }

        public UserProfitReport AttachedUserProfit { get; private set; }

        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => HandleEventAsync;

        public List<string> AttachedUsersHistory { get; private set; }

        public SymbolAmountPair CurrentWallet { get; private set; }

        public void DetachAttachedUser()
        {
            throw new NotImplementedException();
        }

        private async Task<SymbolAmountPair> GetCurrentWalletAsync()
        {
            using (var client = CreateBinanceClient())
            {
                var accountInfo = await client.GetAccountInfoAsync();
                var balance = accountInfo.Data.Balances
                    .Where(b => b.Asset == _symbolPair.Symbol1 || b.Asset == _symbolPair.Symbol2)
                    .OrderBy(b => b.Total)
                    .First();

                return SymbolAmountPair.Create(balance.Asset, balance.Total);
            }
        }

        private async Task UpdateLockedStateAsync()
        {
            if (!_isTradingLocked)
            {
                return;
            }

            using (var client = CreateBinanceClient())
            {
                var order = await client.GetOrderAsync(_symbolPair.ToString(), orderId: _lockedDueToOrderId);
                var orderStatus = order.Data.Status;

                if (DateTime.UtcNow - _lastLockTime > TimeSpan.FromSeconds(_config.MaxTraderLockSeconds) ||
                    orderStatus == OrderStatus.Filled ||
                    orderStatus == OrderStatus.Expired ||
                    orderStatus == OrderStatus.Canceled ||
                    orderStatus == OrderStatus.Rejected)
                {
                    _logger.Warning($"Order with Id {_lockedDueToOrderId} had status of order {orderStatus}. The last lock time was {_lastLockTime}. Unlocking the trading.");
                    _isTradingLocked = false;
                }
            }
        }

        private BinanceClient CreateBinanceClient()
        {
            var creds = new ApiCredentials(_config.BinanceApiKey, _config.BinanceApiSecret);
            var options = new BinanceClientOptions() { ApiCredentials = creds };

            return new BinanceClient(options);
        }

        private async void HandleEventAsync(object sender, ProfitableUserTradedEventArgs e)
        {
            if (_isTradingLocked)
            {
                await UpdateLockedStateAsync();

                if (_isTradingLocked)
                {
                    _logger.Information($"Trading is locked. Skipping profitable user traded with user Id: {e.UserId}");
                    return;
                }
                else
                {
                    CurrentWallet = await GetCurrentWalletAsync();
                }
            }

            if (e.Report.CurrencySymbol != _config.TargetCurrencySymbol)
            {
                _logger.Information("Report was not targeting our currency symbol.");
                return;
            }

            if (AttachedUser == null || AttachedUser.Identifier == e.UserId)
            {
                if (AttachedUser == null)
                {
                    _logger.Information($"Attaching to user with Id: {e.UserId}");
                    AttachedUsersHistory.Add(e.UserId);
                    AttachedUserProfit = e.Report;
                    AttachedUser = _repo.GetUserById(e.UserId);
                }

                _logger.Information("Attached user traded. Repeating actions.");
                var trade = _repo.GetTradeById(e.TradeId);
                _lastTradeDate = trade.TradeTime;
                if (AttachedUser.CurrentWallet.Symbol == CurrentWallet.Symbol)
                {
                    _logger.Information("Currently the trader holds the currency that we already have.");
                    return;
                }

                _logger.Warning($"Wallet balance was {CurrentWallet.Amount}{CurrentWallet.Symbol}");
                _logger.Warning($"Selling {CurrentWallet.Amount}{CurrentWallet.Symbol} and buying {AttachedUser.CurrentWallet.Symbol}");

                using (var client = CreateBinanceClient())
                {
                    var orderSide = CurrentWallet.Symbol == _symbolPair.Symbol1 ? OrderSide.Sell : OrderSide.Buy;
                    var placeOrderResult = await client.PlaceOrderAsync(_symbolPair.ToString(), orderSide, OrderType.Limit, CurrentWallet.Amount);
                    _repo.BlacklistOrderId(placeOrderResult.Data.OrderId);

                    _isTradingLocked = true;
                    _lastLockTime = DateTime.UtcNow;
                    _lockedDueToOrderId = placeOrderResult.Data.OrderId;
                }
                _logger.Warning($"Locked account until order with Id {_lockedDueToOrderId} become filled/expired/rejected.");
            }
            else
            {
                var maxTimeToWaitForAttachedUser = TimeSpan.FromTicks(AttachedUserProfit.AverageTradeThreshold.Ticks * 2);
                if (DateTime.UtcNow - _lastTradeDate > maxTimeToWaitForAttachedUser ||
                    e.Report.ProfitPerHour > AttachedUserProfit.ProfitPerHour)
                {
                    _logger.Information("Detaching current user.");
                    AttachedUser = null;
                    HandleEventAsync(this, e);
                }
            }
        }
    }
}