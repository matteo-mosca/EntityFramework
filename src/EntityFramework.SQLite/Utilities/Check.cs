// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Microsoft.Data.Entity.SQLite.Utilities
{
    [DebuggerStepThrough]
    internal static class Check
    {
        [ContractAnnotation("value:null => halt")]
        public static T NotNull<T>([NoEnumeration] T value, [InvokerParameterName] [NotNull] string parameterName)
        {
            if (ReferenceEquals(value, null))
            {
                NotEmpty(parameterName, "parameterName");
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        [ContractAnnotation("value:null => halt")]
        public static string NotEmpty(string value, [InvokerParameterName] [NotNull] string parameterName)
        {
            Exception e = null;
            if (ReferenceEquals(value, null))
            {
                e = new ArgumentNullException(parameterName);
            }
            else if (value.Trim().Length == 0)
            {
                e = new ArgumentException(Strings.ArgumentIsEmpty(parameterName));
            }

            if (e != null)
            {
                NotEmpty(parameterName, "parameterName");
                throw e;
            }

            return value;
        }

        public static T IsDefined<T>(T value, [InvokerParameterName] [NotNull] string parameterName)
            where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                NotEmpty(parameterName, "parameterName");
                throw new ArgumentException(Strings.InvalidEnumValue(parameterName, typeof(T)));
            }

            return value;
        }
    }
}
