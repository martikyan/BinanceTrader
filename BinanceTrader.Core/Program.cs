using System;
using System.IO;
using System.Threading;
using BinanceTrader.Core.Services;
using Castle.Windsor;
using Microsoft.Extensions.Configuration;
using Serilog;

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

        private static void Tps_ProfitableUserTraded(object sender, ProfitableUserTradedEventArgs e)
        {
            var userProfit = e.Report;

            Console.WriteLine($"==============User detected with positive profit==============");
            Console.WriteLine($"User ID : {e.UserId}");
            Console.WriteLine($"Detected on TradeID : {e.TradeId}");
            Console.WriteLine($"User profit: {userProfit.ProfitPercentage}%");
            Console.WriteLine($"Average trade threshold seconds: {userProfit.AverageTradeThreshold.TotalSeconds}");
            Console.WriteLine($"Minimal trade threshold seconds: {userProfit.MinimalTradeThreshold.TotalSeconds}");
            Console.WriteLine($"Success count: {userProfit.SucceededTradesCount}");
            Console.WriteLine($"Failed count: {userProfit.FailedTradesCount}");
            Console.WriteLine($"Start balance: {userProfit.StartBalance}{userProfit.CurrencySymbol}");
            Console.WriteLine($"Ending balance: {userProfit.EndBalance}{userProfit.CurrencySymbol}");
            Console.WriteLine($"==============================================================");
        }
    }
}