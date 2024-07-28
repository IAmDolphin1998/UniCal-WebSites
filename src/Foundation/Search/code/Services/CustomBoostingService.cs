using Sitecore.Abstractions;
using Sitecore.Buckets.Util;
using Sitecore.ContentSearch.Boosting;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Pipelines;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Sitecore.Rules.Conditions.FieldConditions;
using Sitecore.Rules.Conditions.ItemConditions;
using Sitecore.Rules.Conditions.PathConditions;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.SitecoreExtensions.Rules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using UniCal.Foundation.Search.Pipelines.CustomResolveBoostingQuery;

namespace UniCal.Foundation.Search.Services
{
    public class CustomBoostingService<T> : ICustomBoostingService<T> where T : ContentPage
    {
        private readonly BaseCorePipelineManager _pipelineManager;

        public CustomBoostingService(BaseCorePipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager;
        }

        public virtual RuleList<RuleBoostingContext> ExtractBoostingRules(IEnumerable<Item> ruleItems)
        {
            RuleList<RuleBoostingContext> boostingRules = new RuleList<RuleBoostingContext>();
            boostingRules.AddRange(RuleFactory.GetRules<RuleBoostingContext>(ruleItems, Sitecore.XA.Foundation.Search.Templates.IBoosting.Fields.Rule.ToString()).Rules);
            return boostingRules;
        }

        public virtual IQueryable<T> BoostQuery(
            IList<Item> boostingItems,
            string searchQuery,
            Item contextItem,
            IQueryable<T> queryable)
        {
            RuleList<RuleBoostingContext> boostingRules = this.ExtractBoostingRules((IEnumerable<Item>)boostingItems);
            return boostingRules.Count > 0 ? this.Boost(boostingItems.First<Item>(), searchQuery, contextItem, ref queryable, boostingRules) : queryable;
        }

        #region PRIVATE METHODS
        protected virtual IQueryable<T> Boost(
            Item boostingItem,
            string searchQuery,
            Item contextItem,
            ref IQueryable<T> queryable,
            RuleList<RuleBoostingContext> boostingRules)
        {
            foreach (Rule<RuleBoostingContext> rule in boostingRules.Rules)
            {
                RuleBoostingContext ruleContext = new RuleBoostingContext(boostingItem);
                rule.Execute(ruleContext);
                queryable = queryable.Where<T>(this.BuildConditionPredicates(rule.Condition, ruleContext.Boost, searchQuery, contextItem));
            }

            return queryable;
        }

        protected virtual Expression<Func<T, bool>> BuildConditionPredicates(
            RuleCondition<RuleBoostingContext> baseCondition,
            float boostValue,
            string searchQuery,
            Item contextItem)
        {
            switch (baseCondition)
            {
                case BinaryCondition<RuleBoostingContext> condition1:
                    return this.GetBinaryPredicate(condition1, boostValue, searchQuery, contextItem);
                case WhenField<RuleBoostingContext> whenField:
                    return this.GetFieldPredicate(whenField.OperatorId, whenField.FieldName, whenField.Value, boostValue);
                case WhenTemplateIs<RuleBoostingContext> whenTemplateIs:
                    return this.GetTemplatePredicate(whenTemplateIs.TemplateId, boostValue);
                case ParentTemplateCondition<RuleBoostingContext> condition2:
                    return this.GetParentTemplatePredicate(condition2, boostValue);
                case ItemPathCondition<RuleBoostingContext> condition3:
                    return this.GetItemPathPredicate(condition3, boostValue);
                case ItemNameCondition<RuleBoostingContext> condition4:
                    return this.GetItemNamePredicate(condition4, boostValue);
                case ItemIdCondition<RuleBoostingContext> condition5:
                    return this.GetItemIdPredicate(condition5, boostValue);
                case ItemTemplateCondition<RuleBoostingContext> condition6:
                    return this.GetTemplateInheritance(condition6, boostValue);
                case AncestorOrSelfCondition<RuleBoostingContext> condition7:
                    return this.GetAncestorOrSelfPredicate(condition7, boostValue);
                case WhenIsDescendantOrSelf<RuleBoostingContext> condition8:
                    return this.GetWhenIsDescendantOrSelfPredicate(condition8, boostValue);
                case LanguageCondition<RuleBoostingContext> condition9:
                    return this.GetLanguagePredicate(condition9, boostValue);
                case ItemLevelCondition<RuleBoostingContext> condition10:
                    return this.GetItemLevelPredicate(condition10, boostValue);
                case FieldEmpty<RuleBoostingContext> condition11:
                    return this.GetFieldEmptyPredicate(condition11, boostValue);
                case ParentNameCondition<RuleBoostingContext> condition12:
                    return this.GetParentNamePredicate(condition12, boostValue);
                case null:
                    return (Expression<Func<T, bool>>)(c => true);
                default:
                    ResolveBoostingQueryEventArgs<T> boostingQueryEventArgs = new ResolveBoostingQueryEventArgs<T>()
                    {
                        RuleCondition = baseCondition,
                        ContextItem = contextItem,
                        SearchQuery = searchQuery,
                        Boost = boostValue
                    };
                    ResolveBoostingQueryEventArgs<T> args = boostingQueryEventArgs;
                    _pipelineManager.Run("customResolveBoostingQuery", (PipelineArgs)args);
                    if (boostingQueryEventArgs.Predicate != null)
                        return boostingQueryEventArgs.Predicate;
                    goto case null;
            }
        }

