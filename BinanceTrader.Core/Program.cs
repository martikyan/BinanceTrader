using System.IO;
using System.Threading;
using BinanceTrader.Core.AutoTrader;
using BinanceTrader.Core.Services;
using Castle.Windsor;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace BinanceTrader.Core
{
    public class Program
    {
        private static ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build()
                .Get<CoreConfiguration>();

            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("log.txt", restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            using (var container = new WindsorContainer())
            {
                container.Install(new TraderInstaller(config, logger));
                var tps = container.Resolve<TradeProcessingService>();
                var autoTrader = container.Resolve<IAutoTrader>();

                tps.ProfitableUserTraded += autoTrader.ProfitableUserTradedHandler;
                tps.StartProcessingLiveTrades();

                _resetEvent.WaitOne();
            }
        }
    }
}