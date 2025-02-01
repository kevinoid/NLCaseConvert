// <copyright file="TestDataFile.cs" company="Kevin Locke">
// Copyright 2019-2025 Kevin Locke.  All rights reserved.
// </copyright>

namespace NLCaseConvert.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Xunit;

    /// <summary>
    /// Utility functions for dealing with test data files.
    ///
    /// <para>
    /// A test data file is a UTF-8-encoded plain text file, stored in the
    /// <c>Test_Data</c> directory, which contains pairs of lines with the
    /// input first and correct capitalization of the input second.
    /// </para>
    ///
    /// <para>
    /// Lines may contain
    /// <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#string-literals">escape
    /// sequences as defined for C# 6 regular string literals</see>.  A
    /// backslash followed by anything other than a valid escape sequence is
    /// invalid.
    /// </para>
    ///
    /// <para>
    /// Blank lines and comments (lines where the first non-whitespace
    /// character is <c>#</c>) are ignored.
    /// </para>
    /// </summary>
    public static class TestDataFile
    {
        private static readonly Regex PossibleEscapeRegex =
            new(@"\\([Uu][0-9a-fA-F]{8}|[Uu][0-9a-fA-F]{4}|x[0-9a-fA-F]{1,4}|.)?");

        public static IEnumerable<string> ReadAllLines(string path)
        {
            // Note: BaseDirectory is never null.  Annotation changed in v5.0.0
            // https://github.com/dotnet/runtime/pull/32486
            string fullPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory!,
                "Test_Data",
                path);
            using var reader = new StreamReader(fullPath, Encoding.UTF8);
            for (string? line = reader.ReadLine();
                line != null;
                line = reader.ReadLine())
            {
                if (IsLineIgnored(line))
                {
                    continue;
                }

                yield return Unescape(line);
            }
        }

        public static TheoryData<string, string> ReadAllPairs(string path)
        {
            using var lines = ReadAllLines(path).GetEnumerator();

            var linePairs = new TheoryData<string, string>();
            while (lines.MoveNext())
            {
                string input = lines.Current;

                if (!lines.MoveNext())
                {
                    throw new FormatException("Unpaired last line");
                }

                linePairs.Add(input, lines.Current);
            }

            return linePairs;
        }

        public static TheoryData<string, string> ReadAll(string path)
        {
            IEnumerable<object[]> alternatePairs = ReadAllPairs(path)
                .SelectMany(GetAlternateLineEndings);
            var linePairs = new TheoryData<string, string>();
            foreach (var linePair in alternatePairs)
            {
                linePairs.Add((string)linePair[0], (string)linePair[1]);
            }

            return linePairs;
        }

        private static IEnumerable<object[]> GetAlternateLineEndings(
            object[] pair)
        {
            yield return pair;

            // Create a test case with \r\n line endings, if the test case
            // only contains \n
            // Note: \r is very infrequent.  Test after \n for perf.
            string input = (string)pair[0];
            string inputCrLf = input.Replace("\n", "\r\n");
            if (!ReferenceEquals(input, inputCrLf)
                && input.IndexOf('\r') < 0)
            {
                string expected = (string)pair[1];
                string expectedCrLf = expected.Replace("\n", "\r\n");
                yield return new[] { inputCrLf, expectedCrLf };
            }
        }

        private static bool IsLineIgnored(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return line[i] == '#';
                }
            }

            return true;
        }

        private static string Unescape(string input)
        {
            return PossibleEscapeRegex.Replace(input, Unescape);
        }

        private static string Unescape(Match match)
        {
            string escapeStr = match.Groups[1].Value;
            char escapeChar = escapeStr.Length > 0 ? escapeStr[0] : '\0';
            switch (escapeChar)
            {
                case '\'': return "'";
                case '"': return "\"";
                case '\\': return "\\";
                case '0': return "\0";
                case 'a': return "\a";
                case 'b': return "\b";
                case 'f': return "\f";
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case 'v': return "\v";
                case 'u':
                case 'x':
                    if (escapeStr.Length > 1)
                    {
                        int code = int.Parse(
                            escapeStr.Substring(1),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture);
                        return char.ConvertFromUtf32(code);
                    }

                    // Character after \x not matched (not hex, or none)
                    break;
                default:
                    // Unrecognized character following \
                    break;
            }

            throw new FormatException("Invalid escape sequence \\" + escapeStr);
        }
    }
}
