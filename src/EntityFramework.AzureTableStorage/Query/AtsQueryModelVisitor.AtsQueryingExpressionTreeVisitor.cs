﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.Data.Entity.AzureTableStorage.Query.Expressions;
using Microsoft.Data.Entity.Query.ExpressionTreeVisitors;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace Microsoft.Data.Entity.AzureTableStorage.Query
{
    public partial class AtsQueryModelVisitor
    {
        protected class AtsEntityQueryableExpressionTreeVisitor : EntityQueryableExpressionTreeVisitor
        {
            private readonly AtsQueryModelVisitor _parent;
            private readonly IQuerySource _querySource;

            public AtsEntityQueryableExpressionTreeVisitor(AtsQueryModelVisitor parent, IQuerySource querySource)
                : base(parent)
            {
                _parent = parent;
                _querySource = querySource;
            }

            protected override Expression VisitSubQueryExpression(SubQueryExpression subQueryExpression)
            {
                var visitor = new AtsQueryModelVisitor(_parent);
                visitor.VisitQueryModel(subQueryExpression.QueryModel);
                return visitor.Expression;
            }

            protected override Expression VisitEntityQueryable(Type elementType)
            {
                var query = new SelectExpression(elementType);
                _parent._queriesBySource[_querySource] = query;

                var entityType = _parent.QueryCompilationContext.Model.GetEntityType(elementType);

                return Expression.Call(
                    _executeQueryMethodInfo.MakeGenericMethod(elementType),
                    QueryContextParameter,
                    Expression.Constant(entityType),
                    Expression.Constant(query)
                    );
            }
        }
    }
}
