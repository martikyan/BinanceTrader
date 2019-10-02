using BinanceTrader.Core;
using BinanceTrader.Core.AutoTrader;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BinanceTrader.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new CoreConfiguration();
            Configuration.GetSection(nameof(CoreConfiguration)).Bind(config);

            services.AddSingleton(config);
            services.AddSingleton(Log.Logger);
            services.AddSingleton<UserProcessingService>();
            services.AddSingleton<TradeRegistrarService>();
            services.AddSingleton<TradeProcessingService>();
            services.AddSingleton<IRepository, Repository>();

            if (config.EnableAutoTrade)
            {
                services.AddSingleton<IAutoTrader, BinanceAutoTrader>();
            }
            else
            {
                services.AddSingleton<IAutoTrader, FakeAutoTrader>();
            }

            services.AddHostedService<TraderHostedService>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}