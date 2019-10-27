using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly object _lockObject = new object();
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
            UpdateCurrentWallet();

            ProfitableUserTradedHandler = EventHandlerPredicate;
        }

        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler { get; private set; }

        public List<SymbolAmountPair> Wallets { get; } = new List<SymbolAmountPair>();
        public List<string> AttachedUsersHistory { get; } = new List<string>();
        public BinanceUser AttachedUser { get; private set; }
        public UserProfitReport AttachedUserProfit { get; private set; }
        public SymbolAmountPair CurrentWallet => Wallets.LastOrDefault();

        public void DetachAttachedUser()
        {
            _logger.Information($"Detaching user with Id {AttachedUser?.Identifier}");
            AttachedUser = null;
        }

        public void PauseTrading()
        {
            _logger.Information("The trading is paused.");
            ProfitableUserTradedHandler = null;
        }

        public void ResumeTrading()
        {
            _logger.Information("The trading is resumed.");
            ProfitableUserTradedHandler = EventHandlerPredicate;
        }

        public void UpdateCurrentWallet()
        {
            using (var client = CreateBinanceClient())
            {
                var price = client.GetPrice(_symbolPair.ToString());
                var accountInfo = client.GetAccountInfo();
                var s1b = accountInfo.Data.Balances
                    .Where(b => b.Asset == _symbolPair.Symbol1)
                    .First();

                var s2b = accountInfo.Data.Balances
                    .Where(b => b.Asset == _symbolPair.Symbol2)
                    .First();

                SymbolAmountPair cw;
                var s1pcb = s1b.Total * price.Data.Price;
                if (s1pcb > s2b.Total)
                {
                    cw = SymbolAmountPair.Create(s1b.Asset, s1b.Total);
                }
                else
                {
                    cw = SymbolAmountPair.Create(s2b.Asset, s2b.Total);
                }

                if (cw != CurrentWallet)
                {
                    Wallets.Add(cw);
                }
            }
        }

        private void UpdateLockedState()
        {
            if (!_isTradingLocked)
            {
                return;
            }

            using (var client = CreateBinanceClient())
            {
                var order = client.GetOrder(_symbolPair.ToString(), orderId: _lockedDueToOrderId);
                var orderStatus = order.Data.Status;

                if (orderStatus == OrderStatus.Filled ||
                    orderStatus == OrderStatus.Expired ||
                    orderStatus == OrderStatus.Canceled ||
                    orderStatus == OrderStatus.Rejected)
                {
                    _logger.Warning($"Order with Id {_lockedDueToOrderId} had status of order {orderStatus}. The last lock time was {_lastLockTime}. Unlocking the trading.");
                    _isTradingLocked = false;
                }
                else if (DateTime.UtcNow - _lastLockTime > TimeSpan.FromSeconds(_config.MaxTraderLockSeconds))
                {
                    client.CancelOrder(_symbolPair.ToString(), orderId: _lockedDueToOrderId);
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

        private void EventHandlerPredicate(object sender, ProfitableUserTradedEventArgs args)
        {
            lock (_lockObject)
            {
                try
                {
                    HandleEvent(sender, args);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"An exception was thrown while handling the ProfitableUserTraded event. Trade Id and user Id were: {args?.TradeId} {args?.UserId}");
                    throw;
                }
            }
        }

        private void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
            if (_isTradingLocked)
            {
                UpdateLockedState();

                if (_isTradingLocked)
                {
                    _logger.Information($"Trading is locked. Skipping profitable user with Id: {e.UserId}");
                    return;
                }
                else
                {
                    UpdateCurrentWallet();
                }
            }

            if (e.Report.CurrencySymbol != _config.TargetCurrencySymbol)
            {
                _logger.Information("Report was not targeting our currency symbol.");
                return;
            }

            var traderUser = _repo.GetUserById(e.UserId);
            if (traderUser.CurrentWallet.Symbol == CurrentWallet.Symbol)
            {
                _logger.Information("Currently the trader holds the currency that we already have.");
                return;
            }

            if (e.Report.AverageProfitPerHour < _config.Limiters.MinimalTraderProfitPerHourPercentage ||
                e.Report.AverageTradesPerHour > _config.Limiters.MaximalTraderTradesPerHour)
            {
                _logger.Information($"Skipping profitable user's trade due to limiters. Trader user Id was {e.UserId}");
                if (AttachedUser?.Identifier == e.UserId)
                {
                    DetachAttachedUser();
                }

                return;
            }

            if (AttachedUser == null || AttachedUser?.Identifier == e.UserId)
            {
                if (AttachedUser == null)
                {
                    _logger.Information($"Attaching to user with Id: {e.UserId}");
                    AttachedUsersHistory.Add(e.UserId);
                    AttachedUserProfit = e.Report;
                    AttachedUser = traderUser;
                }

                _logger.Information("Attached user traded. Repeating actions.");

                using (var client = CreateBinanceClient())
                {
                    var trade = _repo.GetTradeById(e.TradeId);
                    var orderSide = CurrentWallet.Symbol == _symbolPair.Symbol1 ? OrderSide.Sell : OrderSide.Buy;
                    var priceResult = client.GetPrice(_symbolPair.ToString());
                    var price = priceResult.Data.Price;
                    var quantity = CurrentWallet.Symbol == _symbolPair.Symbol1 ? CurrentWallet.Amount : CurrentWallet.Amount / price;
                    quantity = RecorrectQuantity(quantity);

                    _logger.Warning($"Wallet balance is {CurrentWallet.Amount}{CurrentWallet.Symbol}");
                    _logger.Warning($"Selling {CurrentWallet.Amount}{CurrentWallet.Symbol} and buying {AttachedUser.CurrentWallet.Symbol} with price of {price}.");

                    var placeOrderResult = client.PlaceOrder(
                        timeInForce: TimeInForce.GoodTillCancel,
                        symbol: _symbolPair.ToString(),
                        quantity: quantity,
                        type: OrderType.Limit,
                        side: orderSide,
                        price: price);

                    _repo.BlacklistOrder(placeOrderResult.Data.OrderId);

                    _isTradingLocked = true;
                    _lastTradeDate = trade.TradeTime;
                    _lastLockTime = DateTime.UtcNow;
                    _lockedDueToOrderId = placeOrderResult.Data.OrderId;
                }
                _logger.Warning($"Locked the trading until the order with Id {_lockedDueToOrderId} become filled/expired/rejected.");
            }
            else
            {
                var maxSecondsToWait = AttachedUserProfit.AverageTradeThreshold.TotalSeconds * Math.E;
                maxSecondsToWait = Math.Min(maxSecondsToWait, _config.Limiters.MaximalSecondsToWaitForTheTrader);

                if (DateTime.UtcNow - _lastTradeDate > TimeSpan.FromSeconds(maxSecondsToWait) ||
                    e.Report.AverageProfitPerHour > AttachedUserProfit.AverageProfitPerHour * Math.E &&
                    e.Report.AverageTradesPerHour < AttachedUserProfit.AverageTradesPerHour)
                {
                    DetachAttachedUser();
                    HandleEvent(this, e);
                }
            }
        }

        private decimal RecorrectQuantity(decimal amount)
        {
            var maxDec = _config.FirstSymbolMaxDecimals;
            amount *= 0.999m; // Manual fee calculation.
            amount *= (decimal)Math.Pow(10, maxDec);
            amount = Math.Floor(amount);
            amount *= (decimal)Math.Pow(10, -1 * maxDec);
            return amount;
        }
    }
}