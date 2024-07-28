using Sitecore.ContentSearch.Boosting;
using Sitecore.ContentSearch.Linq;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.Search.Rules;
using System;
using System.Linq.Expressions;

namespace UniCal.Foundation.Search.Pipelines.CustomResolveBoostingQuery
{
    public class ResolveFieldContainsCurrentPageFieldRule<T> : ResolveBoostingQueryProcessor<T> where T : ContentPage
    {
        public override Type Condition
        {
            get => typeof(WhenFieldContainsCurrentPageFieldContent<RuleBoostingContext>);
        }

        public override void ResolveBoostingQuery(ResolveBoostingQueryEventArgs<T> args)
        {
            if (args?.ContextItem == null)
                return;
            WhenFieldContainsCurrentPageFieldContent<RuleBoostingContext> condition = args.RuleCondition as WhenFieldContainsCurrentPageFieldContent<RuleBoostingContext>;
            if (string.IsNullOrWhiteSpace(condition.FieldName) || string.IsNullOrWhiteSpace(condition.SourceFieldName))
                return;
            string contextPageFieldContent = args.ContextItem[condition.SourceFieldName];
            args.Predicate = (Expression<Func<T, bool>>)(contentPage => contentPage[condition.FieldName].Contains(contextPageFieldContent).Boost<bool>(args.Boost) || contentPage.Name != string.Empty);
        }
    }
}