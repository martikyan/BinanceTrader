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
        private static IRepository repo;

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
                var dataFiller = container.Resolve<HistoricalDataDownloaderService>();
                var registrar = container.Resolve<ITradeRegistrarService>();
                repo = container.Resolve<IRepository>();

                registrar.RecognizedUserTraded += Registrar_RecognizedUserTraded;

                dataFiller.DownloadToRepositoryAsync().Wait();
                manualResetEvent.WaitOne();
            }
        }

        private static void Registrar_RecognizedUserTraded(object sender, RecognizedUserTradesEventArgs e)
        {
            var user = repo.GetUserById(e.UserId);
            if (user.TradeIds.Count < 7)
            {
                return;
            }

            Console.WriteLine($"=========Continuous Recognized User Traded========");
            Console.WriteLine($"User Id: {e.UserId}");
            var prefix = "****";
            Console.WriteLine(string.Join("->", user.WalletsHistory.Select(w => $"{w.Balance} {w.Symbol}" )));

            prefix += "****";
            foreach (var tradeId in user.TradeIds)
            {
                var trade = repo.GetTradeById(tradeId);
                Console.WriteLine($"{prefix} Trade Id: {tradeId}");
                Console.WriteLine($"{prefix} Trade symbol pair: {trade.SymbolPair}");
                Console.WriteLine($"{prefix} Trade quantity: {trade.Quantity}");
                Console.WriteLine($"{prefix} Trade price: {trade.Price}");
                Console.WriteLine($"{prefix} Trade q*p: {trade.Price * trade.Quantity}");
                Console.WriteLine($"{prefix} Trade time: {trade.TradeTime}");
                Console.WriteLine();
            }

            Console.WriteLine($"=======================================");
        }
    }
}