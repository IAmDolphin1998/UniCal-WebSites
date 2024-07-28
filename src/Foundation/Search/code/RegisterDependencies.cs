using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using UniCal.Foundation.Search.Models;
using UniCal.Foundation.Search.Services;

namespace UniCal.Foundation.Search
{
    public class RegisterDependencies : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ICustomSearchService<DocumentModel>, CustomSearchService<DocumentModel>>();
            serviceCollection.AddSingleton<ICustomBoostingService<DocumentModel>, CustomBoostingService<DocumentModel>>();
        }
    }
}