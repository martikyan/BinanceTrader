using System;
using System.IO;
using System.Linq;
using System.Threading;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Services;
using Castle.Windsor;
using Microsoft.Extensions.Configuration;

namespace BinanceTrader.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var manualResetEvent = new ManualResetEvent(false);
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var configRoot = builder.Build();
            var config = configRoot.Get<CoreConfiguration>();

            using (var container = new WindsorContainer())
            {
                container.Install(new TraderInstaller(config));
                var dataFiller = container.Resolve<HistoricalDataDownloaderService>();
                var tps = container.Resolve<TradeProcessingService>();
                tps.StartProcessingLiveTrades();

                tps.ProfitableUserTraded += Tps_ProfitableUserTraded;

                manualResetEvent.WaitOne();
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