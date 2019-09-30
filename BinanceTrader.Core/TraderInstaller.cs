using System;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Serilog;

namespace BinanceTrader.Core
{
    public class TraderInstaller : IWindsorInstaller
    {
        private readonly CoreConfiguration _config;
        private readonly ILogger _logger;

        public TraderInstaller(CoreConfiguration config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<ILogger>().Instance(_logger));
            container.Register(Component.For<CoreConfiguration>().Instance(_config));
            container.Register(Component.For<HistoricalDataDownloaderService>());
            container.Register(Component.For<UserProcessingService>());
            container.Register(Component.For<TradeRegistrarService>());
            container.Register(Component.For<TradeProcessingService>());
            container.Register(Component.For<IRepository>().ImplementedBy<Repository>());
        }
    }
}