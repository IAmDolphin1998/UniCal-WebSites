using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Boosting;
using Sitecore.Data.Items;
using Sitecore.Rules;
using System.Collections.Generic;
using System.Linq;

namespace UniCal.Foundation.Search.Services
{
    public interface ICustomBoostingService<T> where T : ISearchResult
    {
        RuleList<RuleBoostingContext> ExtractBoostingRules(IEnumerable<Item> ruleItems);

        IQueryable<T> BoostQuery(IList<Item> boostingItems, string searchQuery, Item contextItem, IQueryable<T> queryable);
    }
}
