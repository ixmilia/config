// Copyright (c) IxMilia.  All Rights Reserved.

using Xunit;

namespace IxMilia.Config.Test
{
    public class ConfigEscapeTests
    {
        [Theory]
        [InlineData("abcd", "abcd")]
        [InlineData("ab cd", "ab cd")]
        [InlineData("ab\"cd", "\"ab\\\"cd\"")]
        public void VerifySerialize(string value, string expected)
        {
            var actual = ConfigExtensions.EscapeString(value);
            Assert.Equal(expected, actual);
        }
    }
}
