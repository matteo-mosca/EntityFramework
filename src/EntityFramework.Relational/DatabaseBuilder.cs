// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Model;
using Microsoft.Data.Entity.Relational.Utilities;
using Microsoft.Data.Entity.Utilities;
using ForeignKey = Microsoft.Data.Entity.Relational.Model.ForeignKey;
using Index = Microsoft.Data.Entity.Relational.Model.Index;

namespace Microsoft.Data.Entity.Relational
{
    public abstract class DatabaseBuilder
    {
        // TODO: IModel may not be an appropriate cache key if we want to be
        // able to unload IModel instances and create new ones.
        // Issue #765
        private readonly ThreadSafeDictionaryCache<IModel, ModelDatabaseMapping> _mappingCache
            = new ThreadSafeDictionaryCache<IModel, ModelDatabaseMapping>();

        private readonly RelationalTypeMapper _typeMapper;

        protected DatabaseBuilder([NotNull] RelationalTypeMapper typeMapper)
        {
            Check.NotNull(typeMapper, "typeMapper");

            _typeMapper = typeMapper;
        }

        public virtual RelationalTypeMapper TypeMapper
        {
            get { return _typeMapper; }
        }

        public virtual DatabaseModel GetDatabase([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            return GetMapping(model).Database;
        }

        public virtual ModelDatabaseMapping GetMapping([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            return _mappingCache.GetOrAdd(model, BuildMapping);
        }

        protected virtual ModelDatabaseMapping BuildMapping([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            // TODO: Consider making this lazy since we don't want to load the whole model just to
            // save changes to a single entity.
            var database = new DatabaseModel();
            var mapping = new ModelDatabaseMapping(model, database);

            foreach (var entityType in model.EntityTypes)
            {
                var table = BuildTable(database, entityType);
                mapping.Map(entityType, table);

                foreach (var property in OrderProperties(entityType))
                {
                    mapping.Map(property, BuildColumn(table, property));

                    BuildSequence(property, database);
                }

                var primaryKey = entityType.GetPrimaryKey();
                if (primaryKey != null)
                {
                    mapping.Map(primaryKey, BuildPrimaryKey(database, primaryKey));
                }

                foreach (var key in entityType.Keys.Except(new[] { primaryKey }))
                {
                    mapping.Map(key, BuildUniqueConstraint(database, key));
                }

                foreach (var index in entityType.Indexes)
                {
                    mapping.Map(index, BuildIndex(database, index));
                }
            }

            foreach (var entityType in model.EntityTypes)
            {
                foreach (var foreignKey in entityType.ForeignKeys)
                {
                    mapping.Map(foreignKey, BuildForeignKey(database, foreignKey));
                }
            }

            return mapping;
        }

        private IEnumerable<IProperty> OrderProperties(IEntityType entityType)
        {
            var primaryKey = entityType.GetPrimaryKey();

            var primaryKeyProperties
                = primaryKey != null
                    ? primaryKey.Properties.ToArray()
                    : new IProperty[0];

            var foreignKeyProperties
                = entityType.ForeignKeys
                    .SelectMany(fk => fk.Properties)
                    .Except(primaryKeyProperties)
                    .Distinct()
                    .ToArray();

            var otherProperties
                = entityType.Properties
                    .Except(primaryKeyProperties.Concat(foreignKeyProperties))
                    .OrderBy(GetColumnName)
                    .ToArray();

            return primaryKeyProperties
                .Concat(otherProperties)
                .Concat(foreignKeyProperties);
        }

        private string PrimaryKeyName([NotNull] IKey primaryKey)
        {
            Check.NotNull(primaryKey, "primaryKey");

            return GetKeyName(primaryKey) ?? string.Format("PK_{0}", GetFullTableName(primaryKey.EntityType));
        }

        private string UniqueConstraintName([NotNull] IKey key)
        {
            Check.NotNull(key, "key");

            return GetKeyName(key) ?? string.Format(
                "UC_{0}_{1}",
                GetFullTableName(key.EntityType),
                string.Join("_", key.Properties.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(GetColumnName)));
        }

        private string ForeignKeyName([NotNull] IForeignKey foreignKey)
        {
            Check.NotNull(foreignKey, "foreignKey");

            return GetForeignKeyName(foreignKey) ?? string.Format(
                "FK_{0}_{1}_{2}",
                GetFullTableName(foreignKey.EntityType),
                GetFullTableName(foreignKey.ReferencedEntityType),
                string.Join("_", foreignKey.Properties.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(GetColumnName)));
        }

        private string IndexName([NotNull] IIndex index)
        {
            Check.NotNull(index, "index");

            return GetIndexName(index) ?? string.Format(
                "IX_{0}_{1}",
                GetFullTableName(index.EntityType),
                string.Join("_", index.Properties.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(GetColumnName)));
        }

        private string GetFullTableName(IEntityType entityType)
        {
            var schema = GetSchema(entityType);
            var tableName = GetTableName(entityType);
            return !string.IsNullOrEmpty(schema) ? schema + "." + tableName : tableName;
        }

        // TODO: Make database builders for all providers, including SQLite, then remove these relational-only methods
        // See Issues #853, #875
        protected virtual string GetSchema(IEntityType entityType)
        {
            return entityType.Relational().Schema;
        }

        protected virtual string GetTableName(IEntityType entityType)
        {
            return entityType.Relational().Table;
        }

        protected virtual string GetIndexName(IIndex index)
        {
            return index.Relational().Name;
        }

        protected virtual string GetColumnName(IProperty property)
        {
            return property.Relational().Column;
        }

        protected virtual string GetColumnType(IProperty property)
        {
            return property.Relational().ColumnType;
        }

        protected virtual object GetColumnDefaultValue(IProperty property)
        {
            return property.Relational().DefaultValue;
        }

        protected virtual string GetColumnDefaultSql(IProperty property)
        {
            return property.Relational().DefaultExpression;
        }

        protected virtual string GetForeignKeyName(IForeignKey foreignKey)
        {
            return foreignKey.Relational().Name;
        }

        protected virtual string GetKeyName(IKey key)
        {
            return key.Relational().Name;
        }

        protected virtual bool IsKeyClustered(IKey key)
        {
            // TODO: Clustered is SQL Server-specific elsewhere in the stack
            // Issue #879
            return false;
        }

        protected virtual bool IsIndexClustered(IIndex index)
        {
            // TODO: Clustered is SQL Server-specific elsewhere in the stack
            // Issue #879
            return false;
        }

        protected virtual SchemaQualifiedName GetSchemaQualifiedName(IEntityType entityType)
        {
            return new SchemaQualifiedName(GetTableName(entityType), GetSchema(entityType));
        }

        private Table BuildTable(DatabaseModel database, IEntityType entityType)
        {
            var table = new Table(GetSchemaQualifiedName(entityType));

            database.AddTable(table);

            return table;
        }

        private Column BuildColumn(Table table, IProperty property)
        {
            var column =
                new Column(GetColumnName(property), property.PropertyType, GetColumnType(property))
                    {
                        IsNullable = property.IsNullable,
                        DefaultValue = GetColumnDefaultValue(property),
                        DefaultSql = GetColumnDefaultSql(property),
                        GenerateValueOnAdd =  property.GenerateValueOnAdd,
                        IsComputed = property.IsStoreComputed,
                        IsTimestamp = property.PropertyType == typeof(byte[]) && property.IsConcurrencyToken,
                        MaxLength = property.MaxLength > 0 ? property.MaxLength : (int?)null
                    };

            // TODO: This is a workaround to get the value-generation annotations into the relational model
            // so they can be used for appropriate DDL gen. Hopefully changes can be made to avoid copying all
            // this stuff, or to do it in a cleaner manner.
            // Issue #767
            foreach (var annotation in property.EntityType.Model.Annotations
                .Concat(property.EntityType.Annotations)
                .Concat(property.Annotations))
            {
                column[annotation.Name] = annotation.Value;
            }

            table.AddColumn(column);

            return column;
        }

        private PrimaryKey BuildPrimaryKey(DatabaseModel database, IKey primaryKey)
        {
            Check.NotNull(primaryKey, "primaryKey");

            var table = database.GetTable(GetSchemaQualifiedName(primaryKey.EntityType));
            var columns = primaryKey.Properties.Select(
                p => table.GetColumn(GetColumnName(p))).ToArray();
            // TODO: Clustered is SQL Server-specific elsewhere in the stack
            // Issue #879
            var isClustered = IsKeyClustered(primaryKey);

            table.PrimaryKey = new PrimaryKey(PrimaryKeyName(primaryKey), columns, isClustered);

            return table.PrimaryKey;
        }

        private UniqueConstraint BuildUniqueConstraint(DatabaseModel database, IKey key)
        {
            Check.NotNull(key, "key");

            var table = database.GetTable(GetSchemaQualifiedName(key.EntityType));
            var columns = key.Properties.Select(
                p => table.GetColumn(GetColumnName(p))).ToArray();

            var uniqueConstraint = new UniqueConstraint(UniqueConstraintName(key), columns);

            table.AddUniqueConstraint(uniqueConstraint);

            return uniqueConstraint;
        }

        private ForeignKey BuildForeignKey(DatabaseModel database, IForeignKey foreignKey)
        {
            Check.NotNull(foreignKey, "foreignKey");

            var table = database.GetTable(GetSchemaQualifiedName(foreignKey.EntityType));
            var referencedTable = database.GetTable(GetSchemaQualifiedName(foreignKey.ReferencedEntityType));
            var columns = foreignKey.Properties.Select(
                p => table.GetColumn(GetColumnName(p))).ToArray();
            var referenceColumns = foreignKey.ReferencedProperties.Select(
                p => referencedTable.GetColumn(GetColumnName(p))).ToArray();
            // TODO: Cascading behaviors not supported. Issue #333
            //var cascadeDelete = foreignKey.CascadeDelete();

            var storeForeignKey = new ForeignKey(
                ForeignKeyName(foreignKey), columns, referenceColumns, cascadeDelete: false);

            table.AddForeignKey(storeForeignKey);

            return storeForeignKey;
        }

        private Index BuildIndex(DatabaseModel database, IIndex index)
        {
            Check.NotNull(index, "index");

            var table = database.GetTable(GetSchemaQualifiedName(index.EntityType));
            var columns = index.Properties.Select(
                p => table.GetColumn(GetColumnName(p))).ToArray();

            // TODO: Clustered is SQL Server-specific elsewhere in the stack
            // Issue #879
            var storeIndex = new Index(
                IndexName(index), columns, index.IsUnique, IsIndexClustered(index));

            table.AddIndex(storeIndex);

            return storeIndex;
        }

        private void BuildSequence([NotNull] IProperty property, [NotNull] DatabaseModel database)
        {
            Check.NotNull(property, "property");
            Check.NotNull(database, "database");

            var sequence = BuildSequence(property);
            if (sequence == null)
            {
                return;
            }

            var existingSequence = database.TryGetSequence(sequence.Name);
            if (existingSequence == null)
            {
                database.AddSequence(sequence);
                return;
            }

            if (sequence.Type != existingSequence.Type
                || sequence.StartWith != existingSequence.StartWith
                || sequence.IncrementBy != existingSequence.IncrementBy)
            {
                throw new InvalidOperationException(Strings.SequenceDefinitionMismatch(sequence.Name));
            }
        }

        protected abstract Sequence BuildSequence([NotNull] IProperty property);
    }
}
