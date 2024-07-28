using Sitecore.ContentSearch.Boosting;
using Sitecore.Data.Items;
using Sitecore.Pipelines;
using Sitecore.XA.Foundation.Search.Models;
using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace UniCal.Foundation.Search.Pipelines.CustomResolveBoostingQuery
{
    public class ResolveBoostingQueryEventArgs<T> : PipelineArgs where T : ContentPage
    {
        public Expression<Func<T, bool>> Predicate { get; set; }

        public Sitecore.Rules.Conditions.RuleCondition<RuleBoostingContext> RuleCondition { get; set; }

        public string SearchQuery { get; set; }

        public float Boost { get; set; }

        public Item ContextItem { get; set; }

        public ResolveBoostingQueryEventArgs()
        {
        }

        protected ResolveBoostingQueryEventArgs(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            base.GetObjectData(info, context);
            info.AddValue("Predicate", (object)this.Predicate);
        }
    }
}