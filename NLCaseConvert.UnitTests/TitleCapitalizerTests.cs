// <copyright file="TitleCapitalizerTests.cs" company="Kevin Locke">
// Copyright 2019-2025 Kevin Locke.  All rights reserved.
// </copyright>

namespace NLCaseConvert.UnitTests
{
    using System.Globalization;

    using Xunit;

    public static class TitleCapitalizerTests
    {
        private static readonly TitleCapitalizer TitleCapitalizer =
            new TitleCapitalizer.Builder(CultureInfo.InvariantCulture)
            .Build();

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        [MemberData(nameof(TestDataFile.ReadAll), "Titles.txt", MemberType = typeof(TestDataFile))]
        public static void CapitalizesInvariantCorrectly(string? input, string? expected)
        {
            Assert.Equal(expected, TitleCapitalizer.Transform(input));
        }
    }
}