        protected virtual Expression<Func<T, bool>> GetBinaryPredicate(
            BinaryCondition<RuleBoostingContext> condition,
            float boostValue,
            string searchQuery,
            Item contextItem)
        {
            if (condition != null)
            {
                Expression<Func<T, bool>> first = this.BuildConditionPredicates(condition.LeftOperand, boostValue, searchQuery, contextItem);
                Expression<Func<T, bool>> second = this.BuildConditionPredicates(condition.RightOperand, boostValue, searchQuery, contextItem);
                if (condition is OrCondition<RuleBoostingContext>)
                    return first.Or<T>(second);
                if (condition is AndCondition<RuleBoostingContext>)
                    return first.And<T>(second);
            }
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual Expression<Func<T, bool>> GetFieldPredicate(
          string operandId,
          string fieldName,
          string value,
          float boostValue)
        {
            value = value ?? string.Empty;
            if (operandId == Sitecore.XA.Foundation.Search.Templates.IsEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c[fieldName].Equals(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operandId == Sitecore.XA.Foundation.Search.Templates.IsNotEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(c => !c[fieldName].Equals(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operandId == Sitecore.XA.Foundation.Search.Templates.Contains.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c[fieldName].Contains(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operandId == Sitecore.XA.Foundation.Search.Templates.StartsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c[fieldName].StartsWith(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operandId == Sitecore.XA.Foundation.Search.Templates.EndsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c[fieldName].EndsWith(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual Expression<Func<T, bool>> GetParentTemplatePredicate(
          ParentTemplateCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            string normalizedTemplateId = this.NormalizeId(condition.TemplateId);
            return (Expression<Func<T, bool>>)(contentPage => contentPage.ParentTemplate.Equals(normalizedTemplateId).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
        }

        protected virtual Expression<Func<T, bool>> GetItemPathPredicate(
          ItemPathCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            string path = condition.Value;
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.IsEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Path.Equals(path).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.IsNotEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => !contentPage.Path.Equals(path).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.Contains.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Path.Contains(path).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.StartsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Path.StartsWith(path).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.EndsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Path.EndsWith(path).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual Expression<Func<T, bool>> GetTemplatePredicate(
          ID templateId,
          float boostValue)
        {
            string normalizedId = this.NormalizeId(templateId);
            return (Expression<Func<T, bool>>)(item => item["_template"].Equals(normalizedId).Boost<bool>(boostValue) || item.TemplateId != templateId);
        }

        protected virtual Expression<Func<T, bool>> GetTemplateInheritance(
          ItemTemplateCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            return (Expression<Func<T, bool>>)(contentPage => contentPage.Inheritance.Contains<ID>(condition.TemplateId).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
        }

        protected virtual Expression<Func<T, bool>> GetItemNamePredicate(
          ItemNameCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            string name = condition.Value;
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.IsEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Name.Equals(name).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.IsNotEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => !contentPage.Name.Equals(name).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.Contains.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Name.Contains(name).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.StartsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Name.StartsWith(name).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.EndsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Name.EndsWith(name).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual Expression<Func<T, bool>> GetItemIdPredicate(
          ItemIdCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            string normalizedId = this.NormalizeId(condition.Value);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.IsEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c["_group"].Equals(normalizedId).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.IsNotEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(c => !c["_group"].Equals(normalizedId).Boost<bool>(boostValue) || c.Name != string.Empty);
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual Expression<Func<T, bool>> GetAncestorOrSelfPredicate(
          AncestorOrSelfCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            return (Expression<Func<T, bool>>)(contentPage => contentPage.Paths.Contains<ID>(condition.ItemId).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
        }

        protected virtual Expression<Func<T, bool>> GetWhenIsDescendantOrSelfPredicate(
          WhenIsDescendantOrSelf<RuleBoostingContext> condition,
          float boostValue)
        {
            return (Expression<Func<T, bool>>)(contentPage => contentPage.Paths.Contains<ID>(condition.ItemId).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
        }

        protected virtual Expression<Func<T, bool>> GetLanguagePredicate(
          LanguageCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            return (Expression<Func<T, bool>>)(contentPage => contentPage.Language.Equals(condition.Value).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
        }

        protected virtual Expression<Func<T, bool>> GetItemLevelPredicate(
          ItemLevelCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.NumberIsEqualTo.Id.ToString())
            {
                string level = condition.Value.ToString((IFormatProvider)CultureInfo.InvariantCulture);
                return (Expression<Func<T, bool>>)(contentPage => contentPage["level"].Equals(level).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            }
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.NumberIsNotEqualTo.Id.ToString())
            {
                string level = condition.Value.ToString((IFormatProvider)CultureInfo.InvariantCulture);
                return (Expression<Func<T, bool>>)(contentPage => !contentPage["level"].Equals(level).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
            }
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.NumberIsGreaterThanOrEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Level >= condition.Value.Boost<int>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.NumberIsLessThan.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Level < condition.Value.Boost<int>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.NumberIsLessThanOrEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Level <= condition.Value.Boost<int>(boostValue) || contentPage.Name != string.Empty);
            if (condition.OperatorId == Sitecore.XA.Foundation.Search.Templates.NumberIsGreaterThan.Id.ToString())
                return (Expression<Func<T, bool>>)(contentPage => contentPage.Level > condition.Value.Boost<int>(boostValue) || contentPage.Name != string.Empty);
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual Expression<Func<T, bool>> GetFieldEmptyPredicate(
          FieldEmpty<RuleBoostingContext> condition,
          float boostValue)
        {
            return (Expression<Func<T, bool>>)(contentPage => contentPage[condition.FieldName].Equals(string.Empty).Boost<bool>(boostValue) || contentPage.Name != string.Empty);
        }

        protected virtual Expression<Func<T, bool>> GetParentNamePredicate(
          ParentNameCondition<RuleBoostingContext> condition,
          float boostValue)
        {
            string operatorId = condition.OperatorId;
            string value = condition.Value;
            if (operatorId == Sitecore.XA.Foundation.Search.Templates.IsEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c.ParentName.Equals(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operatorId == Sitecore.XA.Foundation.Search.Templates.IsNotEqualTo.Id.ToString())
                return (Expression<Func<T, bool>>)(c => !c.ParentName.Equals(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operatorId == Sitecore.XA.Foundation.Search.Templates.Contains.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c.ParentName.Contains(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operatorId == Sitecore.XA.Foundation.Search.Templates.StartsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c.ParentName.StartsWith(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            if (operatorId == Sitecore.XA.Foundation.Search.Templates.EndsWith.Id.ToString())
                return (Expression<Func<T, bool>>)(c => c.ParentName.EndsWith(value).Boost<bool>(boostValue) || c.Name != string.Empty);
            return (Expression<Func<T, bool>>)(contentPage => true);
        }

        protected virtual string NormalizeId(ID id) => this.NormalizeId(id.ToString());

        protected virtual string NormalizeId(string id)
        {
            return IdHelper.NormalizeGuid(id).ToLower(CultureInfo.InvariantCulture);
        }
        #endregion
    }
}