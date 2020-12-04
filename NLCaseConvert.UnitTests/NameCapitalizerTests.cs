// <copyright file="NameCapitalizerTests.cs" company="Kevin Locke">
// Copyright 2019-2020 Kevin Locke.  All rights reserved.
// </copyright>

namespace NLCaseConvert.UnitTests
{
    using System.Globalization;

    using Xunit;

    public static class NameCapitalizerTests
    {
        private static readonly NameCapitalizer NameCapitalizer =
            new NameCapitalizer.Builder(CultureInfo.InvariantCulture)
            .Build();

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        [MemberData(nameof(TestDataFile.ReadAll), "Names.txt", MemberType = typeof(TestDataFile))]
        public static void CapitalizesInvariantCorrectly(string? input, string? expected)
        {
            Assert.Equal(expected, NameCapitalizer.Transform(input));
        }
    }
}
