// <copyright file="SentenceCapitalizerTests.cs" company="Kevin Locke">
// Copyright 2019-2025 Kevin Locke.  All rights reserved.
// </copyright>

namespace NLCaseConvert.UnitTests
{
    using System.Collections.Generic;
    using System.Globalization;

    using Xunit;

    public static class SentenceCapitalizerTests
    {
        private static readonly SentenceCapitalizer SentenceCapitalizer =
            new SentenceCapitalizer.Builder(CultureInfo.InvariantCulture)
            .Build();

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        [MemberData(nameof(TestDataFile.ReadAll), "Sentences.txt", MemberType = typeof(TestDataFile))]
        public static void CapitalizesInvariantCorrectly(string? input, string? expected)
        {
            Assert.Equal(expected, SentenceCapitalizer.Transform(input));
        }

        [Fact]
        public static void AppliesCanonicalizerToEachUncapitalizedWord()
        {
            const string input = "\"words n.e.e.d? capitalization, 'or' pun-ctuation.\"";
            var replacements = new Dictionary<string, string>
            {
                { "words", "wORds" },
                { "n.e.e.d", "N.E.E.D" },
                { "capitalization", "cApitalization" },
                { "or", "OR" },
                { "pun-ctuation", "PUN-ctuation" },
            };
            const string expected = "\"wORds N.E.E.D? cApitalization, 'OR' PUN-ctuation.\"";

            string TestCanonicalizer(string word)
            {
                Assert.True(
                    replacements.TryGetValue(word, out string? replacement),
                    "Called at most once for each expected word");
                replacements.Remove(word);

                // Note: ! required until Assert.True annotated with DoesNotReturnIf
                // https://github.com/xunit/xunit/issues/2011
                // https://github.com/xunit/assert.xunit/pull/36
                return replacement!;
            }

            var capitalizer = new SentenceCapitalizer.Builder(CultureInfo.InvariantCulture)
            {
                Canonicalizer = TestCanonicalizer,
            }
                .Build();

            Assert.Equal(expected, capitalizer.Transform(input));
            Assert.Empty(replacements);
        }
    }
}
