using System;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace BinanceTrader.Core
{
    public class TraderInstaller : IWindsorInstaller
    {
        private readonly CoreConfiguration _config;

        public TraderInstaller(CoreConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<CoreConfiguration>().Instance(_config));
            container.Register(Component.For<HistoricalDataDownloaderService>());
            container.Register(Component.For<UserProcessingService>());
            container.Register(Component.For<TradeRegistrarService>());
            container.Register(Component.For<TradeProcessingService>());
            container.Register(Component.For<IRepository>().ImplementedBy<Repository>());
        }
    }
}