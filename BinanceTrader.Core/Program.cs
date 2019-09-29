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
                var dataFiller = container.Resolve<HistoricalDataDownloaderService>();
                var repo = container.Resolve<IRepository>();
                dataFiller.DownloadToRepositoryAsync().Wait();
                manualResetEvent.WaitOne();
            }
        }
    }
}