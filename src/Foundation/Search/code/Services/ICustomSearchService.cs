using Sitecore.ContentSearch;
using Sitecore.XA.Foundation.Search.Models;
using System.Linq;

namespace UniCal.Foundation.Search.Services
{
    public interface ICustomSearchService<T> where T : ISearchResult
    {
        IQueryable<T> GetQuery(SearchQueryModel model, out string indexName);
    }
}
