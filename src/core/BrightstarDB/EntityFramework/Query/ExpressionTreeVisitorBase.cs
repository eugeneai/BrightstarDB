﻿using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BrightstarDB.Rdf;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    internal abstract class ExpressionTreeVisitorBase : ThrowingExpressionTreeVisitor
    {
        protected SparqlQueryBuilder QueryBuilder;

        protected ExpressionTreeVisitorBase(SparqlQueryBuilder queryBuilder)
        {
            QueryBuilder = queryBuilder;
        }

        protected string FormatUnhandledItem<T>(T unhandledItem)
        {
            var itemAsExpression = unhandledItem as Expression;
            return itemAsExpression != null ? FormattingExpressionTreeVisitor.Format(itemAsExpression) : unhandledItem.ToString();
        }

        protected PropertyHint GetPropertyHint(Expression expression)
        {
            if (expression is MemberExpression)
            {
                var memberExpression = expression as MemberExpression;
#if PORTABLE || NETCORE
                if (memberExpression.Member is PropertyInfo)
#else
                if (memberExpression.Member.MemberType == MemberTypes.Property)
#endif
                {
                    var propertyInfo = memberExpression.Member as PropertyInfo;
                    return QueryBuilder.Context.GetPropertyHint(propertyInfo);
                }
            }
            return null;
        }

        protected PropertyInfo GetPropertyInfo(Expression expression)
        {
            if (expression is MemberExpression)
            {
                var memberExpression = expression as MemberExpression;
#if PORTABLE || NETCORE
                return memberExpression.Member as PropertyInfo;
#else
                if (memberExpression.Member.MemberType == MemberTypes.Property)
                {
                    return memberExpression.Member as PropertyInfo;
                }
#endif
            }
            return null;
        }

        protected string GetSourceVarName(MemberExpression expression)
        {
            string sourceVarName = null;
            if (expression.Expression is QuerySourceReferenceExpression)
            {
                var querySource = expression.Expression as QuerySourceReferenceExpression;
                Expression mappedExpression;
                if (QueryBuilder.TryGetQuerySourceMapping(querySource.ReferencedQuerySource, out mappedExpression) &&
                    mappedExpression is SelectVariableNameExpression)
                {
                    sourceVarName = (mappedExpression as SelectVariableNameExpression).Name;
                }
                else
                {
                    sourceVarName = querySource.ReferencedQuerySource.ItemName;
                    QueryBuilder.AddQuerySourceMapping(
                        querySource.ReferencedQuerySource,
                        new SelectVariableNameExpression(querySource.ReferencedQuerySource.ItemName, VariableBindingType.Resource, querySource.ReferencedQuerySource.ItemType));
                }

            }
            else if (expression.Expression is MemberExpression)
            {

                var memberExpression = VisitExpression(expression.Expression);
                if (memberExpression is SelectVariableNameExpression)
                {
                    sourceVarName = (memberExpression as SelectVariableNameExpression).Name;
                }
            }
            return sourceVarName;
        }

        
        

        public string MakeResourceAddress(PropertyInfo identifierProperty, string topicId)
        {
            return QueryBuilder.Context.MapIdToUri(identifierProperty, topicId);
        }

        protected bool HandleAddressOrIdEquals(Expression left, Expression right)
        {
            string itemName = null;
            PropertyHint propertyHint = GetPropertyHint(left);
            if (propertyHint == null ||
                !(propertyHint.MappingType == PropertyMappingType.Address ||
                  propertyHint.MappingType == PropertyMappingType.Id))
            {
                return false;
            }
            if (left is SelectVariableNameExpression)
            {
                itemName = (left as SelectVariableNameExpression).Name;
            }
            else if (left is MemberExpression)
            {
                var m = left as MemberExpression;
                if (m.Expression is QuerySourceReferenceExpression)
                {
                    itemName = (m.Expression as QuerySourceReferenceExpression).ReferencedQuerySource.ItemName;
                }
                else
                {
                    var visitResult = VisitMemberExpression(m);
                    if (visitResult is SelectVariableNameExpression)
                    {
                        itemName = (visitResult as SelectVariableNameExpression).Name;
                    }
                }
            }
            if (itemName == null) return false;
            var constantExpression = right as ConstantExpression;
            if (constantExpression == null) return false;
            string address = null;
            if (propertyHint.MappingType == PropertyMappingType.Id)
            {
                address = MakeResourceAddress(GetPropertyInfo(left), constantExpression.Value.ToString());
            }
            else if (propertyHint.MappingType == PropertyMappingType.Address)
            {
                address = constantExpression.Value.ToString();
            }
            if (address != null)
            {
                if (QueryBuilder.SelectVariables.Contains(itemName))
                {
                    QueryBuilder.AddFilterExpression(String.Format("(?{0}=<{1}>)", itemName, address));
                }
                else
                {
                    QueryBuilder.ConvertVariableToConstantUri(itemName, address);
                }
                return true;
            }
            return false;
        }

        protected string GetDatatype(Type systemType)
        {
            return QueryBuilder.Context.GetDatatype(systemType);
        }

        protected internal override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            if (expression is SelectVariableNameExpression)
            {
                return VisitSelectVariableNameExpression(expression as SelectVariableNameExpression);
            }
            return base.VisitExtensionExpression(expression);
        }

        protected virtual Expression VisitSelectVariableNameExpression(SelectVariableNameExpression expression)
        {
            return expression;
        }
    }
}