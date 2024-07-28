using Sitecore.ContentSearch.Boosting;
using Sitecore.ContentSearch.Linq;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.Search.Rules;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System;
using System.Linq.Expressions;

namespace UniCal.Foundation.Search.Pipelines.CustomResolveBoostingQuery
{
    public class ResolveFieldAndQueryMatchRule<T> : ResolveBoostingQueryProcessor<T> where T : ContentPage
    {
        public override Type Condition => typeof(WhenFieldAndQueryMatches<RuleBoostingContext>);

        public override void ResolveBoostingQuery(ResolveBoostingQueryEventArgs<T> args)
        {
            if (string.IsNullOrWhiteSpace(args.SearchQuery))
                return;
            WhenFieldAndQueryMatches<RuleBoostingContext> ruleCondition = args.RuleCondition as WhenFieldAndQueryMatches<RuleBoostingContext>;
            string operatorId = ruleCondition.OperatorId;
            string fieldName = ruleCondition.FieldName;
            if (operatorId.EqualsId(Sitecore.XA.Foundation.Search.Templates.SearchStringOperators.IsEqualTo.Id))
                args.Predicate = (Expression<Func<T, bool>>)(c => c[fieldName].Equals(args.SearchQuery).Boost<bool>(args.Boost) || c.Name != string.Empty);
            else if (operatorId.EqualsId(Sitecore.XA.Foundation.Search.Templates.SearchStringOperators.IsNotEqualTo.Id))
                args.Predicate = (Expression<Func<T, bool>>)(c => !c[fieldName].Equals(args.SearchQuery).Boost<bool>(args.Boost) || c.Name != string.Empty);
            else if (operatorId.EqualsId(Sitecore.XA.Foundation.Search.Templates.SearchStringOperators.Contains.Id))
                args.Predicate = (Expression<Func<T, bool>>)(c => c[fieldName].Contains(args.SearchQuery).Boost<bool>(args.Boost) || c.Name != string.Empty);
            else if (operatorId.EqualsId(Sitecore.XA.Foundation.Search.Templates.SearchStringOperators.StartsWith.Id))
                args.Predicate = (Expression<Func<T, bool>>)(c => c[fieldName].StartsWith(args.SearchQuery).Boost<bool>(args.Boost) || c.Name != string.Empty);
            else if (operatorId.EqualsId(Sitecore.XA.Foundation.Search.Templates.SearchStringOperators.EndsWith.Id))
                args.Predicate = (Expression<Func<T, bool>>)(c => c[fieldName].EndsWith(args.SearchQuery).Boost<bool>(args.Boost) || c.Name != string.Empty);
            else
                args.Predicate = (Expression<Func<T, bool>>)(contentPage => true);
        }
    }
}