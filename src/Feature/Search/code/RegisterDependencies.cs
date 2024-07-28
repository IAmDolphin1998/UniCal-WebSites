using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using UniCal.Feature.Search.Controllers;
using UniCal.Feature.Search.Repositories;

namespace UniCal.Feature.Search
{
    public class RegisterDependencies : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IPrimoPianoRepository, PrimoPianoRepository>();
            serviceCollection.AddTransient<PrimoPianoController>();
        }
    }
}