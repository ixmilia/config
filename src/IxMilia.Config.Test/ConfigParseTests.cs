// Copyright (c) IxMilia.  All Rights Reserved.

using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigParseTests
    {
        [Theory]
        [InlineData("some string", "some string")] // regular string
        [InlineData("abba", "abba")] // first and last characters identical, but not quotes
        [InlineData(@"'final\nvalue'", "final\nvalue")] // single-quoted string
        [InlineData(@"""final\nvalue""", "final\nvalue")] // double-quoted string
        public void VerifyParse(string value, string expected)
        {
            var actual = ConfigExtensions.ParseString(value);
            Assert.Equal(expected, actual);
        }
    }
}
