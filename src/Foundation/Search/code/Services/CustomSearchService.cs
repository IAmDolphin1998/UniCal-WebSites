using Microsoft.Extensions.DependencyInjection;
using Sitecore.Abstractions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Pipelines;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.Search.Pipelines.NormalizeSearchPhrase;
using Sitecore.XA.Foundation.Search.Services;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace UniCal.Foundation.Search.Services
{
    public class CustomSearchService<T> : ICustomSearchService<T> where T : ContentPage
    {
        private readonly ISearchContextService _searchContextService;

        private readonly IMultisiteContext _multisiteContext;

        private readonly IIndexResolver _indexResolver;

        private readonly IContext _context;

        private readonly ICustomBoostingService<T> _customBoostingService;

        private readonly ISearchQueryTokenResolver _searchQueryTokenResolver;

        private readonly BaseCorePipelineManager _pipelineManager;

        public CustomSearchService(ISearchContextService searchContextService, IMultisiteContext multisiteContext, IIndexResolver indexResolver, IContext context, ICustomBoostingService<T> customBoostingService, ISearchQueryTokenResolver searchQueryTokenResolver)
        {
            _searchContextService = searchContextService;
            _multisiteContext = multisiteContext;
            _indexResolver = indexResolver;
            _context = context;
            _customBoostingService = customBoostingService;
            _searchQueryTokenResolver = searchQueryTokenResolver;
            _pipelineManager = ServiceLocator.ServiceProvider.GetService<BaseCorePipelineManager>();
        }

        #region PROPERTIES
        public bool IsGeolocationRequest
        {
            get => ((IEnumerable<string>)_context.Request.QueryString.AllKeys).Contains<string>("g");
        }
        #endregion

        public virtual IQueryable<T> GetQuery(SearchQueryModel searchQueryModel, out string indexName)
        {
            Item contextItem = this.GetContextItem(searchQueryModel.ItemID);
            ISearchIndex searchIndex = _indexResolver.ResolveIndex(contextItem);
            IList<Item> list = (IList<Item>)searchQueryModel.ScopesIDs.Select<ID, Item>(new Func<ID, Item>(_context.Database.GetItem)).ToList<Item>();
            indexName = searchIndex.Name;

            IEnumerable<SearchStringModel> models = list.Select<Item, string>((Func<Item, string>)(i => i["ScopeQuery"])).SelectMany<string, SearchStringModel>(new Func<string, IEnumerable<SearchStringModel>>(SearchStringModel.ParseDatasourceString));
            IEnumerable<SearchStringModel> searchStringModel = _searchQueryTokenResolver.Resolve((IEnumerable<SearchStringModel>)models.ToList<SearchStringModel>(), contextItem);
            IQueryable<T> query = LinqHelper.CreateQuery<T>(searchIndex.CreateSearchContext(), searchStringModel);

            string str = this.NormalizeSearchPhrase(searchQueryModel.Query);
            IQueryable<T> queryable = query.Where<T>(this.IsGeolocationRequest ?
                this.GeolocationPredicate(searchQueryModel.Site) :
                this.PageOrMediaPredicate(searchQueryModel.Site))
                    .Where<T>(this.ContentPredicate(str))
                    .Where<T>(this.LanguagePredicate(searchQueryModel.Languages))
                    .Where<T>(this.LatestVersionPredicate())/*.ApplyFacetFilters(_context.Request.QueryString, searchQueryModel.Coordinates, searchQueryModel.Site)*/;

            return _customBoostingService.BoostQuery(list, str, contextItem, queryable);
        }

        #region PRIVATE METHODS
        protected virtual Expression<Func<T, bool>> GeolocationPredicate(string siteName)
        {
            Item homeItem = _searchContextService.GetHomeItem(siteName);
            Item siteItem = _multisiteContext.GetSiteItem(homeItem);
            if (homeItem == null || siteItem == null)
                return PredicateBuilder.False<T>();
            string siteShortId = siteItem.ID.ToSearchID();
            Expression<Func<T, bool>> first = (Expression<Func<T, bool>>)(i => i.RawPath == siteShortId && i.IsPointOfInterest);
            MultilistField field = (MultilistField)_multisiteContext.GetSettingsItem(homeItem)?.Fields[Sitecore.XA.Foundation.Search.Templates._SearchCriteria.Fields.AssociatedContent];
            if (field != null)
            {
                foreach (string str in ((IEnumerable<ID>)field.TargetIDs).Select<ID, string>((Func<ID, string>)(i => i.ToSearchID())))
                {
                    string id = str;
                    first = first.Or<T>((Expression<Func<T, bool>>)(i => i.RawPath == id && i.IsPointOfInterest));
                }
            }

            return first;
        }

        protected virtual Expression<Func<T, bool>> PageOrMediaPredicate(string siteName)
        {
            Item homeItem = _searchContextService.GetHomeItem(siteName);
            if (homeItem == null)
                return PredicateBuilder.False<T>();
            string homeShortId = homeItem.ID.ToSearchID();
            Expression<Func<T, bool>> first = (Expression<Func<T, bool>>)(i => i.RawPath == homeShortId && i.IsSearchable);
            Item settingsItem = _multisiteContext.GetSettingsItem(homeItem);
            if (settingsItem != null)
            {
                MultilistField field1 = (MultilistField)settingsItem.Fields[Sitecore.XA.Foundation.Search.Templates._SearchCriteria.Fields.AssociatedContent];
                if (field1 != null)
                {
                    foreach (string str in ((IEnumerable<ID>)field1.TargetIDs).Select<ID, string>((Func<ID, string>)(i => i.ToSearchID())))
                    {
                        string id = str;
                        first = first.Or<T>((Expression<Func<T, bool>>)(i => i.RawPath == id && i.IsSearchable));
                    }
                }
                MultilistField field2 = (MultilistField)settingsItem.Fields[Sitecore.XA.Foundation.Search.Templates._SearchCriteria.Fields.AssociatedMedia];
                if (field2 != null)
                {
                    foreach (string str in ((IEnumerable<Item>)field2.GetItems()).Select<Item, string>((Func<Item, string>)(i => i.ID.ToSearchID())))
                    {
                        string shortId = str;
                        first = first.Or<T>((Expression<Func<T, bool>>)(i => i.RawPath == shortId));
                    }
                }
            }

            return first;
        }

        protected virtual Expression<Func<T, bool>> ContentPredicate(string content)
        {
            Expression<Func<T, bool>> first = PredicateBuilder.True<T>();
            if (string.IsNullOrWhiteSpace(content))
                return first;
            foreach (string str in ((IEnumerable<string>)content.Split()).TrimAndRemoveEmpty())
            {
                string t = str;
                first = first.And<T>((Expression<Func<T, bool>>)(i => i.AggregatedContent.Contains(t) || i.AggregatedContent.Equals(t, StringComparison.InvariantCultureIgnoreCase)));
            }

            return first;
        }

        protected virtual Expression<Func<T, bool>> LanguagePredicate(IEnumerable<string> languages)
        {
            if (!languages.Any<string>())
                return PredicateBuilder.True<T>();
            Expression<Func<T, bool>> seed = PredicateBuilder.False<T>();

            return languages.Aggregate<string, Expression<Func<T, bool>>>(seed, (Func<Expression<Func<T, bool>>, string, Expression<Func<T, bool>>>)((p, l) => p.Or<T>((Expression<Func<T, bool>>)(i => i.Language == l))));
        }

        protected virtual Expression<Func<T, bool>> LatestVersionPredicate()
        {
            return PredicateBuilder.True<T>().And<T>((Expression<Func<T, bool>>)(i => i.LatestVersion));
        }

        protected virtual string NormalizeSearchPhrase(string phrase)
        {
            NormalizeSearchPhraseEventArgs args = new NormalizeSearchPhraseEventArgs()
            {
                Phrase = phrase
            };
            _pipelineManager.Run("normalizeSearchPhrase", (PipelineArgs)args);
            return args.Phrase;
        }

        protected virtual Item GetContextItem(ID itemId)
        {
            Item contextItem = (Item)null;
            if (!ID.IsNullOrEmpty(itemId))
                contextItem = _context.Database.GetItem(itemId);
            return contextItem;
        }
        #endregion
    }
}