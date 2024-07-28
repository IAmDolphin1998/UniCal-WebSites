using Sitecore.ContentSearch.Boosting;
using Sitecore.Rules.Conditions;
using Sitecore.XA.Foundation.Search.Models;
using System;

namespace UniCal.Foundation.Search.Pipelines.CustomResolveBoostingQuery
{
    public abstract class ResolveBoostingQueryProcessor<T> where T : ContentPage
    {
        public abstract Type Condition { get; }

        public bool CanResolve(RuleCondition<RuleBoostingContext> ruleCondition)
        {
            return this.Condition == ruleCondition.GetType();
        }

        public abstract void ResolveBoostingQuery(ResolveBoostingQueryEventArgs<T> args);

        public void Process(ResolveBoostingQueryEventArgs<T> args)
        {
            if (!this.CanResolve(args.RuleCondition))
                return;
            this.ResolveBoostingQuery(args);
        }
    }
}