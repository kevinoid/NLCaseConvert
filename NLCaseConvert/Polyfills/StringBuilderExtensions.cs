// <copyright file="StringBuilderExtensions.cs" company="Kevin Locke">
// Copyright 2019 Kevin Locke.  All rights reserved.
// </copyright>
//
// Based on
// https://github.com/dotnet/corefx/blob/b2097cb/src/Common/src/CoreLib/System/Text/StringBuilder.cs
// with the following license header:
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NLCaseConvert
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// <c>AppendJoin</c> extensions for <see cref="StringBuilder" /> compatible
    /// with the <c>AppendJoin</c> methods in .NET Standard 2.1.
    /// </summary>
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendJoin(this StringBuilder builder, string? separator, params object?[] values)
        {
            return AppendJoinCore(builder, separator ?? string.Empty, values);
        }

        public static StringBuilder AppendJoin<T>(this StringBuilder builder, string? separator, IEnumerable<T> values)
        {
            return AppendJoinCore(builder, separator ?? string.Empty, values);
        }

        public static StringBuilder AppendJoin(this StringBuilder builder, string? separator, params string?[] values)
        {
            return AppendJoinCore(builder, separator ?? string.Empty, values);
        }

        public static StringBuilder AppendJoin(this StringBuilder builder, char separator, params object?[] values)
        {
            return AppendJoinCore(builder, char.ToString(separator), values);
        }

        public static StringBuilder AppendJoin<T>(this StringBuilder builder, char separator, IEnumerable<T> values)
        {
            return AppendJoinCore(builder, char.ToString(separator), values);
        }

        public static StringBuilder AppendJoin(this StringBuilder builder, char separator, params string?[] values)
        {
            return AppendJoinCore(builder, char.ToString(separator), values);
        }

        private static StringBuilder AppendJoinCore<T>(StringBuilder builder, string separator, IEnumerable<T> values)
        {
            Debug.Assert(separator != null, "null already replaced by empty string");

            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            using IEnumerator<T> en = values.GetEnumerator();
            if (!en.MoveNext())
            {
                return builder;
            }

            var value = en.Current;
            if (value != null)
            {
                builder.Append(value.ToString());
            }

            while (en.MoveNext())
            {
                builder.Append(separator);
                value = en.Current;
                if (value != null)
                {
                    builder.Append(value.ToString());
                }
            }

            return builder;
        }
    }
}