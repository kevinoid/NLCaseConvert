// <copyright file="TitleCapitalizer.cs" company="Kevin Locke">
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

    public class TitleCapitalizer
    {
        private readonly Regex capitalizeRegex;

        protected TitleCapitalizer(Builder builder)
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

                        string first = match.Groups["first"].Value;
                        string rest = match.Groups["rest"].Value;
                        return first.ToUpper(this.CultureInfo) + rest;
                    });
        }

        private static string GetPattern(Builder builder)
        {
            // Note: Regex relies on alternation order during matching.
            // May not be reliable https://github.com/dotnet/docs/issues/15770
            return string.Empty

                // Match words which are excluded from case conversion
                + @"(?<exclusion>" + builder.GetExcludePattern() + ")"

                // Match letter to capitalize and the rest of the word
                + @"|(?<first>\p{Ll})(?<rest>" + builder.CapitalizeWordSuffixPattern + ")"

                // Match remaining words (to advance match position)
                + @"|(?<unchanged>" + builder.OtherWordPattern + ")";
        }

        /// <summary>
        /// Facilitates constructing a <see cref="TitleCapitalizer" /> from
        /// configurable properties.
        ///
        /// <para>
        /// Style guides differ about which words should be capitalized in a
        /// title and when.  For example, see
        /// <see href="https://titlecaseconverter.com/rules/" /> for discussion
        /// of the title capitalization conventions from several popular style
        /// guides.
        /// </para>
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

            public CultureInfo CultureInfo { get; }

            /// <summary>
            /// Gets additional patterns to exclude from capitalization.
            /// </summary>
            public ICollection<string> AdditionalExcludePatterns { get; } =
                new List<string>();

            /// <summary>
            /// Gets a pattern to match an email address.
            /// </summary>
            /// <remarks>
            /// Pattern for RFC 5322 email address from
            /// <see href="https://stackoverflow.com/a/201378" />.
            /// </remarks>
            public string EmailPattern { get; } =
                @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])";

            /// <summary>
            /// Gets a pattern to match a file name or path.
            /// </summary>
            public string FilePathPattern { get; } = "(?:"

                // Paths recognizable by their beginning
                + @"(?<!\S)([A-Za-z]:|[.][.]?|~)?(/+|\\+)\w[-\w$&+@./\\]*"

                // Paths/filenames recognizable by their extension
                + @"|[-\w$&+@./\\]*[.][a-zA-Z]{2,4}"

                // Optional possessive
                + ")(?:['’]s|s['’])?";

            /// <summary>
            /// Gets a pattern to match everything after the lowercase letter of a
            /// word which will be converted to title case.
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
            /// <item>
            /// <description>
            /// <c>()</c>, <c>[]</c> are included, since they are commonly used
            /// to denote word alternates/variants like <c>(s)he</c>,
            /// <c>plural(s)</c>, <c>colo(u)r</c>, <c>hair[cut]</c>, except at
            /// the start/end of words where they are more often used for edits
            /// and asides <c>[sic]</c>, <c>[from original]</c>,
            /// <c>[expletive]</c>, etc.
            /// </description>
            /// </item>
            /// </list>
            /// </para>
            /// </summary>
            public string CapitalizeWordSuffixPattern { get; } =
                @"[\p{Ll}\p{Lo}\p{Lm}\p{Mn}\p{Nd}'`’()\[\]]*";

            /// <summary>
            /// Gets a pattern to match a word with at least one uppercase or
            /// titlecase character.
            /// </summary>
            public string MixedCaseWordPattern { get; } =
                @"(?!['`’()\[\]])[\p{L}\p{Mn}\p{Nd}'`’()\[\]]*[\p{Lu}\p{Lt}][\p{L}\p{Mn}\p{Nd}'`’()\[\]]*";

            /// <summary>
            /// Gets a pattern to match a word which was not matched by any other
            /// rule (for performance, to advance the regex position).
            /// </summary>
            public string OtherWordPattern { get; } =
                @"[\p{L}\p{Mn}\p{Nd}][\p{L}\p{Mn}\p{Nd}'`’()\[\]]*";

            /// <summary>
            /// Gets a value indicating whether to capitalize
            /// <see cref="SmallWordPatterns" /> at the end of a title (or
            /// subtitle or quote).
            /// </summary>
            public bool CapitalizeSmallAtEnd { get; } = true;

            /// <summary>
            /// Gets a value indicating whether to capitalize
            /// <see cref="SmallWordPatterns" /> at the start of a hyphenated
            /// word/phrase.
            /// </summary>
            public bool CapitalizeSmallAtHyphenatedStart { get; } = true;

            /// <summary>
            /// Gets a value indicating whether to capitalize
            /// <see cref="SmallWordPatterns" /> at the start of a title (or
            /// subtitle or quote).
            /// </summary>
            public bool CapitalizeSmallAtStart { get; } = true;

            /// <summary>
            /// Gets a value indicating whether to capitalize
            /// <see cref="SmallWordPatterns" /> in the middle of hyphenated
            /// words/phrases.
            /// </summary>
            public bool CapitalizeSmallInHyphenated { get; }

            /// <summary>
            /// Gets expressions for words which are not typically capitalized
            /// in titles.
            /// </summary>
            public ICollection<string> SmallWordPatterns { get; } =
                new List<string>
                {
                    "a",
                    "an",
                    "and",
                    "as",
                    "at",
                    "but",
                    "by",
                    "en",
                    "for",
                    "if",
                    "in",
                    "nor",
                    "of",
                    "on",
                    "or",
                    "per",
                    "sic",
                    "the",
                    "to",
                    "v[.]?",
                    "via",
                    "vs[.]?",
                };

            /// <summary>
            /// Gets a pattern to match a URI.
            /// </summary>
            /// <remarks>
            /// Lenient pattern for a URI (or IRI) based on
            /// <see href="https://stackoverflow.com/a/190405" />.
            ///
            /// <para>
            /// Note: If this is too lenient, consider excluding characters
            /// from RFC 2396 unwise (which are rare) or requiring at least
            /// one dot or slash if not all ASCII.
            /// <see href="https://tools.ietf.org/html/rfc2396#section-2.4.3" />
            /// <see href="https://stackoverflow.com/a/13500078" />.
            /// </para>
            /// </remarks>
            [SuppressMessage(
                "Microsoft.Design",
                "CA1056:UriPropertiesShouldNotBeStrings",
                Justification = "This is not a URI, it is a pattern.")]
            public string UriPattern { get; } = "(?:"

                // URI/IRI with scheme
                + @"[A-Za-z][A-Za-z0-9+.-]*:[^\x00-\x1F <>""\x7F]+"

                // Domain/IDN with optional path
                // Note: IDN character restrictions vary.  \p{L}\p{Nd} for now.
                // https://stackoverflow.com/a/25067247
                // Exclude acronyms/initialisms.
                + @"|(?!(?:\p{L}[.])+(?:\P{L}|$))"
                + @"[A-Za-z0-9\p{L}\p{Nd}-]{1,63}[.][A-Za-z0-9\p{L}\p{Nd}-]{1,63}"
                + @"[^\x00-\x1F <>""\x7F]*"

                // Optional possessive
                + ")(?:['’]s|s['’])?";

            public TitleCapitalizer Build()
            {
                return new TitleCapitalizer(this);
            }

            public string GetExcludePattern()
            {
                StringBuilder excludePattern = new("(?:");

                if (this.SmallWordPatterns.Count > 0)
                {
                    if (this.CapitalizeSmallAtStart)
                    {
                        excludePattern.Append(@"(?<!(?:^|[:.;?!'""“‘‹«\u2014\u2015])[\s_]*)");
                    }

                    if (this.CapitalizeSmallInHyphenated)
                    {
                        excludePattern.Append(@"(?<![-\u2010\u2011])");
                    }
                    else if (this.CapitalizeSmallAtHyphenatedStart)
                    {
                        // Match if small word is not followed by hyphen
                        // or is both followed and preceded by hyphen.
                        // By De Morgan's Law:  not (followed and not preceded)
                        excludePattern
                            .Append(@"(?!(?<![-\u2010\u2011])(?:")
                            .AppendJoin('|', this.SmallWordPatterns)
                            .Append(@")[-\u2010\u2011])");
                    }

                    excludePattern
                        .Append("(?:")
                        .AppendJoin('|', this.SmallWordPatterns)
                        .Append(')');

                    if (this.CapitalizeSmallAtHyphenatedStart
                            && this.CapitalizeSmallInHyphenated)
                    {
                        excludePattern.Append(@"(?![-\u2010\u2011])");
                    }

                    if (this.CapitalizeSmallAtEnd)
                    {
                        excludePattern.Append(@"(?![\s_]*(?:[:.;?!'""“‘»]|$))");
                    }
                }

                foreach (string pattern in new[]
                {
                    // Note: Precedence ordering
                    this.UriPattern,
                    this.EmailPattern,
                    this.FilePathPattern,
                    this.MixedCaseWordPattern,
                }.Concat(this.AdditionalExcludePatterns))
                {
                    if (string.IsNullOrEmpty(pattern))
                    {
                        continue;
                    }

                    if (excludePattern.Length > 0)
                    {
                        excludePattern.Append('|');
                    }

                    excludePattern.Append(pattern);
                }

                // Ensure exclusions end on a word boundary
                excludePattern
                    .Append(@")(?![\p{L}\p{Mn}\p{Nd}]|['`’][\p{L}\p{Mn}\p{Nd}])");
                return excludePattern.ToString();
            }
        }
    }
}
