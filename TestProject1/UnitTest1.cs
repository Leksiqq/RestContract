using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.Dto;
using NUnit.Framework;
using System.Diagnostics;

namespace TestProject1
{
    public class Tests
    {
        private IHost _host;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices(serviceCollection =>
                {
                    DtoKit.Install(serviceCollection, services =>
                    {
                        services.AddTransient<IShipCall, ShipCall>();
                        services.AddTransient<ILocation, Location>();
                        services.AddTransient<IRoute, Route>();
                        services.AddTransient<ILine, Line>();
                        services.AddTransient<IVessel, Vessel>();
                        services.AddTransient<IShipCallForListing, ShipCall>();
                        services.AddTransient<IShipCallAdditionalInfo, ShipCall>();
                        services.AddTransient<IArrivalShipCall, ShipCall>();
                        services.AddTransient<IDepartureShipCall, ShipCall>();
                        services.AddTransient<IRouteShort, Route>();
                        services.AddTransient<IVesselShort, Vessel>();
                        services.AddTransient<ITravelForListing, Travel>();
                    });
                });
            _host = hostBuilder.Build();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}