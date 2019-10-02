using System;
using System.Threading;
using System.Threading.Tasks;
using BinanceTrader.Core.AutoTrader;
using BinanceTrader.Core.Services;
using Microsoft.Extensions.Hosting;

namespace BinanceTrader.API
{
    public class TraderHostedService : IHostedService
    {
        private readonly TradeProcessingService _tradeProcessingService;
        private readonly IAutoTrader _autoTrader;

        public TraderHostedService(TradeProcessingService tradeProcessingService, IAutoTrader autoTrader)
        {
            _tradeProcessingService = tradeProcessingService ?? throw new ArgumentNullException(nameof(tradeProcessingService));
            _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _tradeProcessingService.ProfitableUserTraded += _autoTrader.ProfitableUserTradedHandler;
            _tradeProcessingService.StartProcessingLiveTrades();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _tradeProcessingService.ProfitableUserTraded -= _autoTrader.ProfitableUserTradedHandler;

            return Task.CompletedTask;
        }
    }
}