using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Ego.WebHookCatcher;

namespace EgoCatcher.Tests.Utils
{
    public class ApiWebApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // will be called after the `ConfigureServices` from the Startup
            builder.ConfigureLogging(logging =>
             {
                 logging.ClearProviders();
                 logging.AddConsole();
             });

            builder.ConfigureAppConfiguration(config =>
            {
                var integrationConfig = new ConfigurationBuilder()
                      .AddJsonFile("appsettings.test.json")
                      .AddUserSecrets<ApiWebApplicationFactory>()
                      .AddEnvironmentVariables()
                      .Build();

                config.AddConfiguration(integrationConfig);

            });

            builder.ConfigureTestServices(services =>
            {
                //services.AddTransient<IWeatherForecastConfigService, WeatherForecastConfigStub>();
            });
        }
    }

}
