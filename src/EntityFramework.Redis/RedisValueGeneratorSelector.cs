// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Identity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Redis.Utilities;

namespace Microsoft.Data.Entity.Redis
{
    public class RedisValueGeneratorSelector : ValueGeneratorSelector
    {
        private readonly RedisValueGeneratorFactory _redisValueGeneratorFactory;

        public RedisValueGeneratorSelector(
            [NotNull] SimpleValueGeneratorFactory<GuidValueGenerator> guidFactory,
            [NotNull] RedisValueGeneratorFactory redisValueGeneratorFactory)
            : base(guidFactory)
        {
            Check.NotNull(redisValueGeneratorFactory, "redisValueGeneratorFactory");

            _redisValueGeneratorFactory = redisValueGeneratorFactory;
        }

        public override IValueGeneratorFactory Select(IProperty property)
        {
            Check.NotNull(property, "property");

            if (property.GenerateValueOnAdd
                && (property.PropertyType.IsInteger()
                    || property.PropertyType == typeof(uint)
                    || property.PropertyType == typeof(ulong)
                    || property.PropertyType == typeof(ushort)
                    || property.PropertyType == typeof(sbyte)))
            {
                return _redisValueGeneratorFactory;
            }

            return base.Select(property);
        }
    }
}
