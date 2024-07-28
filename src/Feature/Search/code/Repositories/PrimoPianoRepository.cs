using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Mvc.Repositories.Base;
using Sitecore.XA.Foundation.RenderingVariants.Repositories;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.Search.Services;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using UniCal.Feature.Search.Models;
using UniCal.Foundation.Search.Models;
using UniCal.Foundation.Search.Services;

namespace UniCal.Feature.Search.Repositories
{
    public class PrimoPianoRepository : VariantsRepository, IPrimoPianoRepository
    {
        private readonly IVariantsRepository _variantsRepository;

        private readonly ISiteInfoResolver _siteInfoResolver;

        private readonly IScopeService _scopeService;

        private readonly ICustomSearchService<DocumentModel> _customSearchService;

        private readonly ISortingService _sortingService;

        public PrimoPianoRepository(IVariantsRepository variantsRepository, ISiteInfoResolver siteInfoResolver, IScopeService scopeService, ICustomSearchService<DocumentModel> customSearchService, ISortingService sortingService)
        {
            _variantsRepository = variantsRepository;
            _siteInfoResolver = siteInfoResolver;
            _scopeService = scopeService;
            _customSearchService = customSearchService;
            _sortingService = sortingService;
        }

        #region PROPERTIES

        public string HomeUrl => _siteInfoResolver.GetHomeUrl(this.PageContext.Current);

        protected virtual DefaultLanguageFilter DefaultLanguageFilter
        {
            get
            {
                string parameter = this.Rendering.Parameters[nameof(DefaultLanguageFilter)];
                if (ID.IsID(parameter))
                {
                    Item enumItem = this.Context.Database.GetItem(parameter);
                    if (enumItem != null)
                        return enumItem.ToEnum<DefaultLanguageFilter>();
                }

                return DefaultLanguageFilter.AllLanguages;
            }
        }

        protected virtual int PageSize => this.Rendering.Parameters.ParseInt(nameof(PageSize), 20);

        protected virtual string DefaultSortOrder
        {
            get
            {
                string parameter = this.Rendering.Parameters[nameof(DefaultSortOrder)];
                if (!string.IsNullOrWhiteSpace(parameter) && ID.IsID(parameter))
                {
                    Item obj = this.Context.Database.GetItem(parameter);
                    if (obj != null)
                    {
                        string str1 = string.Empty;
                        string str2 = string.Empty;
                        if (ID.IsID(obj["Facet"]))
                            str1 = this.Context.Database.GetItem(obj["Facet"]).Name;
                        if (ID.IsID(obj["Direction"]))
                            str2 = this.Context.Database.GetItem(obj["Direction"]).Name;
                        if (!string.IsNullOrWhiteSpace(str1) && !string.IsNullOrWhiteSpace(str2))
                            return str1 + "," + str2;
                    }
                }

                return string.Empty;
            }
        }

        #endregion     

        public override IRenderingModelBase GetModel()
        {
            PrimoPianoRenderingModel m = new PrimoPianoRenderingModel();
            FillBaseProperties((object)m);

            try
            {
                string index;
                IQueryable<DocumentModel> documents = _customSearchService.GetQuery(
                    new SearchQueryModel
                    {
                        ItemID = this.Context.Item.ID
                    },
                    out index
                );

                m.Documents = documents;
            }
            catch (Exception ex)
            {
                Log.Error("Results endpoint exception", ex, (object)this);
                throw new HttpResponseException(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = (HttpContent)new StringContent(ex.Message)
                });
            }

            return (IRenderingModelBase)m;
        }
    }
}