// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Data.Entity.Infrastructure
{
    /// <summary>
    ///     These are convenience methods for obtaining services directly from the DI container for situations
    ///     where using normal constructor injecttion for each service is not appropriate. For example, this is
    ///     used in <see cref="StateEntry" /> instances where we want to carry only one reference around rather than
    ///     carrying a reference for dependent service.
    /// </summary>
    public class ContextServices
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     This constructor is intended only for use when creating test doubles that will override members
        ///     with mocked or faked behavior. Use of this constructor for other purposes may result in unexpected
        ///     behavior including but not limited to throwing <see cref="NullReferenceException" />.
        /// </summary>
        protected ContextServices()
        {
        }

        public ContextServices([NotNull] IServiceProvider serviceProvider)
        {
            Check.NotNull(serviceProvider, "serviceProvider");

            _serviceProvider = serviceProvider;
        }

        public virtual IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        public virtual ClrPropertyGetterSource ClrPropertyGetterSource
        {
            get { return _serviceProvider.GetRequiredServiceChecked<ClrPropertyGetterSource>(); }
        }

        public virtual ClrPropertySetterSource ClrPropertySetterSource
        {
            get { return _serviceProvider.GetRequiredServiceChecked<ClrPropertySetterSource>(); }
        }

        public virtual ClrCollectionAccessorSource ClrCollectionAccessorSource
        {
            get { return _serviceProvider.GetRequiredServiceChecked<ClrCollectionAccessorSource>(); }
        }

        public virtual ContextSets ContextSets
        {
            get { return ServiceProvider.GetRequiredServiceChecked<ContextSets>(); }
        }

        public virtual DataStoreSelector DataStoreSelector
        {
            get { return _serviceProvider.GetRequiredServiceChecked<DataStoreSelector>(); }
        }

        public virtual EntityMaterializerSource EntityMaterializerSource
        {
            get { return _serviceProvider.GetRequiredServiceChecked<EntityMaterializerSource>(); }
        }

        public virtual StateManager StateManager
        {
            get { return ServiceProvider.GetRequiredServiceChecked<StateManager>(); }
        }

        public virtual EntityKeyFactorySource EntityKeyFactorySource
        {
            get { return _serviceProvider.GetRequiredServiceChecked<EntityKeyFactorySource>(); }
        }

        public virtual IModelSource ModelSource
        {
            get { return _serviceProvider.GetRequiredServiceChecked<IModelSource>(); }
        }

        public virtual OriginalValuesFactory OriginalValuesFactory
        {
            get { return _serviceProvider.GetRequiredServiceChecked<OriginalValuesFactory>(); }
        }

        public virtual RelationshipsSnapshotFactory RelationshipsSnapshotFactory
        {
            get { return _serviceProvider.GetRequiredServiceChecked<RelationshipsSnapshotFactory>(); }
        }

        public virtual StateEntryNotifier StateEntryNotifier
        {
            get { return ServiceProvider.GetRequiredServiceChecked<StateEntryNotifier>(); }
        }

        public virtual ChangeDetector ChangeDetector
        {
            get { return ServiceProvider.GetRequiredServiceChecked<ChangeDetector>(); }
        }

        public virtual StoreGeneratedValuesFactory StoreGeneratedValuesFactory
        {
            get { return _serviceProvider.GetRequiredServiceChecked<StoreGeneratedValuesFactory>(); }
        }

        public virtual StateEntryFactory StateEntryFactory
        {
            get { return ServiceProvider.GetRequiredServiceChecked<StateEntryFactory>(); }
        }

        public virtual IEnumerable<IEntityStateListener> EntityStateListeners
        {
            get
            {
                return ServiceProvider.TryGetService<IEnumerable<IEntityStateListener>>()
                       ?? Enumerable.Empty<IEntityStateListener>();
            }
        }
    }
}
