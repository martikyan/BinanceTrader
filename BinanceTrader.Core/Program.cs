using System.IO;
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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configRoot = builder.Build();
            var config = configRoot.Get<CoreConfiguration>();

            using (var container = new WindsorContainer())
            {
                container.Install(new TraderInstaller(config));
                var dataFiller = container.Resolve<HistoricalDataDownloader>();
                var repo = container.Resolve<IRepository>();
                dataFiller.DownloadToRepositoryAsync().Wait();
                dataFiller.RecognizedUserTrades += DataFiller_RecognizedUserTrades;
                manualResetEvent.WaitOne();
            }
        }

        private static void DataFiller_RecognizedUserTrades(object sender, RecognizedUserTradesEventArgs e)
        {
            System.Console.WriteLine($"User with id {e.UserId} traded. Trade id: {e.TradeId}");
        }
    }
}