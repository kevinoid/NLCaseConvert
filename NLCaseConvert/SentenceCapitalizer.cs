// <copyright file="SentenceCapitalizer.cs" company="Kevin Locke">
// Copyright 2019-2020 Kevin Locke.  All rights reserved.
// </copyright>

namespace NLCaseConvert
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public class SentenceCapitalizer
    {
        /// <summary>
        /// Pattern for characters which are ignored before the start of a
        /// sentence.  Must match spaces, inverted punctuation, quotes,
        /// footnote markers, and anything else that can occur between the end
        /// of one sentence and the start of the next.
        /// Currently includes all spaces, marks, punctuation, and superscript
        /// numbers.
        /// </summary>
        private const string IgnoreBeforeSentence =
            @"[\s\p{M}\p{P}⁰¹²³⁴-⁹]*";

        /// <summary>
        /// String of characters with Unicode Line_Break property Quotation.
        /// </summary>
        /// <remarks>
        /// <see href="https://www.unicode.org/cldr/utility/list-unicodeset.jsp?a=%5B%3ALine_Break%3DQuotation%3A%5D&amp;esc=on&amp;g=&amp;i=" />
        /// with astral characters removed.
        /// </remarks>
        private const string LineBreakQuotation =
            "\""
            + "'"
            + "\u00AB"
            + "\u00BB"
            + "\u2018"
            + "\u2019"
            + "\u201B"
            + "\u201C"
            + "\u201D"
            + "\u201F"
            + "\u2039"
            + "\u203A"
            + "\u275B-\u2760"
            + "\u2E00"
            + "\u2E01"
            + "\u2E02-\u2E05"
            + "\u2E06-\u2E08"
            + "\u2E09"
            + "\u2E0A"
            + "\u2E0B"
            + "\u2E0C"
            + "\u2E0D"
            + "\u2E1C"
            + "\u2E1D"
            + "\u2E20"
            + "\u2E21";

        /// <summary>
        /// String of characters with Unicode Sentence_Break property ATerm.
        /// </summary>
        /// <remarks>
        /// <see href="https://www.unicode.org/cldr/utility/list-unicodeset.jsp?a=%5B%3ASentence_Break%3DATerm%3A%5D&amp;esc=on&amp;g=&amp;i=" />
        /// with astral characters removed.
        /// </remarks>
        private const string SentenceBreakATerm =
            "."
            + "\u2024"
            + "\uFE52"
            + "\uFF0E";

        /// <summary>
        /// String of characters with Unicode Sentence_Break property STerm.
        /// </summary>
        /// <remarks>
        /// <see href="https://www.unicode.org/cldr/utility/list-unicodeset.jsp?a=%5B%3ASentence_Break%3DSTerm%3A%5D&amp;esc=on&amp;g=&amp;i=" />
        /// with astral characters removed.
        /// </remarks>
        private const string SentenceBreakSTerm =
            "!"
            + "?"
            + "\u0589"
            + "\u061E"
            + "\u061F"
            + "\u06D4"
            + "\u0700"
            + "\u0701"
            + "\u0702"
            + "\u07F9"
            + "\u0837"
            + "\u0839"
            + "\u083D"
            + "\u083E"
            + "\u0964"
            + "\u0965"
            + "\u104A"
            + "\u104B"
            + "\u1362"
            + "\u1367"
            + "\u1368"
            + "\u166E"
            + "\u1735"
            + "\u1736"
            + "\u1803"
            + "\u1809"
            + "\u1944"
            + "\u1945"
            + "\u1AA8-\u1AAB"
            + "\u1B5A"
            + "\u1B5B"
            + "\u1B5E"
            + "\u1B5F"
            + "\u1C3B"
            + "\u1C3C"
            + "\u1C7E"
            + "\u1C7F"
            + "\u203C"
            + "\u203D"
            + "\u2047"
            + "\u2048"
            + "\u2049"
            + "\u2E2E"
            + "\u2E3C"
            + "\u3002"
            + "\uA4FF"
            + "\uA60E"
            + "\uA60F"
            + "\uA6F3"
            + "\uA6F7"
            + "\uA876"
            + "\uA877"
            + "\uA8CE"
            + "\uA8CF"
            + "\uA92F"
            + "\uA9C8"
            + "\uA9C9"
            + "\uAA5D-\uAA5F"
            + "\uAAF0"
            + "\uAAF1"
            + "\uABEB"
            + "\uFE56"
            + "\uFE57"
            + "\uFF01"
            + "\uFF1F"
            + "\uFF61";

        private readonly Func<string, string?> canonicalizer;
        private readonly Regex capitalizableRegex;

        protected SentenceCapitalizer(Builder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            this.CultureInfo = builder.CultureInfo;

            this.canonicalizer =
                builder.Canonicalizer ?? this.DefaultCanonicalizer;
            this.capitalizableRegex = new Regex(
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

            return this.capitalizableRegex.Replace(
                input,
                (match) =>
                {
                    string word = match.Groups["word"].Value;
                    string? canonical = this.canonicalizer(word);
                    if (canonical != null)
                    {
                        if (canonical == word)
                        {
                            return match.Value;
                        }

                        return match.Groups["context"].Value + canonical;
                    }

                    var textInfo = this.CultureInfo.TextInfo;
                    var context = match.Groups["context"];

                    // Capitalize entire initialism
                    if (match.Groups["initialism"].Success)
                    {
                        return context.Value + textInfo.ToUpper(word);
                    }

                    // Capitalize word if context requires it
                    // Note: context matches empty at start of string
                    if (context.Success)
                    {
                        return context.Value
                            + textInfo.ToUpper(word[0])
                            + word.Substring(1);
                    }

                    // Otherwise, leave match as-is
                    return match.Value;
                });
        }

        private static string GetPattern(Builder builder)
        {
            // Ensure match is at the beginning of a word
            string pattern = @"(?:"

                // Context before word which requires capitalization
                + @"(?<context>"

                // Capitalize start of string
                + "^" + IgnoreBeforeSentence

                // Match sentence terminal followed by space.
                //
                // FIXME: Follow rules in UAX #29 (or language-specific CLDR rules)
                // more closely
                // https://www.unicode.org/reports/tr29/#Default_Sentence_Boundaries
                + "|["
                + SentenceBreakATerm
                + SentenceBreakSTerm
                + "]" + IgnoreBeforeSentence;

            if (builder.AfterLineBreak)
            {
                pattern += @"|[\r\n\u0085\p{Zl}]" + IgnoreBeforeSentence;
            }

            if (builder.AfterParagraphBreak)
            {
                pattern += @"|\p{Zp}" + IgnoreBeforeSentence;

                if (!builder.AfterLineBreak)
                {
                    // Treat two or more line breaks (with optional ws) as a paragraph
                    pattern += @"|(?:(?:\r\n|\r(?!\n)|[\n\u0085\p{Zl}])"
                        + IgnoreBeforeSentence + "){2,}";
                }
            }

            // First word of a quote is capitalized only if the quote is a
            // complete sentence.
            // Match quote which contains sentence terminator.
            pattern +=
                "|[" + LineBreakQuotation + "]"
                + IgnoreBeforeSentence
                + "(?="
                    + "[^" + LineBreakQuotation + "]*"
                    + "[" + SentenceBreakATerm + SentenceBreakSTerm + "]"
                    + ".*?" + "[" + LineBreakQuotation + "]"
                + ")"

                // End of (?<context>
                + ")"

                // If there is no context, ensure this is the start of a word
                + @"|(?<![\p{L}\p{Mn}\p{Nd}]|[\p{L}\p{Mn}\p{Nd}][.'`’()\[\]\u2010\u2011-]))"

                // Match word
                + @"(?<word>\p{Ll}"

                // Match initialism with . between each letter.  Include last .
                // but not other sentence terminators.
                + @"(?:(?<initialism>"
                + "(?:[" + SentenceBreakATerm + @"]\s*\p{Ll})+"
                + "(?:[" + SentenceBreakATerm + "]|(?=[" + SentenceBreakSTerm + "])))"

                + "|" + builder.CapitalizeWordSuffixPattern
                + "))";

            return pattern;
        }

        private string? DefaultCanonicalizer(string word)
        {
            if (word[0] == 'i' && (word.Length == 1 || word[1] == '\''))
            {
                return this.CultureInfo.TextInfo.ToUpper(word[0])
                    + word.Substring(1);
            }

            return null;
        }

        /// <summary>
        /// Facilitates constructing a <see cref="SentenceCapitalizer" /> from
        /// configurable properties.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1034:NestedTypesShouldNotBeVisible",
            Justification = "Builder as nested type is conventional.")]
        public class Builder(CultureInfo cultureInfo)
        {
            public Builder()
                : this(CultureInfo.CurrentCulture)
            {
            }

            public CultureInfo CultureInfo { get; } = cultureInfo
                    ?? throw new ArgumentNullException(nameof(cultureInfo));

            public bool AfterParagraphBreak { get; set; }

            public bool AfterLineBreak { get; set; }

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
                @"(?:[\p{Ll}\p{Lo}\p{Lm}\p{Mn}\p{Nd}.'`’()\[\]\u2010\u2011-]*[\p{Ll}\p{Lo}\p{Lm}\p{Mn}\p{Nd})\]])?(?![\p{L}\p{Mn}\p{Nd}])";

            /// <summary>
            /// Gets or sets a function which returns a canonical capitalization
            /// for a non-capitalized word, if one exists.
            ///
            /// <para>
            /// This function is used for capitalizing proper nouns and other
            /// words which always have a particular capitalization.  Since the
            /// list of such nouns is likely to be quite large (or the inverse
            /// of a set of known words) and the storage of that list has
            /// time/space tradeoffs (e.g. between Dictionary and
            /// case-insensitive Set), this function allows the caller to
            /// optimize as they see fit.
            /// </para>
            /// </summary>
            public Func<string, string?>? Canonicalizer { get; set; }

            public SentenceCapitalizer Build()
            {
                return new SentenceCapitalizer(this);
            }
        }
    }
}
