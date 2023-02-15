// <copyright file="NameCapitalizer.cs" company="Kevin Locke">
// Copyright 2019-2020 Kevin Locke.  All rights reserved.
// </copyright>

namespace NLCaseConvert
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class NameCapitalizer
    {
        private readonly Regex capitalizeRegex;

        protected NameCapitalizer(Builder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            this.CultureInfo = builder.CultureInfo;

            this.capitalizeRegex = new Regex(
                GetPattern(builder),
                RegexOptions.ExplicitCapture);
        }

        public CultureInfo CultureInfo { get; }

        [return: NotNullIfNotNull(nameof(input))]
        public virtual string? Transform(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            return this.capitalizeRegex
                .Replace(
                    input,
                    (match) =>
                    {
                        string exclusion = match.Groups["exclusion"].Value;
                        if (exclusion.Length > 0)
                        {
                            return exclusion;
                        }

                        string unchanged = match.Groups["unchanged"].Value;
                        if (unchanged.Length > 0)
                        {
                            return unchanged;
                        }

                        string uprefix = match.Groups["uprefix"].Value;
                        if (uprefix.Length > 0)
                        {
                            return uprefix;
                        }

                        var textInfo = this.CultureInfo.TextInfo;

                        string allupper = match.Groups["allupper"].Value;
                        if (allupper.Length > 0)
                        {
                            return textInfo.ToUpper(allupper);
                        }

                        string cprefix = match.Groups["cprefix"].Value;
                        if (cprefix.Length > 0)
                        {
                            return textInfo.ToUpper(cprefix[0])
                                + cprefix.Substring(1);
                        }

                        string first = match.Groups["first"].Value;
                        string rest = match.Groups["rest"].Value;
                        return textInfo.ToUpper(first[0]) + rest;
                    });
        }

        private static string GetPattern(Builder builder)
        {
            // Note: Regex relies on alternation order during matching.
            // May not be reliable https://github.com/dotnet/docs/issues/15770
            return string.Empty

                // Match words which are excluded from case conversion
                + @"(?<exclusion>" + builder.GetExcludePattern() + ")"

                + @"|(?<allupper>" + builder.GetAllUpperPattern() + ")"

                // Match capitalized prefix
                + @"|(?<cprefix>" + builder.GetCapitalizedPrefixPattern() + ")"

                // Match uncapitalized prefix
                + @"|(?<uprefix>" + builder.GetUncapitalizedPrefixPattern() + ")"

                // Match letter to capitalize and the rest of the word
                + @"|(?<first>\p{Ll})(?<rest>" + builder.CapitalizeWordSuffixPattern + ")"

                // Match remaining words (to advance match position)
                + @"|(?<unchanged>" + builder.OtherWordPattern + ")";
        }

        /// <summary>
        /// Facilitates constructing a <see cref="NameCapitalizer" /> from
        /// configurable properties.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1034:NestedTypesShouldNotBeVisible",
            Justification = "Builder as nested type is conventional.")]
        public class Builder
        {
            public Builder()
                : this(CultureInfo.CurrentCulture)
            {
            }

            public Builder(CultureInfo cultureInfo)
            {
                this.CultureInfo = cultureInfo
                    ?? throw new ArgumentNullException(nameof(cultureInfo));
            }

            /// <summary>
            /// Gets a pattern to match Arabic name prefixes which should not
            /// be capitalized.
            /// </summary>
            public static ICollection<string> ArabicPrefixPatterns { get; } =

                // https://en.wikipedia.org/wiki/Wikipedia:Manual_of_Style/Arabic#Capitalization
                // https://en.wikipedia.org/wiki/Arabic_name
                Array.AsReadOnly(new[]
                {
                    "al-",
                    "as-",
                    "ash-",
                    "at-",
                });

            /// <summary>
            /// Gets a pattern to match name prefixes from the Celtic language
            /// family which should be capitalized.
            /// </summary>
            public static ICollection<string> CelticCapitalizedPrefixPatterns { get; } =

                // https://en.wikipedia.org/wiki/List_of_Scottish_Gaelic_surnames#Mac-
                // https://en.wikipedia.org/wiki/Celtic_onomastics#Surname_prefixes
                Array.AsReadOnly(new[]
                {
                    @"ill[e']",
                    @"ma?c(\s+an?|')?",
                    @"ó",
                    @"o'",
                });

            /// <summary>
            /// Gets a pattern to match name prefixes from the Celtic language
            /// family which should not be capitalized.
            /// </summary>
            public static ICollection<string> CelticUncapitalizedPrefixPatterns { get; } =

                // https://en.wikipedia.org/wiki/List_of_Scottish_Gaelic_surnames#Mac-
                // https://en.wikipedia.org/wiki/Celtic_onomastics#Surname_prefixes
                Array.AsReadOnly(new[]
                {
                    @"t-",
                });

            /// <summary>
            /// Gets a pattern to match words which are excluded from
            /// capitalization in Arabic names.
            ///
            /// <para>
            /// Currently matches common patronymics and teknonymics.
            /// </para>
            /// </summary>
            public static ICollection<string> ExcludedArabicWordPatterns { get; } =

                // https://en.wikipedia.org/wiki/Wikipedia:Manual_of_Style/Arabic#Capitalization
                // https://en.wikipedia.org/wiki/Patronymic#Arabic
                Array.AsReadOnly(new[]
                {
                    @"ab[uū]",
                    @"ben",
                    @"bin",
                    @"bint",
                    @"bte[.]?",
                    @"ibn",
                    @"umm",
                });

            /// <summary>
            /// Gets a pattern to match words which are excluded from
            /// capitalization in Dutch names.
            ///
            /// <para>
            /// Currently matches prepositions which do not follow another word.
            /// </para>
            /// </summary>
            public static ICollection<string> ExcludedDutchWordPatterns { get; } =

                // Only capitalize when not following another word
                // https://www.dutchgenealogy.nl/how-to-capitalize-dutch-names-with-prefixes/
                // https://english.stackexchange.com/a/185889
                // https://en.wikipedia.org/wiki/Van_(Dutch)#Related_prepositions
                // TODO: Does Belgian or German capitalization convention differ?
                Array.AsReadOnly(new[]
                {
                    @"de[nr]?",
                    @"het",
                    @"mes",
                    @"te[nr]?",
                    @"van",
                });

            /// <summary>
            /// Gets a pattern to match Italian name prefixes.
            /// </summary>
            public static ICollection<string> ItalianPrefixPatterns { get; } =

                // FIXME: Italian prepositional particles (di de d') are not
                // capitalized for historical figures (before surnames existed)
                // but are for contemporary surnames.
                // https://www.thoughtco.com/italian-capitalization-rules-2011478
                Array.AsReadOnly(new[]
                {
                    // Note: di and di prefixes currently excluded due to too
                    // many false-positives (Dennis, Denver, Diego, Diana)
                    @"d'",
                });

            /// <summary>
            /// Gets a pattern to match a word with at least one uppercase or
            /// titlecase character.
            /// </summary>
            public static string MixedCaseWordPattern { get; } =
                @"(?!['`’-])[\p{L}\p{Mn}\p{Nd}'`’]*[\p{Lu}\p{Lt}][\p{L}\p{Mn}\p{Nd}'`’-]*";

            public CultureInfo CultureInfo { get; }

            /// <summary>
            /// Gets a pattern to match a word where all letters should be
            /// changed to upper-case.
            /// </summary>
            public ICollection<string> AllUpperWordPatterns { get; } =
                new List<string>()
                {
                    // Roman numerals (generational suffix or reginal number)
                    // Exclude xi which is a common Chinese family name
                    // Add assertion to ensure match at least one character
                    // https://stackoverflow.com/a/267405
                    @"(?:(?=[mdclxvi])(?!xi\b)m{0,4}(?:cm|cd|d?c{0,3})(?:xc|xl|l?x{0,3})(?:ix|iv|v?i{0,3}))",
                };

            /// <summary>
            /// Gets a pattern to match everything after the lowercase letter
            /// of a word which will be capitalized.
            ///
            /// <para>
            /// Differences from <c>\w</c> (<c>[\p{L}\p{Mn}\p{Nd}\p{Pc}]</c>):
            /// <list type="bullet">
            /// <item>
            /// <description>
            /// <c>\p{Pc}</c> is excluded, since <c>_</c> is commonly used to
            /// _underline_words_ where it functions as a word break and
            /// undertie as an elision slur between words.
            /// </description>
            /// </item>
            /// <item>
            /// <description>
            /// <c>'</c> and <c>`</c> are included, since they are used in
            /// contractions, possessives, and as ASCII diacritical marks,
            /// except at the start/end of words where they are more often used
            /// as quotes.
            /// </description>
            /// </item>
            /// </list>
            /// </para>
            /// </summary>
            public string CapitalizeWordSuffixPattern { get; } =
                @"[\p{Ll}\p{Lo}\p{Lm}\p{Mn}\p{Nd}'`’]*";

            /// <summary>
            /// Gets patterns for name prefixes where both the prefix and
            /// following component are capitalized.
            /// </summary>
            public ICollection<string> CapitalizedPrefixPatterns { get; } =
                CelticCapitalizedPrefixPatterns
                    .Concat(ItalianPrefixPatterns)
                    .ToList();

            /// <summary>
            /// Gets patterns for words which are excluded from capitalization
            /// in names.
            /// </summary>
            public ICollection<string> ExcludedWordPatterns { get; } =
                ExcludedArabicWordPatterns
                    .Concat(new[] { MixedCaseWordPattern })
                    .ToList();

            /// <summary>
            /// Gets patterns for words which are excluded from capitalization
            /// when they occur after other words in names.
            /// </summary>
            public ICollection<string> ExcludedInteriorWordPatterns { get; } =
                ExcludedDutchWordPatterns.ToList();

            /// <summary>
            /// Gets a pattern to match a word which was not matched by any
            /// other rule (for performance, to advance the regex position).
            /// </summary>
            public string OtherWordPattern { get; } =
                @"[\p{L}\p{Mn}\p{Nd}][\p{L}\p{Mn}\p{Nd}'`’-]*";

            /// <summary>
            /// Gets patterns for name prefixes where the component following
            /// the prefix is capitalized, but the prefix is not.
            /// </summary>
            public ICollection<string> UncapitalizedPrefixPatterns { get; } =
                ArabicPrefixPatterns
                    .Concat(CelticUncapitalizedPrefixPatterns)
                    .ToList();

            public NameCapitalizer Build()
            {
                return new NameCapitalizer(this);
            }

            public string GetAllUpperPattern()
            {
                return "(?:"

                    + string.Join("|", this.AllUpperWordPatterns)

                    // Ensure pattern ends on a word boundary
                    + @")(?![\p{L}\p{Mn}\p{Nd}]|['`’][\p{L}\p{Mn}\p{Nd}])";
            }

            public string GetCapitalizedPrefixPattern()
            {
                return string.Join("|", this.CapitalizedPrefixPatterns);
            }

            public string GetExcludePattern()
            {
                StringBuilder excludePattern = new("(?:");

                var interiorWordPatterns = this.ExcludedInteriorWordPatterns;
                if (interiorWordPatterns.Count > 0)
                {
                    excludePattern
                        .Append(@"(?<=[\p{L}\p{Mn}\p{Nd}]\s+)(?:")
                        .AppendJoin('|', interiorWordPatterns)
                        .Append(')');
                }

                var wordPatterns = this.ExcludedWordPatterns;
                if (wordPatterns.Count > 0)
                {
                    if (excludePattern.Length > 0)
                    {
                        excludePattern.Append('|');
                    }

                    excludePattern
                        .Append("(?:")
                        .AppendJoin('|', wordPatterns)
                        .Append(')');
                }

                // Ensure exclusions end on a word boundary
                excludePattern
                    .Append(@")(?![\p{L}\p{Mn}\p{Nd}]|['`’][\p{L}\p{Mn}\p{Nd}])");

                return excludePattern.ToString();
            }

            public string GetUncapitalizedPrefixPattern()
            {
                return string.Join("|", this.UncapitalizedPrefixPatterns);
            }
        }
    }
}
